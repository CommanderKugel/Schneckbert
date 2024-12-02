using System.Diagnostics.Metrics;
using static Constants;

public static class History
{   
    // Main Butterfly History
    // side-from-to
    private static short[][] ButterflyHistory;  // 2 * 64 * 64 

    // Main Capture History
    // side-victim-attacker-to
    private static short[][][] CaptureHistory;  // 2 * 6 * 6*64

    // counter-history
    // side-lastPiece-lastTo-piece-to
    private static short[][][] CounterHistory;  // 2 * 6*64 * 6*64

    public const int CONT_HIST_BACKWARDS_SIZE = 1;

    public static void init()
    {
        ButterflyHistory = new short[2][];
        ButterflyHistory[BLACK] = new short[64 * 64];
        ButterflyHistory[WHITE] = new short[64 * 64];

        CaptureHistory    = new short[2][][]; // color
        CaptureHistory[0] = new short[5][];   // victimPieceType
        CaptureHistory[1] = new short[5][];

        CounterHistory    = new short[2][][];    // color
        CounterHistory[0] = new short[6 * 64][]; // last movingPieceType & to
        CounterHistory[1] = new short[6 * 64][];

        for (int pt=PAWN; pt<KING; pt++)
        {
            CaptureHistory[0][pt] = new short[6 * 64]; // attackerPieceType & to
            CaptureHistory[1][pt] = new short[6 * 64];
        }

        for (int i=0; i<6*64; i++)
        {
            CounterHistory[0][i] = new short[6 * 64]; // movingPieceType & to
            CounterHistory[1][i] = new short[6 * 64];
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

        for (int i=0; i<6*64; i++)
        {
            Array.Fill(CounterHistory[0][i], (short)0);
            Array.Fill(CounterHistory[1][i], (short)0);
        }
    }

    /// <summary>
    /// returns the Butterly-History-Value of the given Move
    /// </summary>
    public static ref short getButterflyHistVal(int stm, move m) => ref ButterflyHistory[stm][m.FromTo];

    /// <summary>
    /// returns the Capture-History-Value of the given capture
    /// </summary>
    public static ref short getCaptureHistVal(int stm, int att, int vic, int sq) => ref CaptureHistory[stm][vic][att*64 + sq];
    public static ref short getCaptureHistVal(pos p, move m) => ref CaptureHistory[p.us][m.IsEp ? PAWN : p.piece_on(m.to)][p.piece_on(m.from)*64 + m.from];


    /// <summary>
    /// returns an array, arrays are reference-types in C#, therefor this returns a reference
    /// should be stored in the next ss-entry of the upcoming ply
    /// should be used in next ply with getCounterHistVal()
    /// </summary>
    public static short[] getCounterHistReference(int color, int movingPieceType, int to) 
    {
        if (movingPieceType != PIECE_TYPE_NONE)
            return CounterHistory[color][movingPieceType * 64 + to];
        else
            return CounterHistory[color][0];
    } 

    /// <summary>
    /// returns a reference to the corresponding CounterHistory's value
    /// the CounterHistory must already be provided
    /// </summary>
    public static ref short getCounterHistVal(ref short[] counterHist, int pt, int to) => ref counterHist[pt * 64 + to];

    /// <summary>
    /// Calculates the delta value used to increase or decrease the History Scores of moves
    /// </summary>
    public static short calcHistDelta(int depth) => (short)(depth * depth);

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
