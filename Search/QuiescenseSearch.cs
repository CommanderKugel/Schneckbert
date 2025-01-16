using static Constants;
using static Utils;

using static System.Math;
using System.Runtime.CompilerServices;


public static class Quiescense
{
    public static int seldepth;


    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe int QSearch<NodeType>(pos p, int alpha, int beta, int ply, SS* ss)
        where NodeType : NODE
    {
        TimeManager.NodeCnt++;

        // #1 avoid stack-overflows or IndexOutOfBound Exceptions
        if (ply >= MAX_SEARCH_PLY)
        {
            return p.accumulator.Evaluate(ref p);
        }

        // #2 Draw detection (besides Stalemate)
        if (RepetitionTable.IsRepeatedPosition(p) ||
            p.IsFiftyMoveDraw ||
            p.IsInsufficientMaterial)
        {
            return 0;
        }

        ss->checkers = p.get_checkers();
        bool inCheck = ss->checkers != 0;
        bool nonPV   = typeof(NodeType) == typeof(NON_PV);
        bool isPV    = typeof(NodeType) == typeof(PV_NODE);

        int bestScore = -SCORE_MATE;
        int score;
      

        // #3 fetch the Transpositiontables entry
        //    also try for cutoffs if possible
        ref var ttEntry = ref TranspositionTable.Probe(p.ZobristKey);
        bool ttHit = TranspositionTable.isTTHit(p.ZobristKey, ref ttEntry);
        move ttMove = ttHit ? ttEntry.move : move.NullMove;

        // TT Cutoff
        if (nonPV && ttHit && Abs(ttEntry.score) < SCORE_MATE/2 && (
            ttEntry.flag == BOUND_UPPER && ttEntry.score <= alpha ||
            ttEntry.flag == BOUND_LOWER && ttEntry.score >= beta  ||
            ttEntry.flag == BOUND_EXACT
            )) 
        {
            return ttEntry.score;
        }


        // #4 Static Evaluation
        int staticEval = !inCheck ? p.accumulator.Evaluate(ref p) : 0;


        // #5 Quiescense Search Stand Pat & Evaluate
        //    when a Quiet Position is reached, return the static evaluation score
        //    int a Quiet Position the best move is quiet (mostly: not a capture)
        if (staticEval >= beta && !inCheck)
        {
            return staticEval;
        }

        if (staticEval >= alpha && !inCheck)
        {
            alpha = staticEval;
        }

        bestScore = !inCheck ? staticEval : -SCORE_MATE + ply ;


        // #6 Move Generating and Ordering
        //    outsourced via the MovePicker class
        //    ToDo: Staged Move Generation
        Span<move> moves = stackalloc move[MAX_MOVE_CNT];
        Span<int> scores = stackalloc int[MAX_MOVE_CNT];
        var picker = new MovePicker(p, !inCheck, ttMove, ss, ref moves, ref scores);


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
            bool nonMatingLineExists = Abs(bestScore) < SCORE_MATE/2;

            // #7 Static Exchange Evaluation pruning
            //    If the move hat a bad SEE score in the scoring phase
            //    and we can safely prune the move, run another SEE with a wider margin.
            if ( nonMatingLineExists &&
                 nonPV &&
                 picker.try_see(ref scores) &&
                !SEE.see_threshold(m, ref p, alpha - staticEval - 300))
            {
                continue;
            }

            // Copy the position
            // make the move, but only if it is legal
            pos nextPos = p;
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
                score = -QSearch<NodeType>(nextPos, -beta, -alpha, ply+1, ss+1);
            }
            else
            {
                // Reduced zero-window search
                score = -QSearch<NON_PV>(nextPos, -alpha-1, -alpha, ply+1, ss+1);


                // If we are in a PV node and one move seems to beat alpha, we need to re-search at full depth
                // and with a full window, to confirm we really beat alpha and get an exact score. 
                // Searches using a null-window only return upper bounds.
                if (!nonPV && score > alpha)
                {
                    score = -QSearch<NodeType>(nextPos, -beta, -alpha, ply+1, ss+1);
                }
            }

            // here would be the moment to undo the move but its just copy-make
            RepetitionTable.Pop();

            if (score > bestScore)
            {
                bestScore = score;
                locBestMove = m;

                seldepth = Max(seldepth, ply);

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
                            History.updateQuietHistValues(playedAndLegal, movesPlayed-1, 1, p);
                        }
    
                        break;
                    }
                }
            }
        }

        // if we have a valid move in the TT and we dont beat alpha or want to overwrite it with
        // a null move, don't
        if ((locBestMove.IsNull || bestScore < alpha) && ttHit && !ttMove.IsNull)
        {
            locBestMove = ttMove;
        }

        // enter data into the TT
        int flag = bestScore >= beta ? BOUND_LOWER : alpha > startAlpha ? BOUND_EXACT : BOUND_UPPER;
        TranspositionTable.Push(ref ttEntry, p.ZobristKey, bestScore, 0, flag, locBestMove);

        return bestScore;
    }
}
