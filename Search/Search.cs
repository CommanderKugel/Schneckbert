using static Constants;
using static Utils;

using static System.Math;
using System.Runtime.CompilerServices;


public static class Search
{
    static int iteration;

    static move rootBestMove;
    public static int rootScore;

    static move[][] PV;


    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe move iterativeDeepen(
        pos  root, 
        bool info     = false,
        int  maxDepth = 32, 
        long maxNodes = long.MaxValue)
    {
        fixed (SS* ss = SearchStack.stack)
        {

            rootBestMove = move.NullMove;
            iteration = 1;
            Quiescense.seldepth = 1;

            const int delta = 35;
            int alpha = -SCORE_MATE;
            int beta  =  SCORE_MATE;

            TimeManager.NodeCnt = 0;

            do
            {
                ResetPV(iteration);
                rootScore = Negamax<ROOT_NODE>(root, alpha, beta, iteration, 0, ss, false, info);

                // ToDo: Gradual widening
                if (rootScore <= alpha || rootScore >= beta)
                {
                    rootScore = Negamax<ROOT_NODE>(root, -SCORE_MATE, SCORE_MATE, iteration, 0, ss+1, false, info);
                }

                // update the Windows
                alpha = rootScore - delta;
                beta  = rootScore + delta;


                if (info)
                {
                    Console.WriteLine(
                        $"info depth {iteration} seldepth {Quiescense.seldepth} time {TimeManager.ElapsedMilliseconds()} score cp {rootScore} nodes {TimeManager.NodeCnt} nps {TimeManager.NPS()} pv {getPV()}"
                    );
                }
                else
                    Console.WriteLine("info depth "+iteration+" score "+rootScore);

                iteration++;
            }
            while (iteration <= maxDepth && TimeManager.InSoftTimeLimit() && TimeManager.NodeCnt < maxNodes);

            return rootBestMove;

        } // fixed SearchStack
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe int Negamax<NodeType>(pos p, int alpha, int beta, int depth, int ply, SS* ss, bool doNull, bool info)
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


        // bestScore will contain this nodes score, score is a tmp variable
        int bestScore = -SCORE_MATE;
        int score     = -SCORE_MATE;


        // #6 Transposition Table Probing
        //    A Transposition occurs, if one position is reached multiple times
        //    in the Search Tree, most likely over multiple IIR iterations.
        //    Test if there is a valid Transposition Table Entry for the current position.
        ref var ttEntry = ref TranspositionTable.Probe(p.ZobristKey);
        bool ttHit = TranspositionTable.isTTHit(p.ZobristKey, ref ttEntry);
        move ttMove = ttHit ? ttEntry.move : move.NullMove;

        // Transposition Table Cutoff
        // If we found a valid TTEntry, we can return the saved score under some circumstances.
        if (nonPV && ttHit && ttEntry.depth >= depth && !score_is_terminal(ttEntry.score) && (
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
        if (nonPV && !inCheck && !isRoot && depth<=7 &&
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
        if (doNull && nonPV && !inCheck && depth>2 && ss->StaticEval>=beta)
        {
            pos copy = p;
            copy.force_null_move(ss);

            score = -Negamax<NON_PV>(copy, -beta, -alpha, depth-3, ply+1, ss+1, false, false);
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

        // #14 IIR
        // *COMING SOON*

        
        // #15 Move Generating and Ordering
        //     outsourced via the MovePicker class
        //     ToDo: Staged Move Generation
        Span<move> moves = stackalloc move[MAX_MOVE_CNT];
        Span<int> scores = stackalloc int[MAX_MOVE_CNT];
        var picker = new MovePicker(p, false, ttMove, ss, ref moves, ref scores);


        // keep track of moves that were played out, some will be pruned or illegal        
        int movesPlayed = 0;
        Span<move> playedAndLegal = stackalloc move[picker.mvCnt];

        int startAlpha = alpha;
        move m;
        move locBestMove = move.NullMove;
        
        // main move loop here
        while (!(m = picker.next(ref moves, ref scores)).IsNull)
        {

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
            if ( nonMatingLineExists &&
                !isCapture &&
                !m.IsPromo &&
                 nonPV &&
                !inCheck &&
                 depth<=4 &&
                (movesPlayed > depth * depth + 2))
            {
                continue;
            }

            // #18 Static Exchange Evaluation Pruning
            //     If the move hat a bad SEE score in move ordering,
            //     and the move can possibly be pruned, 
            //     run another SEE with a wider margin.
            if ( nonMatingLineExists &&
                 nonPV &&
                 picker.try_see(ref scores))
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


            // #20 Singular Extensions
            // #21 Multi Cut
            // #22 Negative Extensions
            // *COMING SOON*

            
            // #23 Late Move Reductions
            //     Assuming our move-ordering is good, the later a move is picked, the worse it is.
            //     For later moves, we only want to prove that it really is worse, using a shallower search.
            if (movesPlayed > 1 && depth > 2 && !isCapture)
            {
                int R = ln[movesPlayed];

                score = -Negamax<NON_PV>(nextPos, -alpha-1, -alpha, depth-R, ply+1, ss+1, true, info);

                // if the shallower search failse high, we need to prove that the move really beats alpha
                // by re-searching at full depth.
                if (R > 1 && score > alpha)
                {
                    score = -Negamax<NON_PV>(nextPos, -alpha-1, -alpha, depth-1, ply+1, ss+1, true, info);
                }
            }

            // if LMR conditions dont apply, do a full-depth Zero-Window Search.
            else if (nonPV || movesPlayed > 1)
            {
                score = -Negamax<NON_PV>(nextPos, -alpha-1, -alpha, depth-1, ply+1, ss+1, true, info);
            }

            // if we are at a PVNode and ply either the first move, or a later move has beaten alpha, re-search
            // at full depth with a full window, to optain am exact score.
            if (isPV && (score > alpha || movesPlayed == 1))
            {
                score = isPV
                    ? -Negamax<PV_NODE>(nextPos, -beta, -alpha, depth-1, ply+1, ss+1, true, info)
                    : -Negamax<NON_PV >(nextPos, -beta, -alpha, depth-1, ply+1, ss+1, true, info);
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

                if (info && ply < iteration)
                {
                    UpdatePV(m, ply);
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

        // For all-nodes, if there is already a ttEntry, dont overwrite the ttMove if alpha was not beaten.
        if (ttHit && !ttMove.IsNull && bestScore < alpha)
        {
            locBestMove = ttMove;
        }

        // #27 Save Node to TT
        int flag = bestScore >= beta ? BOUND_LOWER : alpha > startAlpha ? BOUND_EXACT : BOUND_UPPER;
        TranspositionTable.Push(ref ttEntry, p.ZobristKey, bestScore, Max(depth, 0), flag, locBestMove);

        return bestScore;
    }


    /// <summary>
    /// copies the newest Principal Variation line onto the current ply
    /// </summary>
    private static void UpdatePV(move m, int ply)
    {
        PV[ply][ply] = m;
        for (int i = ply + 1; i < iteration; i++)
            PV[ply][i] = PV[ply + 1][i];
    }
    
    /// <summary>
    /// returns a uci-string-representation of the Principal Variation
    /// </summary>
    public static string getPV()
    {
        string s = "";
        for (int i = 0; i < iteration; i++)
            s += $"{PV[0][i]} ";
        return s;
    }

    /// <summary>
    /// clears the current PV-Arrays
    /// </summary>
    public static void ResetPV(int depth)
    {
        PV = new move[depth][];
        for (int i = 0; i < depth; i++)
            PV[i] = new move[depth];
    }
}
