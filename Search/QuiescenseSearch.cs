using static Constants;
using static Utils;

using static System.Math;
using System.Runtime.CompilerServices;


public static partial class Search
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe int QSearch<NodeType>(pos p, int alpha, int beta, int ply, SS* ss)
        where NodeType : NODE
    {
        seldepth = Max(seldepth, ply);
        TimeManager.NodeCnt++;
        TimeManager.QSNodeCnt++;

        bool nonPV   = typeof(NodeType) == typeof(NON_PV);
        bool isPV    = typeof(NodeType) == typeof(PV_NODE);

        if (isPV)
        {
            PV[ply][ply] = move.NullMove;
        }

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

        int bestScore = -SCORE_MATE;
        int score;
      

        // #3 fetch the Transpositiontables entry
        //    also try for cutoffs if possible
        var ttEntry = TranspositionTable.get_entry(p.ZobristKey);
        bool ttHit = ttEntry.key == p.ZobristKey;
        move ttMove = ttHit ? ttEntry.move : move.NullMove;

        // TT Cutoff
        if (nonPV && ttHit && !score_is_terminal(ttEntry.score) && (
            ttEntry.flag == BOUND_UPPER && ttEntry.score <= alpha ||
            ttEntry.flag == BOUND_LOWER && ttEntry.score >= beta  ||
            ttEntry.flag == BOUND_EXACT
            )) 
        {
            return ttEntry.score;
        }


        // #4 Static Evaluation
        if (inCheck)
        {
            ss->StaticEval = ss->RawStaticEval = 0;
        }
        else
        {
            ss->StaticEval = 
                ss->RawStaticEval = p.accumulator.Evaluate(ref p);
            //CorrHist.correct_static_eval(ref p, ss);
        }


        // #5 Quiescense Search Stand Pat & Evaluate
        //    when a Quiet Position is reached, return the static evaluation score
        //    int a Quiet Position the best move is quiet (mostly: not a capture)
        if (ss->StaticEval >= beta && !inCheck)
        {
            return ss->StaticEval;
        }

        if (ss->StaticEval >= alpha && !inCheck)
        {
            alpha = ss->StaticEval;
        }

        bestScore = !inCheck ? ss->StaticEval : -SCORE_MATE + ply ;


        // #6 Move Generating and Ordering
        //    outsourced via the MovePicker class
        //    ToDo: Staged Move Generation
        Span<move> moves = stackalloc move[MAX_MOVE_CNT];
        Span<int> scores = stackalloc int[MAX_MOVE_CNT];
        var picker = new MovePicker(p, !inCheck, ttMove, ss, ref moves, ref scores);


        // keep track of moves that were played out, some will be pruned or illegal        
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
            bool isCapture = p.is_capture(m);
            bool nonMatingLineExists = !score_is_terminal(bestScore);

            // #7 Static Exchange Evaluation pruning
            //    If the move hat a bad SEE score in the scoring phase
            //    and we can safely prune the move, run another SEE with a wider margin.
            if ( nonMatingLineExists &&
                 nonPV &&
                 picker.curr_score(ref scores) < 900_000 &&
                !SEE.see_threshold(m, ref p, alpha - ss->StaticEval - 300))
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

            movesPlayed++;
            if (isCapture) capturesList[capturesPlayed++] = m;
            else           quietsList[quietsPlayed++]     = m;

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
                if (isPV && score > alpha)
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

                if (isPV)
                {
                    push_to_pv(m, ply);
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
                            int delta = History.calcHistDelta(1);
                            History.increaseSingleHistValue(m, delta, ref p, isCapture);
                            History.decreaseCaptureHistValues(ref capturesList, capturesPlayed-1, delta, ref p);
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
        TranspositionTable.Push(p.ZobristKey, bestScore, 0, flag, locBestMove);

        return bestScore;
    }
}
