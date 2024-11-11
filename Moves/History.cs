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

    public static void Reset()
    {
        Array.Fill(ButterflyHistory[0], (short)0);
        Array.Fill(ButterflyHistory[1], (short)0);
    }

    public static ref short getHistVal(int stm, move m)
    {
        return ref ButterflyHistory[stm][m.FromTo];
    }

    public static short calcHistDelta(int depth)
    {
        return (short)(depth * depth);
    }

    public static void updateHistValues(move[] moves, int lastMoveIdx, int depth, int stm)
    {
        short delta = calcHistDelta(depth);
        getHistVal(stm, moves[lastMoveIdx]) += delta;
    }
}
