using static Constants;

public static partial class Search
{
    /// <summary>
    /// Containts the reductions applied to quiet moves
    /// Indexed by Min(movesPlayed, 63)
    /// </summary>
    private static int[] lmrTable;

    /// <summary>
    /// Contains the number of moves that have to be played before
    /// Late Move Pruning prunes quiet moves
    /// Indexed by depth
    /// </summary>
    private static int[] lmpTable;

    public static void init()
    {
        // initialize late-move-reduction values
        lmrTable = new int[64];

        for (int i=0; i<lmrTable.Length; i++)
        {
            lmrTable[i] = 1 + (int)Math.Log(i);
        }

        // initialize late-move-pruning counts
        lmpTable = new int[5];

        for (int i=0; i<lmpTable.Length; i++)
        {
            lmpTable[i] = 2 + i * i;
        }
    }

}
