using static Constants;

public static class History
{
    private static short[][] ButterflyHistory;

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
    /// returns the HistoryValue of the given Move
    /// </summary>
    public static ref short getHistVal(int stm, move m)
    {
        return ref ButterflyHistory[stm][m.FromTo];
    }

    /// <summary>
    /// Calculates the delta value used to increase or decrease the History Scores of moves
    /// </summary>
    public static short calcHistDelta(int depth)
    {
        return (short)(depth * depth);
    }

    /// <summary>
    /// Updates the History Value of the move that caused the beta cutoff
    /// </summary>
    public static void updateHistValues(move[] moves, int lastMoveIdx, int depth, int stm)
    {
        short delta = calcHistDelta(depth);
        getHistVal(stm, moves[lastMoveIdx]) += delta;
    }
}
