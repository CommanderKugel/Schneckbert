public static class RepetitionTable
{
    private static int ply = 0;
    private static ulong[] repTable = new ulong[Constants.MAX_GAME_PLY];

    /// <summary>
    /// completely clears the repetition table
    /// </summary>
    public static void Reset()
    {
        Array.Clear(repTable);
        ply = 0;
    }

    /// <summary>
    /// inserts a new zobrist key into the Repetitiontable
    /// the position can cause a two-fold-repetition-detection afterwards
    /// </summary>
    public static void Push(ulong key) 
    {
        repTable[ply++] = key;
    }

    /// <summary>
    /// removes the last entry in the Repetitiontable
    /// the last position can not cause a repetition anymore
    /// </summary>
    public static void Pop()
    {
        repTable[ply--] = 0;
    }

    /// <summary>
    /// probes the Repetition Table for a 2-fold repetition
    /// Repetitions are detected by comparison of zobrist hashes
    /// </summary>
    public static bool IsRepeatedPosition(pos p)
    {
        if (p.FiftyMoveCnt < 4) 
        {
            return false;
        }
        int x = Math.Max(ply-p.FiftyMoveCnt, 0);
        for (int i=ply-2; i>=x; i-=2) 
        {
            if (repTable[i] == p.ZobristKey)
            {
                return true;
            }
        }
        return false;
    }
}
