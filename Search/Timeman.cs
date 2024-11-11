using System.Diagnostics;

public static class TimeManager
{
    public static long NodeCnt = 0;
    public static long TotalNodes = 0;

    private static Stopwatch watch = new Stopwatch();    
    public static void   RestartTimer() => watch.Restart();

    public static long ElapsedMilliseconds() => Math.Max(watch.ElapsedMilliseconds, 1);
    public static int  NPS() => (int)((double)NodeCnt / (double)ElapsedMilliseconds() * 1000.0d);
    
    // time in ms
    private static long HardTimeLimit;
    private static long SoftTimeLimit;

    public static void SetNewTimelimit(int totalTime)
    {
        SoftTimeLimit = totalTime / 30;
        HardTimeLimit = totalTime /  5;
        RestartTimer();
    }

    public static bool InSoftTimeLimit() => watch.ElapsedMilliseconds < SoftTimeLimit;
    public static bool InHardTimeLimit() => watch.ElapsedMilliseconds < HardTimeLimit;

    public static void Reset(bool resetTotalNodes=false)
    {
        TotalNodes = resetTotalNodes ? 0 : TotalNodes + NodeCnt;
        NodeCnt = 0;
        SetNewTimelimit(int.MaxValue); // hotfix for benches
        watch.Reset();
    }
}