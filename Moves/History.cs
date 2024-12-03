using System.Diagnostics.Metrics;
using static Constants;

public static class History
{   
    // Main Butterfly History
    // side-from-to
    private static short[][] ButterflyHistory;  // 2 * 64 * 64 

    public static void init()
    {
        ButterflyHistory = new short[2][];
        ButterflyHistory[BLACK] = new short[64 * 64];
        ButterflyHistory[WHITE] = new short[64 * 64];
    }

    /// <summary>
    /// Clears all the History Arrays
    /// </summary>
    public static void Reset()
    {
        Array.Fill(ButterflyHistory[0], (short)0);
        Array.Fill(ButterflyHistory[1], (short)0);
    }

    /// <summary>
    /// returns the Butterly-History-Value of the given Move
    /// </summary>
    public static ref short getButterflyHistVal(int stm, move m) => ref ButterflyHistory[stm][m.FromTo];
    /// <summary>
    /// Calculates the delta value used to increase or decrease the History Scores of moves
    /// </summary>
    public static short calcHistDelta(int depth) => (short)(depth * depth);

    /// <summary>
    /// Updates the History Value of all moves that were played at this node
    /// </summary>
    public static unsafe void updateQuietHistValues(move[] moves, int lastMoveIdx, int depth, pos p, SS* ss)
    {
        short delta = calcHistDelta(depth);

        for (int i=0; i<lastMoveIdx; i++)
        {
            ref move m = ref moves[i];
            if (p.piece_on(m.to) != PIECE_TYPE_NONE)
                continue;

            int pt = p.piece_on(m.from);
            getButterflyHistVal(p.us, m) -= delta;
        }

        ref move mv = ref moves[lastMoveIdx];
        getButterflyHistVal(p.us, mv) += delta;
    }
}
