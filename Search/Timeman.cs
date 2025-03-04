using System.Diagnostics;

public static class TimeManager
{
    public static long NodeCnt    = 0;
    public static long QSNodeCnt  = 0;
    public static long TotalNodes = 0;

    public static long[] RootNodeCounts = new long[0b0000_1111_1111_1111];

    private static Stopwatch watch = new Stopwatch();    
    public static void RestartTimer() => watch.Restart();
    public static void StopTimer() => watch.Stop();

    public static long ElapsedMilliseconds() => Math.Max(watch.ElapsedMilliseconds, 1);
    public static int  NPS() => (int)((double)NodeCnt / (double)ElapsedMilliseconds() * 1000.0d);
    
    // time in ms
    private static long HardTimeLimit;
    private static long SoftTimeLimit;

    /// <summary>
    /// Accepts the amount of time Schneckbert has left for the remainder of the whole game as argument
    /// Schedules the time used to pick the next best move automatically.
    /// Used for the "go wtime <A> btime <B>" command
    /// </summary>
    public static void SetNewTimelimit(int totalTime)
    {
        SoftTimeLimit = totalTime / 30;
        HardTimeLimit = totalTime /  5;
        RestartTimer();
    }

    /// <summary>
    /// Accepts the amount of time Schneckbert is supposed to use to pick the next best move.
    /// Used for "go movetime <X>" command
    /// </summary>
    /// <param name="maxtime"></param>
    public static void SetMaxTimelimit(int maxtime)
    {
        SoftTimeLimit = maxtime;
        HardTimeLimit = maxtime;
        RestartTimer();
    }


    private static move lastBestMove = move.NullMove;
    private static int  pvStability  = 0;

    /// <summary>
    /// Returns true, if the softlimit was not exceeded.
    /// The Softlimit is generally lower than the Hardlimit and can be exceeded in between iterations.
    /// Should be used to abord search before starting the next iteration.
    /// </summary>
    public static bool InSoftTimeLimit(move bestMove) 
    {
        // #1 PV/BestMove Stability TM
        //    If for many iterations the best move stays the same, we seem to be
        //    pretty sure that this might be the best move and can spend a little
        //    less time searching.
        pvStability = bestMove == lastBestMove ? Math.Min(10, pvStability+1) : 0;
        double pvStabilityFactor = 1.20d - 0.04d * pvStability;
        
        // #2 Node TM
        //    If the majority of all nodes searched are spent on the best move, all the
        //    other moves seem to be refuted quickly and we need to spent less time. 
        //    If nodes are spent on many different moves, we are rather unsure of the 
        //    best move and need to spent more time searching.
        long bmNodes = RootNodeCounts[bestMove.FromTo];
        double not_bm_nodes_fraction = 1.0d - (double)bmNodes / ((double)NodeCnt + 1);
        double nodeScalingFactor = Math.Max(2.3d * not_bm_nodes_fraction + 0.45d, 0.55d);
        
        return watch.ElapsedMilliseconds < SoftTimeLimit * pvStabilityFactor * nodeScalingFactor;
    }

    /// <summary>
    /// Returns true, if the hardlimit was not exceeded.
    /// The Hardlimit is generally higher than the Softlimit and will never be exceeded.
    /// Should be used to abort search mid-iteration.
    /// </summary>
    public static bool InHardTimeLimit() => watch.ElapsedMilliseconds < HardTimeLimit;

    /// <summary>
    /// Returns true, if the softlimit is not very low.
    /// </summary>
    public static bool HasEnoughTime() => SoftTimeLimit >= 100;

    /// <summary>
    /// Clears all Node Counts and Timelimits.
    /// </summary>
    public static void Reset(bool resetTotalNodes=false)
    {
        TotalNodes = resetTotalNodes ? 0 : TotalNodes + NodeCnt;
        NodeCnt = 0;

        lastBestMove = move.NullMove;
        pvStability  = 0;

        SetNewTimelimit(int.MaxValue); // hotfix for benches
        watch.Reset();
    }
}