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
        /* log(i) -> log(depth)*log(i)
        --------------------------------------------------
        Results of dev vs main (8+0.08, NULL, 16MB, UHO_2024_8mvs_big_+105_+124.pgn):
        Elo: 3.38 +/- 3.76, nElo: 5.00 +/- 5.56
        LOS: 96.10 %, DrawRatio: 41.59 %, PairsRatio: 1.04
        Games: 15022, Wins: 4541, Losses: 4395, Draws: 6086, Points: 7584.0 (50.49 %)
        Ptnml(0-2): [388, 1758, 3124, 1802, 439], WL/DD Ratio: 1.47
        LLR: 1.55 (-2.25, 2.89) [0.00, 5.00]
        --------------------------------------------------
        */
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
