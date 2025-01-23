using static Constants;

using System.Runtime.CompilerServices;

public static partial class Search
{
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
}
