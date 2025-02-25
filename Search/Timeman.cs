using System.Diagnostics;

public static class TimeManager
{
    public static long NodeCnt    = 0;
    public static long QSNodeCnt  = 0;
    public static long TotalNodes = 0;

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
        pvStability = bestMove == lastBestMove ? Math.Min(10, pvStability+1) : 0;
        double pvStabilityFactor = 1.20d - 0.04d * pvStability;
        
        //long nonPVNodes = NodeCnt - Search.rootPVNodes;
        //double nodeFactor = Math.Clamp(2*nonPVNodes/NodeCnt + 0.5, 0.75, 1.5);
        
        return watch.ElapsedMilliseconds < SoftTimeLimit * pvStabilityFactor;
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