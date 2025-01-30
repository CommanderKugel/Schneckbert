using static Constants;
using static Utils;

using static System.Math;
using System.Runtime.CompilerServices;


public static partial class Search
{
    static int iteration;

    static move rootBestMove;
    public static int rootScore;


    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe int Negamax<NodeType>(pos p, int alpha, int beta, int depth, int ply, SS* ss)
        where NodeType : NODE
    {

        bool isRoot = typeof(NodeType) == typeof(ROOT_NODE);
        bool nonPV  = typeof(NodeType) == typeof(NON_PV);
        bool isPV   = typeof(NodeType) == typeof(PV_NODE) || isRoot;

        // #1 Quiescense Search
        //    If we arrive at a leafe node, drop into QSearch.
        //    It only makes sense to evaluate Quiet positions, because captures 
        //    drastically change the positions evaluation.
        //    To get a valid evaluation, play captures until no usefull capture remains.
        if (depth <= 0)
        {
            return Quiescense.QSearch<NodeType>(p, alpha, beta, ply, ss);
        }

        // #2 Timeout Check
        //    Negamax will negate this big score into the worst mate-score possible
        if (iteration > 1 && !TimeManager.InHardTimeLimit())
        {
            return 30_000;
        }

        // #3 Avoid stack-overflows or IndexOutOfBound Exceptions
        if (ply >= MAX_SEARCH_PLY)
        {
            return p.accumulator.Evaluate(ref p);
        }

        // #4 Draw Detection
        //    Excludes Stalemate, this is left to the move loop
        //    Exclude RootNodes, to always return a valid move.
        if (!isRoot && (
            RepetitionTable.IsRepeatedPosition(p) ||
            p.IsFiftyMoveDraw ||
            p.IsInsufficientMaterial))
        {
            return 0;
        }

        // #5 Mate Distance Pruning
        //    If we already found a mate and the current search-depth guarantees
        //    that we cant find a quicker mate anymore, prune this branch.
        int mdAplha = Max(alpha, -SCORE_MATE + ply);
        int mdBeta  = Min(beta,   SCORE_MATE - ply - 1);
        if (!isRoot && mdAplha >= mdBeta)
        {
            return mdAplha;
        }
        

        // Count the Node as visited now
        TimeManager.NodeCnt++;

        // test if the side-to-move is in check
        ss->checkers = p.get_checkers();
        bool inCheck = ss->checkers != 0;
        bool inSingularity = !ss->ExcludedMove.IsNull;


        // bestScore will contain this nodes score, score is a tmp variable
        int bestScore = -SCORE_MATE;
        int score     = -SCORE_MATE;


        // #6 Transposition Table Probing
        //    A Transposition occurs, if one position is reached multiple times
        //    in the Search Tree, most likely over multiple IIR iterations.
        //    Test if there is a valid Transposition Table Entry for the current position.
        var ttEntry = TranspositionTable.get_entry(p.ZobristKey);
        bool ttHit = p.ZobristKey == ttEntry.key;
        move ttMove = ttHit ? ttEntry.move : move.NullMove;

        // Transposition Table Cutoff
        // If we found a valid TTEntry, we can return the saved score under some circumstances.
        if ( nonPV && ttHit && ttEntry.depth >= depth && 
            !score_is_terminal(ttEntry.score) && !inSingularity && 
            (
                ttEntry.flag == BOUND_UPPER && ttEntry.score <= alpha ||
                ttEntry.flag == BOUND_LOWER && ttEntry.score >= beta  ||
                ttEntry.flag == BOUND_EXACT
            )) 
        {
            return ttEntry.score;
        }


        // #7 Static Evaluation
        //    We will not return this score, because we didn't prove that this position is quiet.
        //    We can use this to make educated guesses about this position and this branch of the game tree.
        ss->StaticEval = !inCheck ? p.accumulator.Evaluate(ref p) : 0;


        // #8 Static Evaluation Correction History
        // COMING SOON*


        // #9 Reverse Futility Pruning
        //    if the static Evaluation beats beta by a margin, we are probably a piece up
        //    and the opponent needs to recapture somewhere earlier in the search-tree.
        //    Thus, we can safely cut here
        if (nonPV && !inCheck && !isRoot && depth<=7 && !inSingularity &&
            ss->StaticEval - 75 * depth >= beta)
        {
            return ss->StaticEval;
        }


        // #10 Razoring
        // *COMING SOON*


        // #11 Null Move Pruning
        //     the Null-Move-Observation states, that in most positions, it is an advantage 
        //     to be able to move first. So if we can give our opponent two moves in a row, and
        //     still beat beta, this position is too good and we can cut off here.
        //     Zugzwang Positions are the exception and arent accounted for yet, e.g. via p.hasNonPawnMaterial()
        if ( nonPV && 
            !(ss-1)->Move.IsNull &&
            !inCheck && 
             depth>2 && 
            !inSingularity && 
             ss->StaticEval>=beta)
        {
            pos copy = p;
            copy.force_null_move(ss);

            score = -Negamax<NON_PV>(copy, -beta, -alpha, depth-3, ply+1, ss+1);
            RepetitionTable.Pop();

            if (score >= beta)
            {
                return score;
            }
        }


        // #12 Prob Cut
        // *COMING SOON*


        // #13 Check Extensions
        //     Positions where we are in Check are highly tactical and only a few moves are legal.
        //     Also, evasions are extremely important to the outcome of the game.
        //     To ensure we find the correct tactic here, extend this node.
        if (inCheck)
        {
            depth++;
        }

        // #14 Internal Iterative Reductions
        //     If we are at sufficient depth and we did not see this node before, search at a lower depth.
        //     Assume that we will encouner this node again in the next ID-iteration and search at the 
        //     intended depth, while also using a ttMove.
        if (!ttHit && depth > 4)
        {
            depth--;
        }

        
        // #15 Move Generating and Ordering
        //     outsourced via the MovePicker class
        //     ToDo: Staged Move Generation
        Span<move> moves = stackalloc move[MAX_MOVE_CNT];
        Span<int> scores = stackalloc int[MAX_MOVE_CNT];
        var picker = new MovePicker(p, false, ttMove, ss, ref moves, ref scores);


        // preparing the main move-loop
        int movesPlayed = 0;
        Span<move> playedAndLegal = stackalloc move[picker.mvCnt];
        int startAlpha = alpha;
        move m;
        move locBestMove = move.NullMove;
        
        // main move loop here
        while (!(m = picker.next(ref p, ref moves, ref scores)).IsNull)
        {

            if (m == ss->ExcludedMove)
            {
                continue;
            }

            bool isCapture = p.is_capture(m);
            bool nonMatingLineExists = !score_is_terminal(bestScore);

            // #16 Futility Pruning
            //     If static evaluation falls below alpha, even by a margin,
            //     we dont expect that quiet moves will gain enough to beat alpha again.
            //     Only applicable after proving a non-mate line exists (includes mvsplayed>0 implicitly)
            if ( nonMatingLineExists &&
                !isCapture && 
                !m.IsPromo &&
                 nonPV && 
                !inCheck &&
                 depth<5 && 
                (ss->StaticEval + 150*depth < alpha))
            {
                continue;
            }

            // #17 Late Move Pruning
            if (!isCapture &&
                !m.IsPromo &&
                 nonMatingLineExists &&
                 depth < 5 &&
                 movesPlayed > lmpTable[depth])
            {
                continue;
            }

            // #18 Static Exchange Evaluation Pruning
            //     If the move hat a bad SEE score in move ordering,
            //     and the move can possibly be pruned, 
            //     run another SEE with a wider margin.
            if (nonMatingLineExists &&
                nonPV &&
                picker.curr_score(ref scores) < 900_000)
            {
                int margin = 
                    isCapture ? -200 * depth 
                              : -25 * depth * depth;
                if (!SEE.see_threshold(m, ref p, margin))
                {
                    continue;
                }
            }

            // #19 Copy Make
            //     Copy the position, then make the move on the copied position.
            //     Avoids complex undo_move() method.
            pos nextPos = p;
            if (!nextPos.make_move(m, ss))
            {
                continue;
            }

            // save the played move, maybe its history will be updated later
            playedAndLegal[movesPlayed++] = m;

            int extensions = 0;

            // #20 Singular Extensions
            //     If the TT suggests that we have a really strong move and we have sufficient depth
            //     left in our search-iteration, we can test if the TTMove is the only strong move.
            //     For that, search this node again, while excluding the candidate singular move.
            //     If no other move fails high, the move is singular and we should extend it,
            //     because it is a lot more important than the other moves.
            if ( movesPlayed == 1 &&
                !inSingularity &&
                !isRoot &&
                 depth >= 8 &&
                 m == ttMove &&
                 ttEntry.depth >= depth-3 &&
                 ttEntry.flag != BOUND_UPPER)
            {
                int singularBeta = Max(-SCORE_MATE+1, ttEntry.score - depth * 2);
                int singularDepth = (depth - 1) / 2;

                ss->ExcludedMove = m;
                int singularScore = Negamax<NON_PV>(p, singularBeta-1, singularBeta, singularDepth, ply, ss);
                ss->ExcludedMove = move.NullMove;

                // extension
                if (singularScore < singularBeta)
                {
                    extensions = 1;
                }

                // #21 Multi Cut
                //     If the candidate-singular move is proven not singular and any other move would
                //     fail high, even in the current window-bounds, this position is probably too good
                //     to be true and our opponent wont allow this branch of the tree to be played out.
                //
                // *PROMISING AT STC, TRY AGAIN LATER AT LCT*
                // Elo: 2.99 +/- 8.98
                // Games: 2438, Wins: 671, Losses: 650, Draws: 1117, Points: 1229.5 (50.43 %)
                // Ptnml(0-2): [54, 288, 516, 305, 56], WL/DD Ratio: 0.97
                //
                /*
                else if (singularScore >= singularBeta && singularBeta >= beta && !score_is_terminal(singularScore))
                {
                    RepetitionTable.Pop();
                    return singularBeta;
                }
                */

                // #22 Negative Extensions
                // *COMING SOON*

            }


            int newDepth = depth + extensions - 1;

            
            // #23 Late Move Reductions
            //     Assuming our move-ordering is good, the later a move is picked, the worse it is.
            //     For later moves, we only want to prove that it really is worse, using a shallower search.
            if (movesPlayed > 1 && depth > 2 && !isCapture)
            {
                int R = lmrTable[Min(movesPlayed, 63)];

                score = -Negamax<NON_PV>(nextPos, -alpha-1, -alpha, newDepth+1-R, ply+1, ss+1);

                // if the shallower search failse high, we need to prove that the move really beats alpha
                // by re-searching at full depth.
                if (R > 1 && score > alpha)
                {
                    score = -Negamax<NON_PV>(nextPos, -alpha-1, -alpha, newDepth, ply+1, ss+1);
                }
            }

            // if LMR conditions dont apply, do a full-depth Zero-Window Search.
            else if (nonPV || movesPlayed > 1)
            {
                score = -Negamax<NON_PV>(nextPos, -alpha-1, -alpha, newDepth, ply+1, ss+1);
            }

            // if we are at a PVNode and ply either the first move, or a later move has beaten alpha, re-search
            // at full depth with a full window, to optain am exact score.
            if (isPV && (score > alpha || movesPlayed == 1))
            {
                
                score = -Negamax<PV_NODE>(nextPos, -beta, -alpha, newDepth, ply+1, ss+1);
            }


            // here would be the moment to undo the move but its just copy-make
            RepetitionTable.Pop();

            // #24 Score Update
            if (score > bestScore)
            {
                bestScore = score;
                locBestMove = m;

                // updating root moves mid-iteration works, because we always search the roots best move first
                if (isRoot)
                {
                    rootBestMove = m;
                }

                if (isPV && ply < iteration)
                {
                    update_pv(m, ply);
                }

                if (score > alpha)
                {
                    alpha = score;

                    // fail high
                    // If we beat beta, our opponent has a move that guarantees a position that scores beta,
                    // so he will never allow us to get to a position with a better score and we can safely prune here.
                    if (score >= beta)
                    {   

                        // #25 Update history Scores and Killer Moves
                        if (!isCapture)
                        {
                            // Update the Killer-move heuristic, if this move was a quiet-move.
                            // we shouldnt add captures to killer moves, or they would never be filled with quiet moves.
                            ss->killerMove = m;

                            // Update the history-scores of all played quiet moves.
                            // History Scores are greater for generally good moves, and smaller for worse ones.
                            // History Scores can be falsified in favor for weaker but more common vs. stronger but rarer moves.
                            // Currently the Butterfly- and PieceTo histories are implemented.
                            History.updateQuietHistValues(playedAndLegal, movesPlayed-1, depth, p);
                        }
    
                        break;
                    }
                }
            }
        }

        // #26 Check- & Stalemate detection
        //     If no moves in a position are legal, the side to move is either chekmated or stalemated.
        //     Dont save terminal nodes in the TT.
        if (movesPlayed == 0)
        {
            return inCheck ? ply - SCORE_MATE : 0;
        }

        
        // #27 Save Node to TT
        //     Skip singular confirmation searches, because the best move was excluded there
        if (!inSingularity)
        {
            // For all-nodes, if there is already a ttEntry, dont overwrite the ttMove if alpha was not beaten.
            if (ttHit && !ttMove.IsNull && bestScore < alpha)
            {
                locBestMove = ttMove;
            }

            int flag = bestScore >= beta ? BOUND_LOWER : alpha > startAlpha ? BOUND_EXACT : BOUND_UPPER;
            TranspositionTable.Push(p.ZobristKey, bestScore, depth, flag, locBestMove);
        }

        return bestScore;
    }
}
