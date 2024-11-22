using static Constants;

public static class History
{
    // from-to
    private static short[][] ButterflyHistory;
    // victim-to-attacker
    private static short[][][] CaptureHistory;

    public static void init()
    {
        ButterflyHistory = new short[2][];
        ButterflyHistory[BLACK] = new short[64 * 64];
        ButterflyHistory[WHITE] = new short[64 * 64];

        CaptureHistory = new short[2][][];
        CaptureHistory[0] = new short[5][];
        CaptureHistory[1] = new short[5][];
        for (int pt=PAWN; pt<KING; pt++)
        {
            CaptureHistory[0][pt] = new short[6 * 64];
            CaptureHistory[1][pt] = new short[6 * 64];
        }
    }

    /// <summary>
    /// Clears all the History Arrays
    /// </summary>
    public static void Reset()
    {
        Array.Fill(ButterflyHistory[0], (short)0);
        Array.Fill(ButterflyHistory[1], (short)0);

        for (int pt=PAWN; pt<KING; pt++)
        {
            Array.Fill(CaptureHistory[0][pt], (short)0);
            Array.Fill(CaptureHistory[1][pt], (short)0);
        }
        
    }

    /// <summary>
    /// returns the Butterly-History-Value of the given Move
    /// </summary>
    public static ref short getButterflyHistVal(int stm, move m) => ref ButterflyHistory[stm][m.FromTo];

    /// <summary>
    /// returns the Capture-History-Value of the given capture
    /// </summary>
    public static ref short getCaptureHistVal(int stm, int att, int vic, int sq) => ref CaptureHistory[stm][vic][att * sq];

    /// <summary>
    /// Calculates the delta value used to increase or decrease the History Scores of moves
    /// </summary>
    public static short calcHistDelta(int depth)
    {
        return (short)(depth * depth);
    }

    /// <summary>
    /// Updates the History Value of all moves that were played at this node
    /// </summary>
    public static void updateQuietHistValues(move[] moves, int lastMoveIdx, int depth, pos p)
    {
        short delta = calcHistDelta(depth);
        int victim;

        for (int i=0; i<lastMoveIdx; i++)
        {
            ref move m = ref moves[i];
            victim = p.piece_on(m.to);

            if (victim == PIECE_TYPE_NONE)
            {
                getButterflyHistVal(p.us, m) -= delta;
            }
            else
            {
               getCaptureHistVal(p.us, p.piece_on(m.from), victim, m.to) -= delta;
            }
        }

        ref move mv = ref moves[lastMoveIdx];
        victim = p.piece_on(mv.to);
        if (victim == PIECE_TYPE_NONE)
        {
            getButterflyHistVal(p.us, mv) += delta;
        }
        else
        {
           getCaptureHistVal(p.us, p.piece_on(mv.from), victim, mv.to) += delta;
        }
    }

    public static void updateCaptureHistValues(move[] moves, int lastMoveIdx, int depth, pos p)
    {
        short delta = calcHistDelta(depth);
        int victim;

        for (int i=0; i<lastMoveIdx; i++)
        {
            ref move m = ref moves[i];
            victim = p.piece_on(m.to);

            if (victim == PIECE_TYPE_NONE)
            {   
                // not TT Move -> Stage Quiets
                if (i > 0)
                {
                    break;
                }
                continue;
            }

            getCaptureHistVal(p.us, p.piece_on(m.from), victim, m.to) -= delta;
        }

        ref move mv = ref moves[lastMoveIdx];
        victim = p.piece_on(mv.to);
        getCaptureHistVal(p.us, p.piece_on(mv.from), victim, mv.to) += delta;
    }
    
}
