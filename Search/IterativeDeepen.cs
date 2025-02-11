using static Constants;

using System.Runtime.CompilerServices;

public static partial class Search
{
    static int iteration;
    public static int seldepth;


    static move rootBestMove;
    public static int rootScore;

    public static long rootPVNodes;


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
    /// Copies the newest Principal Variation line onto the current ply
    /// </summary>
    private static void push_to_pv(move m, int ply)
    {
        PV[ply][ply] = m;
        for (int i = ply + 1; i < iteration; i++)
        {
            PV[ply][i] = PV[ply + 1][i];

            if (PV[ply][i].IsNull) 
            {
                break;
            }
        }
    }
    
    /// <summary>
    /// Returns a uci-string-representation of the Principal Variation
    /// </summary>
    public static string get_pv()
    {
        string s = "";
        for (int i = 0; i < iteration && !PV[0][i].IsNull; i++)
        {
            s += $"{PV[0][i]} ";
        }
        return s;
    }

    /// <summary>
    /// Clears the current PV-Arrays
    /// </summary>
    public static void clear_whole_pv(int size)
    {
        PV = new move[size][];
        for (int i = 0; i < size; i++)
        {
            PV[i] = new move[size];
        }
    }
}
