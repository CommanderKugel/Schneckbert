using static Constants;
using static Utils;

using static System.Math;


public static class Search
{
    const int MAX_PLY = 64;

    static int iteration;
    static int seldepth;
    static move rootBestMove;

    static move[][] PV;


    public static move iterativeDeepen(pos root, bool info = false,
                                       int maxDepth = 32)
    {
        rootBestMove = move.NullMove;
        iteration = 1;
        seldepth = 1;

        const int delta = 35;
        int alpha = -SCORE_MATE;
        int beta  =  SCORE_MATE;

        do
        {
            ResetPV(iteration);
            int rootScore = Negamax(root, -SCORE_MATE, SCORE_MATE, iteration, 0, info);


            if (info)
            {
                Console.WriteLine($"info depth {iteration} seldepth {seldepth} time {TimeManager.ElapsedMilliseconds()} score cp {rootScore} nodes {TimeManager.NodeCnt} nps {TimeManager.NPS()} pv {getPV()}");
            }
            else
            {
                Console.WriteLine($"info depth {iteration} score cp {rootScore}");
            }

            iteration++;
        }
        while (iteration <= maxDepth && TimeManager.InSoftTimeLimit());

        return rootBestMove;
    }


    public static int Negamax(pos p, int alpha, int beta, int depth, int ply, bool info)
    {
        // #1 check for timeout and immediately return
        //    Negamax will negate this big score into the worst mateing score possible
        if (iteration > 1 && !TimeManager.InHardTimeLimit())
        {
            return 30_000;
        }

        TimeManager.NodeCnt++;

        // #2 avoid stack-overflows or IndexOutOfBound Exceptions
        if (ply >= MAX_PLY)
        {
            return Evaluation.Evaluate(p);
        }

        bool inQS     = depth <= 0;
        bool isRoot   = ply == 0;
        bool nonPV    = alpha + 1 == beta;
        bool inCheck  = p.get_checkers() == 0;
        int bestScore = ply - SCORE_MATE;

        // #3 Draw detection (besides Stalemate)
        if (!isRoot && (
            RepetitionTable.IsRepeatedPosition(p) ||
            p.IsFiftyMoveDraw ||
            p.IsInsufficientMaterial))
        {
            return 0;
        }


        // #4 fetch the Transpositiontables entry
        //    also try for cutoffs if possible
        ref var entry = ref TranspositionTable.Probe(p.ZobristKey);
        bool ttHit = TranspositionTable.isTTHit(p.ZobristKey, ref entry);
        move ttMove = ttHit ? entry.move : move.NullMove;

        // TT Cutoff
        if (nonPV && ttHit && entry.depth >= depth && (
            entry.flag == BOUND_UPPER && entry.score <= alpha ||
            entry.flag == BOUND_LOWER && entry.score >= beta
            )) 
        {
            return entry.score;
        }


        // #5 Quiescense Search Stand Pat & Evaluate
        //    when a Quiet Position is reached, return the static evaluation score
        //    int a Quiet Position the best move is quiet (mostly: not a capture)
        if (inQS)
        {
            int eval = Evaluation.Evaluate(p);

            if (eval >= beta)
            {
                return eval;
            }

            if (eval >= alpha)
            {
                alpha = eval;
            }

            bestScore = eval;
        }

        // init MovePicker and maybe later other stuff for main move loop
        var picker = new MovePicker(p, inQS, ttMove);


        // init stuff for main move loop        
        int startAlpha = alpha;
        int movesPlayed = 0;
        int score;
        move m;
        move locBestMove = move.NullMove;


        // main move loop here
        while (!(m = picker.next()).IsNull)
        {
            pos nextPos = new pos(p);
            if (!nextPos.make_move(m))
            {
                continue;
            }

            movesPlayed++;
            

            // Full window search in pv-nodes
            // will be null-window in non-pv-nodes because null window gets passed either way
            if (movesPlayed == 1)
            {
                score = -Negamax(nextPos, -beta, -alpha, depth - 1, ply + 1, info);
            }
            else
            {
                // soon to be lmr stuff
                int R = 1;

                // somehow didnt pass sprt yet lol
                // maybe eval is too weak for now
                // needs a re-search when reactivated!
                if (false && nonPV && depth > 2 && nextPos.CapturedPiece != PIECE_NONE)
                {
                    //R += ln[movesPlayed];
                }

                score = -Negamax(nextPos, -alpha-1, -alpha, depth-R, ply+1, info);

                // Research if score beats current alpha
                // only usefull in pv-nodes because no full window exists
                if (!nonPV && score > alpha)
                {
                    score = -Negamax(nextPos, -beta, -alpha, depth - 1, ply + 1, info);
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

                if (info && !inQS)
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
                        picker.updateHistories(depth);
                        break;
                    }
                }
            }
        }

        // enter data into the TT
        if (Abs(bestScore) < SCORE_MATE / 2) 
        {
            int flag = bestScore >= beta  ? BOUND_LOWER
                     : alpha > startAlpha ? BOUND_EXACT
                                          : BOUND_LOWER;
            TranspositionTable.Push(ref entry, p.ZobristKey, bestScore, Max(depth, 0), flag, locBestMove);
        }

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
