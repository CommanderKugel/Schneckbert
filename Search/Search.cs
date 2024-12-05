using static Constants;
using static Utils;

using static System.Math;


public static class Search
{
    static int iteration;
    static int seldepth;

    static move rootBestMove;
    public static int rootScore;

    static move[][] PV;


    public static unsafe move iterativeDeepen(
        pos  root, 
        bool info     = false,
        int  maxDepth = 32, 
        long maxNodes = long.MaxValue
    )
    {
        fixed (SS* ss = SearchStack.stack)
        {

            rootBestMove = move.NullMove;
            iteration = 1;
            seldepth = 1;

            const int delta = 35;
            int alpha = -SCORE_MATE;
            int beta  =  SCORE_MATE;

            TimeManager.NodeCnt = 0;
            do
            {
                ResetPV(iteration);
                rootScore = Negamax(root, alpha, beta, iteration, 0, ss, false, info);

                if (rootScore <= alpha || rootScore >= beta)
                {
                    rootScore = Negamax(root, -SCORE_MATE, SCORE_MATE, iteration, 0, ss+1, false, info);
                }

                alpha = rootScore - delta;
                beta  = rootScore + delta;


                if (info)
                {
                    Console.WriteLine(
                        $"info depth {iteration} seldepth {seldepth} time {TimeManager.ElapsedMilliseconds()} score cp {rootScore} nodes {TimeManager.NodeCnt} nps {TimeManager.NPS()} pv {getPV()}"
                    );
                }
                else
                    Console.WriteLine("info score "+rootScore);


                iteration++;
            }
            while (iteration <= maxDepth && TimeManager.InSoftTimeLimit() && TimeManager.NodeCnt < maxNodes);

            return rootBestMove;

        } // fixed SearchStack
    }


    public static unsafe int Negamax(pos p, int alpha, int beta, int depth, int ply, SS* ss, bool doNull, bool info)
    {
        // #1 check for timeout and immediately return
        //    Negamax will negate this big score into the worst mateing score possible
        if (iteration > 1 && !TimeManager.InHardTimeLimit())
        {
            return 30_000;
        }

        TimeManager.NodeCnt++;

        // #2 avoid stack-overflows or IndexOutOfBound Exceptions
        if (ply >= MAX_SEARCH_PLY)
        {
            return NNUE.Evaluate(p);
        }

        ss->checkers = p.get_checkers();

        bool inQS     =  depth <= 0;
        bool isRoot   =  ply == 0;
        bool nonPV    =  alpha + 1 == beta;
        bool inCheck  =  ss->checkers != 0;

        int bestScore = -SCORE_MATE;
        int score;


        // #3 Check Extensions
        if (inCheck)
        {
            depth = Max(depth+1, 1);
        }


        // #4 Draw detection (besides Stalemate)
        if (!isRoot && (
            RepetitionTable.IsRepeatedPosition(p) ||
            p.IsFiftyMoveDraw ||
            p.IsInsufficientMaterial))
        {
            return 0;
        }


        // #5 fetch the Transpositiontables entry
        //    also try for cutoffs if possible
        ref var ttEntry = ref TranspositionTable.Probe(p.ZobristKey);
        bool ttHit = TranspositionTable.isTTHit(p.ZobristKey, ref ttEntry);
        move ttMove = ttHit ? ttEntry.move : move.NullMove;

        // TT Cutoff
        if (nonPV && ttHit && ttEntry.depth >= depth && Abs(ttEntry.score) < SCORE_MATE/2 && (
            ttEntry.flag == BOUND_UPPER && ttEntry.score <= alpha ||
            ttEntry.flag == BOUND_LOWER && ttEntry.score >= beta
            )) 
        {
            return ttEntry.score;
        }


        // #6 compute static Evaluation
        int staticEval = NNUE.Evaluate(p);


        // #7 Quiescense Search Stand Pat & Evaluate
        //    when a Quiet Position is reached, return the static evaluation score
        //    int a Quiet Position the best move is quiet (mostly: not a capture)
        if (inQS)
        {
            if (staticEval >= beta)
            {
                return staticEval;
            }

            if (staticEval >= alpha)
            {
                alpha = staticEval;
            }

            bestScore = staticEval;
        }


        // #8 Reverse Futility Pruning
        //    if the static Evaluation beats beta by a margin, we are probably a piece up
        //    and the opponent needs to recapture somewhere earlier in the search-tree.
        //    Thus, we can safely cut here
        if (nonPV && !inCheck && !isRoot && !inQS && depth<=7 &&
            staticEval - 75 * depth >= beta)
        {
            return staticEval;
        }


        // #9 Null Move Pruning
        //    the Null-Move-Observation states, that in most positions, it is an advantage 
        //    to be able to move first. So if we can give our opponent two moves in a row, and
        //    still beat beta, this position is too good and we can cut off here.
        //    Zugzwang Positions are the exception and arent accounted for yet, e.g. via p.hasNonPawnMaterial()
        if (doNull && nonPV && !inCheck && depth>2 && staticEval>=beta)
        {
            pos copy = new pos(p);
            copy.force_null_move(ss);

            score = -Negamax(copy, -beta, -alpha, depth-3, ply+1, ss+1, false, false);
            RepetitionTable.Pop();

            if (score >= beta)
            {
                return score;
            }
        }

        
        // #10 Move Generating and Ordering
        //     outsourced via the MovePicker class
        //     ToDo: Staged Move Generation
        var picker = new MovePicker(p, inQS, ttMove, ss);


        // keep track of moves that were played out, some will be pruned or illegal        
        int movesPlayed = 0;
        Span<move> playedAndLegal = stackalloc move[picker.mvCnt];

        int startAlpha = alpha;
        move m;
        move locBestMove = move.NullMove;
        
        // prepare futility pruning, this is not the optimal way of doing things
        // but changing would require another SPRT for probably 0.5 elo or so
        bool canFP = nonPV && !inCheck && depth<5 && (staticEval+150*depth < alpha);

        // main move loop here
        while (!(m = picker.next()).IsNull)
        {

            bool isCapture = p.is_capture(m);

            // #11 Futility Pruning
            //     If static evaluation falls below alpha, even by a margin
            //     we dont think that quiet moves will gain enough to beat alpha again
            //     only applicable after proving a non-mate line exists (includes mvsplayed>0 implicitly)
            if ( Abs(bestScore)<SCORE_MATE/2 &&
                !isCapture && 
                !m.IsPromo &&
                 canFP)
            {
                continue;
            }

            // Copy the position
            // make the move, but only if it is legal
            pos nextPos = new pos(p);
            if (!nextPos.make_move(m, ss))
            {
                continue;
            }

            playedAndLegal[movesPlayed++] = m;

            // Full window search in pv-nodes
            // if this node is a nonPV node, we still pass the zero window
            // The first move is assumed to be the best and shouldnt be pruned, reduced, etc.
            if (movesPlayed == 1)
            {
                score = -Negamax(nextPos, -beta, -alpha, depth-1, ply+1, ss+1, true, info);
            }
            else
            {
                // #12 Late Move Reductions
                //     Assuming that our Move-Ordering is good, the later moves should be increasingly bad.
                //     We search later moves at shallower depths to prove that they really are worse
                int R = 1;
                if (depth>2 && nonPV && !isCapture)
                {
                    R += 1;
                }

                // Reduced zero-window search á la #12
                score = -Negamax(nextPos, -alpha-1, -alpha, depth-R, ply+1, ss+1, true, info);

                // If a reduced Search fails high, we need to re-search at full depth to confirm that it 
                // really is better.
                if (score > alpha && R > 1)
                {
                    score = -Negamax(nextPos, -alpha-1, -alpha, depth-1, ply+1, ss+1, true, info);
                }

                // If we are in a PV node and one move seems to beat alpha, we need to re-search at full depth
                // and with a full window, to confirm we really beat alpha and get an exact score. 
                // Searches using a null-window only return upper bounds.
                if (!nonPV && score > alpha)
                {
                    score = -Negamax(nextPos, -beta, -alpha, depth-1, ply+1, ss+1, true, info);
                }
            }

            // here would be the moment to undo the move but its just copy-make
            RepetitionTable.Pop();

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
                if (info && inQS)
                {
                    seldepth = Max(seldepth, ply);
                }

                if (score > alpha)
                {
                    alpha = score;

                    // fail high
                    // If we beat beta, our opponent has a move that guarantees a position that scores beta,
                    // so he will never allow us to get to a position with a better score and we can safely prune here.
                    if (score >= beta)
                    {   
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

        // check-/stalemate detection
        if (!inQS && movesPlayed == 0)
        {
            return inCheck ? ply - SCORE_MATE : 0;
        }

        // enter data into the TT
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
