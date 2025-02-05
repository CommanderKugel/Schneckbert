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


        // Improving
        // Not a pruning technique in itself, but used to slightly tweak other heuristics.
        // rfp (-11 elo) eval - 75 * (depth - improving) >= beta
        // lmr (-10 elo) R -= improving
        //bool improving = !inCheck && ply > 1 && (ss-2)->StaticEval != 0 && (ss-2)->StaticEval < ss->StaticEval;
        //int  intImproving = improving ? 1 : 0;


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

            int nmpDepth = depth - 3 - depth / 6;
            score = -Negamax<NON_PV>(copy, -beta, -alpha, nmpDepth, ply+1, ss+1);
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
        Span<int> scores = stackalloc int [MAX_MOVE_CNT];
        var picker = new MovePicker(p, false, ttMove, ss, ref moves, ref scores);


        // save the every played move and the number of played moves
        // to update Histries accordingly
        int movesPlayed    = 0;
        int quietsPlayed   = 0;
        int capturesPlayed = 0;
        Span<move> quietsList   = stackalloc move[MAX_MOVE_CNT];
        Span<move> capturesList = stackalloc move[MAX_MOVE_CNT];

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
            //     Assuming our moveordering is good, later picked moves are 
            //     assumed to be worse than earlier moves.
            //     So skip later, worse scoring and non-tactical moves at low depths.
            // *IMPROVING SPRT COMING SOON*
            if (!isCapture &&
                !m.IsPromo &&
                 nonMatingLineExists &&
                 depth < 5 &&
                 movesPlayed > lmpTable[depth])
            {
                continue;
            }


            // #18 History Pruning
            //     If the History Score of a move is really bad and 
            //     there are no tactical reasons to try the move, prune it.
            // *COMING SOON*


            // #19 Static Exchange Evaluation Pruning
            //     If the move hat a bad SEE score in move ordering,
            //     and the move can possibly be pruned, 
            //     run another SEE with a wider margin.
            if (nonMatingLineExists &&
                nonPV &&
                picker.curr_score(ref scores) < 900_000)
            {
                int margin = isCapture 
                           ? -200 * depth 
                           :  -25 * depth * depth;
                if (!SEE.see_threshold(m, ref p, margin))
                {
                    continue;
                }
            }

            // #20 Copy Make
            //     Copy the position, then make the move on the copied position.
            //     Avoids complex undo_move() method.
            pos nextPos = p;
            if (!nextPos.make_move(m, ss))
            {
                continue;
            }

            // save the played move, maybe its history will be updated later
            movesPlayed++;
            if (isCapture) capturesList[capturesPlayed++] = m;
            else           quietsList[quietsPlayed++]     = m;

            int extensions = 0;

            // #21 Singular Extensions
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

                ss->ExcludedMove  = m;
                int singularScore = Negamax<NON_PV>(p, singularBeta-1, singularBeta, singularDepth, ply, ss);
                ss->ExcludedMove  = move.NullMove;

                // extension
                if (singularScore < singularBeta)
                {
                    extensions = 1;
                }

                // #22 Multi Cut
                //     If the candidate-singular move is proven not singular and any other move would
                //     fail high, even in the current window-bounds, this position is probably too good
                //     to be true and our opponent wont allow this branch of the tree to be played out.
                //
                // *PROMISING AT STC, TRY AGAIN LATER AT LCT*
                // *somehow doesnt gain by a bizillion elo*
                // Elo: 2.99 +/- 8.98 (2438 games)
                /*
                else if (singularScore >= singularBeta && singularBeta >= beta && !score_is_terminal(singularScore))
                {
                    RepetitionTable.Pop();
                    return singularBeta;
                }
                */

                // #23 Negative Extensions
                // *COMING SOON*

            }

            // apply extensions
            int newDepth = depth + extensions - 1;

            
            // #24 Late Move Reductions
            //     Assuming our move-ordering is good, the later a move is picked, the worse it is.
            //     For later moves, we only want to prove that it really is worse, using a shallower search.
            if (movesPlayed > 1 && depth > 2 && !isCapture)
            {
                // log-formula = 1 + log(depth) * log(moveCount) / 1.5
                int R = lmrTable[Min(depth, 63)][Min(movesPlayed, 63)];

                // History Reduction 
                // *COMING SOON*
                //if (picker.curr_score(ref scores) < -500) R++;

                //if (!improving) R++;          -> -10.13 +/-  7.68 @ 40+0.40 
                //if (nonPV) R++;               -> -21.37 +/- 11.47 @  8+0.08
                //if (!improving && nonPV) R++; ->  -7.65 +/-  7.32 @  8+0.08

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

            // #25 Score Update
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

                    // #26 Fail High
                    //     If we beat beta, our opponent has a move that guarantees a position that scores beta,
                    //     so he will never allow us to get to a position with a better score and we can safely prune here.
                    //     This makes up about 95% of all pruning in common Chess-Engine Negamax implementations.
                    if (score >= beta)
                    {   

                        // #27 Update Killer Moves
                        if (!isCapture)
                        {
                            ss->killerMove = m;
                        }

                        // #28 Update History Values
                        int delta = History.calcHistDelta(depth);
                        
                        if (isCapture) capturesPlayed--;
                        else           quietsPlayed--;

                        History.increaseSingleHistValue(m, delta, ref p, isCapture);
                        History.decreaseCaptureHistValues(ref capturesList, capturesPlayed, delta, ref p);
                        if (!isCapture)
                        {
                            History.decreaseQuietHistValues(ref quietsList, quietsPlayed, delta, ref p);
                        }

                        break;
                    }
                }
            }
        }

        // #29 Check- & Stalemate detection
        //     If no moves in a position are legal, the side to move is either chekmated or stalemated.
        //     Dont save terminal nodes in the TT.
        if (movesPlayed == 0)
        {
            return inCheck ? ply - SCORE_MATE : 0;
        }

        
        // #30 Save Node to TT
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
