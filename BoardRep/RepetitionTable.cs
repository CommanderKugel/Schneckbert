using System.Runtime.CompilerServices;

public static class RepetitionTable
{
    /// <summary>
    /// keeps track of the most recent entry in the Table.
    /// </summary>
    private static int ply = 0;

    /// <summary>
    /// Contains the Zobrist Keys of the Positions that were visited so far.
    /// Requires Pushing new and Popping old keys.
    /// </summary>
    private static ulong[] repTable = new ulong[Constants.MAX_GAME_PLY];

    /// <summary>
    /// Completely clears the repetition table.
    /// </summary>
    public static void Reset()
    {
        Array.Clear(repTable);
        ply = 0;
    }

    /// <summary>
    /// Inserts a new Zobrist-Key into the Repetitiontable.
    /// The position can now cause a two-fold-repetition-detection.
    /// </summary>
    public static void Push(ulong key) => repTable[ply++] = key;

    /// <summary>
    /// Removes the last entry from the Repetitiontable.
    /// The last position can no longer cause a two-fold-repetition
    /// </summary>
    public static void Pop() => repTable[ply--] = 0;

    /// <summary>
    /// Probes the Repetition Table for a 2-fold repetition.
    /// Returns true, if the Zobrist-Key of the position is already contained in the Table.
    /// </summary>
    public static bool IsRepeatedPosition(pos p)
    {
        // Just one full-move can not cause a repetition, so we take a quick exit. 
        if (p.FiftyMoveCnt < 4) 
        {
            return false;
        }

        // If the fifts-move-rule-counter is updated, a position can no longer repeat itself,
        // so we dont need to check further than the fifty-move-counter.
        int earliesPossibleRepetition = Math.Max(ply-p.FiftyMoveCnt, 0);

        // Also, a Repetition can only occur after every full move,
        // so we only need to check every two half-moves -> i-=2
        for (int i = ply-2; i >= earliesPossibleRepetition; i -= 2) 
        { 
            if (repTable[i] == p.ZobristKey)
            {
                return true;
            }
        }
        return false;
    }
}
