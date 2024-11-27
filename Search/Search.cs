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


    public static move iterativeDeepen(pos root, bool info = false,
                                       int maxDepth = 32, long maxNodes = long.MaxValue)
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
            rootScore = Negamax(root, alpha, beta, iteration, 0, false, info);

            if (rootScore <= alpha || rootScore >= beta)
            {
                rootScore = Negamax(root, -SCORE_MATE, SCORE_MATE, iteration, 0, false, info);
            }

            alpha = rootScore - delta;
            beta  = rootScore + delta;


            if (info)
                Console.WriteLine(
                    $"info depth {iteration} seldepth {seldepth} time {TimeManager.ElapsedMilliseconds()} score cp {rootScore} nodes {TimeManager.NodeCnt} nps {TimeManager.NPS()} pv {getPV()}"
                );


            iteration++;
        }
        while (iteration <= maxDepth && TimeManager.InSoftTimeLimit() && TimeManager.NodeCnt < maxNodes);

        return rootBestMove;
    }


    public static int Negamax(pos p, int alpha, int beta, int depth, int ply, bool doNull, bool info)
    {
        // #1 check for timeout and immediately return
        //    Negamax will negate this big score into the worst mateing score possible
        if (iteration > 1 && !TimeManager.InHardTimeLimit())
        {
            return 30_000;
        }

        TimeManager.NodeCnt++;

        // #2 avoid stack-overflows or IndexOutOfBound Exceptions
        if (ply >= Board.MAX_SEARCH_DEPTH)
        {
            return NNUE.Evaluate(p);
        }

        bool inQS     = depth <= 0;
        bool isRoot   = ply == 0;
        bool nonPV    = alpha + 1 == beta;
        bool inCheck  = p.get_checkers() != 0;
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

        // SearchStack Lookup
        // current entry is for ply+1 so we dont get out of bounds errors
        ref SS ss = ref SearchStack.stack[ply];


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
            copy.force_null_move(ref ss);

            score = -Negamax(copy, -beta, -alpha, depth-3, ply+1, false, false);
            RepetitionTable.Pop();

            if (score >= beta)
            {
                return score;
            }
        }


        // init MovePicker and maybe later other stuff for main move loop
        var picker = new MovePicker(p, inQS, ttMove);


        // init stuff for main move loop        
        int startAlpha = alpha;
        int movesPlayed = 0;
        move m;
        move locBestMove = move.NullMove;


        // main move loop here
        while (!(m = picker.next()).IsNull)
        {
            pos nextPos = new pos(p);
            if (!nextPos.make_move(m, ref ss))
            {
                continue;
            }

            movesPlayed++;

            // Full window search in pv-nodes
            // will be null-window in non-pv-nodes because null window gets passed either way
            if (movesPlayed == 1)
            {
                score = -Negamax(nextPos, -beta, -alpha, depth-1, ply+1, true, info);
            }
            else
            {
                int R = 1;

                if (depth>2 && nonPV && ss.CapturedPiece==PIECE_TYPE_NONE)
                {
                    R += 1;
                }

                // reduced zero-window search
                score = -Negamax(nextPos, -alpha-1, -alpha, depth-R, ply+1, true, info);

                // re-search at full depth, also with zero window
                if (score > alpha && R > 1)
                {
                    score = -Negamax(nextPos, -alpha-1, -alpha, depth-1, ply+1, true, info);
                }

                // if score beats alpha and its a PV node, re-search using a full-window
                if (!nonPV && score > alpha)
                {
                    score = -Negamax(nextPos, -beta, -alpha, depth-1, ply+1, true, info);
                }
            }

            // here would be the moment to undo the move but its just copy-make
            RepetitionTable.Pop();

            if (score > bestScore)
            {
                bestScore = score;
                locBestMove = m;

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
                    seldepth = Max(seldepth, iteration - depth);
                }

                if (score > alpha)
                {
                    alpha = score;

                    if (score >= beta)
                    {
                        if (p.piece_on(m.to) == PIECE_TYPE_NONE)
                        {
                            picker.updateQuietHistories(depth, p);
                        }
                        else
                        {
                            picker.updateCaptHistories(depth, p);
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
