using static Constants;

using System.Runtime.CompilerServices;

public static partial class Search
{
    static int iteration;
    public static int seldepth;


    static move rootBestMove;
    public static int rootScore;

    public static long rootPVNodes;
    static bool info_;


    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe move iterativeDeepen(
        pos  root, 
        bool info,
        int  maxDepth = 32, 
        long maxNodes = long.MaxValue)
    {
        fixed (SS* ss = SearchStack.stack)
        {

            rootBestMove = move.NullMove;
            iteration = 1;
            seldepth  = 1;

            rootPVNodes = 0;
            info_ = info;

            const int delta = 35;
            int alpha = -SCORE_MATE;
            int beta  =  SCORE_MATE;

            TimeManager.NodeCnt = 0;

            do
            {
                rootScore = Negamax<ROOT_NODE>(root, alpha, beta, iteration, 0, ss);

                // ToDo: Gradual widening
                if (rootScore <= alpha || rootScore >= beta)
                {
                    rootScore = Negamax<ROOT_NODE>(root, -SCORE_MATE, SCORE_MATE, iteration, 0, ss+1);
                }

                // update the Windows
                alpha = rootScore - delta;
                beta  = rootScore + delta;

                if (info_ && iteration >= 4)
                {
                    report_to_uci(root);
                }

                iteration++;
            }
            while (iteration <= maxDepth && 
                   TimeManager.InSoftTimeLimit(rootBestMove) && 
                   TimeManager.NodeCnt < maxNodes);

            return rootBestMove;

        } // fixed SearchStack
    }


    private static void report_to_uci(pos root)
    {
        string myScore = score_is_terminal(rootScore) 
            ? $"mate {(Math.Abs(rootScore) - SCORE_MATE) / 2}" : $"cp {rootScore}";
                    
            Console.WriteLine(
                $"info depth {iteration} seldepth {seldepth} time {TimeManager.ElapsedMilliseconds()} score {myScore} nodes {TimeManager.NodeCnt} nps {TimeManager.NPS()} pv {rootBestMove}"
            );
    }

    
    /// <summary>
    /// Returns a string of the Principal Variation in uci-format.
    /// </summary>
    public static unsafe string get_pv(pos p)
    {
        string pv = "";

        SS ss = new SS();
        Span<move> moves = stackalloc move[MAX_MOVE_CNT];
        int ply = 0;

        bool moveFound = true;
        while (moveFound)
        {
            moveFound = false;
            var entry = TranspositionTable.get_entry(p.ZobristKey);
            
            // no ttHit or no move saved
            if (entry.key != p.ZobristKey || entry.move.IsNull)
            {
                break;
            }

            // move is in movelist && is legal
            int moveCount = MoveGen.GenerateMoves(ref moves, ref p, false, p.get_checkers());
            for (int i=0; i<moveCount; i++)
            {
                if (moves[i] == entry.move && p.make_move(moves[i], &ss))
                {
                    ply++;
                    pv += moves[i].ToString() + " ";
                    moveFound = true;
                    break;
                }
            }

            if (moveFound && (
                RepetitionTable.IsRepeatedPosition(p) || p.IsFiftyMoveDraw))
            {
                moveFound = false;
            }
        }

        for (int i=0; i<ply; i++)
        {
            RepetitionTable.Pop();
        }

        return pv;
    }
}
