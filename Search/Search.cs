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
                rootScore = Negamax(root, alpha, beta, iteration, 0, ss, false, info);

                // ToDo: Gradual widening
                if (rootScore <= alpha || rootScore >= beta)
                {
                    rootScore = Negamax(root, -SCORE_MATE, SCORE_MATE, iteration, 0, ss+1, false, info);
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
    public static unsafe int Negamax(pos p, int alpha, int beta, int depth, int ply, SS* ss, bool doNull, bool info)
    {
        // #1 check for timeout, maybe stop searching
        //    Negamax will negate this big score into the worst mate-score possible
        if (iteration > 1 && !TimeManager.InHardTimeLimit())
        {
            return 30_000;
        }


        // #2 Avoid stack-overflows or IndexOutOfBound Exceptions
        if (ply >= MAX_SEARCH_PLY)
        {
            return p.accumulator.Evaluate(ref p);
        }

        
        // #3 Drop into QSearch if we found a leaf-node
        //    It only makes sense to evaluate Quiet positions.
        //    Quiescense Search plays all captures possible until a capture is no longer
        //    the best move in a position - a quiet Position.
        if (depth <= 0)
        {
            return Quiescense.QSearch(p, alpha, beta, ply, ss);
        }

        // Only now count this node as visited, to not count double in QSearch
        TimeManager.NodeCnt++;

        // initialize some important variables to use later on
        ss->checkers = p.get_checkers();

        bool isRoot   =  ply == 0;
        bool nonPV    =  alpha + 1 == beta;
        bool inCheck  =  ss->checkers != 0;

        int bestScore = -SCORE_MATE;
        int score     = -SCORE_MATE;


        // #4 Check Extensions
        //    Lines that give check 
        if (inCheck)
        {
            depth = Max(depth+1, 1);
        }


        // #5 Draw detection (excluding Stalemate)
        //    Exclude RootNodes, otherwise we will return an illegal null-move in ID
        if (!isRoot && (
            RepetitionTable.IsRepeatedPosition(p) ||
            p.IsFiftyMoveDraw ||
            p.IsInsufficientMaterial))
        {
            return 0;
        }


        // #6 fetch the Transpositiontables entry
        //    also try for cutoffs if possible
        ref var ttEntry = ref TranspositionTable.Probe(p.ZobristKey);
        bool ttHit = TranspositionTable.isTTHit(p.ZobristKey, ref ttEntry);
        move ttMove = ttHit ? ttEntry.move : move.NullMove;

        // TT Cutoff
        if (nonPV && ttHit && ttEntry.depth >= depth && !score_is_terminal(ttEntry.score) && (
            ttEntry.flag == BOUND_UPPER && ttEntry.score <= alpha ||
            ttEntry.flag == BOUND_LOWER && ttEntry.score >= beta  ||
            ttEntry.flag == BOUND_EXACT
            )) 
        {
            return ttEntry.score;
        }


        // #7 Static Evaluation
        //    We will not return this score, because we cant prove that this position is quiet,
        //    but we can use it to make educated guesses about this branch of the game tree
        ss->StaticEval = !inCheck ? p.accumulator.Evaluate(ref p) : 0;


        // #8 Reverse Futility Pruning
        //    if the static Evaluation beats beta by a margin, we are probably a piece up
        //    and the opponent needs to recapture somewhere earlier in the search-tree.
        //    Thus, we can safely cut here
        if (nonPV && !inCheck && !isRoot && depth<=7 &&
            ss->StaticEval - 75 * depth >= beta)
        {
            return ss->StaticEval;
        }


        // #9 Null Move Pruning
        //    the Null-Move-Observation states, that in most positions, it is an advantage 
        //    to be able to move first. So if we can give our opponent two moves in a row, and
        //    still beat beta, this position is too good and we can cut off here.
        //    Zugzwang Positions are the exception and arent accounted for yet, e.g. via p.hasNonPawnMaterial()
        if (doNull && nonPV && !inCheck && depth>2 && ss->StaticEval>=beta)
        {
            pos copy = p;
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
        Span<move> moves = stackalloc move[MAX_MOVE_CNT];
        Span<int> scores = stackalloc int[MAX_MOVE_CNT];
        var picker = new MovePicker(p, false, ttMove, ss, ref moves, ref scores);


        // keep track of moves that were played out, some will be pruned or illegal        
        int movesPlayed = 0;
        Span<move> playedAndLegal = stackalloc move[picker.mvCnt];

        int startAlpha = alpha;
        move m;
        move locBestMove = move.NullMove;
        
        // prepare futility pruning, this is not the optimal way of doing things
        // but changing would require another SPRT for probably 0.5 elo or so
        bool canFP = nonPV && !inCheck && depth<5 && (ss->StaticEval + 150*depth < alpha);

        // main move loop here
        while (!(m = picker.next(ref moves, ref scores)).IsNull)
        {

            bool isCapture = p.is_capture(m);
            bool nonMatingLineExists = !score_is_terminal(bestScore);

            // #11 Futility Pruning
            //     If static evaluation falls below alpha, even by a margin
            //     we dont think that quiet moves will gain enough to beat alpha again
            //     only applicable after proving a non-mate line exists (includes mvsplayed>0 implicitly)
            if ( nonMatingLineExists &&
                !isCapture && 
                !m.IsPromo &&
                 canFP)
            {
                continue;
            }

            // #12 Static Exchange Evaluation pruning
            //     If the move hat a bad SEE score in the scoring phase
            //     and we can safely prune the move, run another SEE with a wider margin.
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

            // Copy the position
            // make the move, but only if it is legal
            pos nextPos = p;
            if (!nextPos.make_move(m, ss))
            {
                continue;
            }

            playedAndLegal[movesPlayed++] = m;

            
            if (movesPlayed > 1 && depth > 2 && !isCapture)
            {
                int R = ln[movesPlayed];

                score = -Negamax(nextPos, -alpha-1, -alpha, depth-R, ply+1, ss+1, true, info);

                if (R > 1 && score > alpha)
                {
                    score = -Negamax(nextPos, -alpha-1, -alpha, depth-1, ply+1, ss+1, true, info);
                }
            }
            else if (nonPV || movesPlayed > 1)
            {
                score = -Negamax(nextPos, -alpha-1, -alpha, depth-1, ply+1, ss+1, true, info);
            }

            if (!nonPV && (score > alpha || movesPlayed == 1))
            {
                score = -Negamax(nextPos, -beta, -alpha, depth-1, ply+1, ss+1, true, info);
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
        if (movesPlayed == 0)
        {
            return inCheck ? ply - SCORE_MATE : 0;
        }

        // If this is an all-node and the tt contains a move for this position,
        // dont overwrite the ttMove if alphy was not beaten
        locBestMove = ttHit && !ttMove.IsNull && bestScore < alpha ? ttMove : locBestMove;

        // set the flag
        int flag = bestScore >= beta ? BOUND_LOWER : alpha > startAlpha ? BOUND_EXACT : BOUND_UPPER;

        // enter data into the TT
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
