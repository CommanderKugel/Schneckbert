using static Constants;

using System.Runtime.CompilerServices;

public static partial class Search
{
    static int iteration;
    public static int seldepth;


    static move rootBestMove;
    public static int rootScore;

    public static long rootPVNodes;
    private static bool info_;

    private static long hardNodes;


    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe move iterativeDeepen(
        pos  root, 
        bool info,
        int  maxDepth, 
        long softNodes_ = long.MaxValue,
        long hardNodes_ = long.MaxValue)
    {
        fixed (SS* ss = SearchStack.stack)
        {

            rootBestMove = move.NullMove;
            iteration = 1;
            seldepth  = 1;

            rootPVNodes = 0;
            info_ = info;

            hardNodes = hardNodes_;

            const int delta = 35;
            int alpha = -SCORE_MATE;
            int beta  =  SCORE_MATE;

            TimeManager.NodeCnt = 0;
            Array.Fill(TimeManager.RootNodeCounts, 0);

            do
            {
                rootScore = Negamax<ROOT_NODE>(root, alpha, beta, iteration, 0, ss, false);

                // ToDo: Gradual widening
                if (rootScore <= alpha || rootScore >= beta)
                {
                    rootScore = Negamax<ROOT_NODE>(root, -SCORE_MATE, SCORE_MATE, iteration, 0, ss, false);
                }

                // update the Windows
                alpha = rootScore - delta;
                beta  = rootScore + delta;

                if (info_ && iteration >= 4 && Math.Abs(rootScore) != 30_000)
                {
                    report_to_uci();
                }

                iteration++;
            }
            while (iteration <= maxDepth && 
                   TimeManager.InSoftTimeLimit(rootBestMove) && 
                   TimeManager.NodeCnt < softNodes_);

            return rootBestMove;

        } // fixed SearchStack
    }


    private static void report_to_uci()
    {
        string myScore = score_is_terminal(rootScore) 
            ? $"mate {(Math.Abs(rootScore) - SCORE_MATE) / 2}" : $"cp {rootScore}";

        string pv = get_pv();
                    
        Console.WriteLine(
            $"info depth {iteration} seldepth {seldepth} time {TimeManager.ElapsedMilliseconds()} score {myScore} nodes {TimeManager.NodeCnt} nps {TimeManager.NPS()} pv {pv}"
        );
    }

    
    private static move[][] PV;

    static Search()
    {
        PV = new move[MAX_SEARCH_PLY][];
        for (int i=0; i<PV.Length; i++)
        {
            PV[i] = new move[MAX_SEARCH_PLY];
        }
    }


    private static void push_to_pv(move m, int ply)
    {
        PV[ply][ply] = m;

        for (int i=ply+1; i<MAX_SEARCH_PLY; i++)
        {
            ref move next = ref PV[ply+1][i];
            PV[ply][i] = next;
        }
    }
    

    private static string get_pv()
    {
        string pv = "";
        for (int i=0; i<MAX_SEARCH_PLY && !PV[0][i].IsNull; i++)
        {
            pv += PV[0][i].ToString() + " ";            
        }
        return pv;
    }
    
}
