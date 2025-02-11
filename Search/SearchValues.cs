public static partial class Search
{
    /// <summary>
    /// Containts the reductions applied to quiet moves
    /// Indexed by Min(movesPlayed, 63)
    /// </summary>
    private static int[][] lmrTable;

    /// <summary>
    /// Contains the number of moves that have to be played before
    /// Late Move Pruning prunes quiet moves
    /// Indexed by depth
    /// </summary>
    private static int[] lmpTable;

    public static void init()
    {
        // initialize late-move-reduction values
        lmrTable = new int[64][];

        for (int depth=0; depth<lmrTable.Length; depth++)
        {
            lmrTable[depth] = new int[64];
            for (int moves=0; moves<lmrTable[depth].Length; moves++)
            {   
                lmrTable[depth][moves] = 1 + (int)(Math.Log(depth) * Math.Log(moves) / 1.75);
            }
        }

        // initialize late-move-pruning counts
        lmpTable = new int[5];

        for (int i=0; i<lmpTable.Length; i++)
        {
            lmpTable[i] = 2 + i * i;
        }
    }

}
