using static Constants;

public static class History
{   
    // Main Butterfly History
    // color-from-to
    private static short[][] ButterflyHistory;  // 2 * 64 * 64 

    // Main Piece-To-History
    // color-PieceType-to
    private static short[][] PieceToHistory; // 2 * 6 * 64


    const int PAWN_HIST_SIZE = 256;
    private static short[][][] PawnHistory; // 2 * 256 * 6*64

    public static void init()
    {
        ButterflyHistory = new short[2][];
        ButterflyHistory[BLACK] = new short[64 * 64];
        ButterflyHistory[WHITE] = new short[64 * 64];

        PieceToHistory = new short[2][];
        PieceToHistory[BLACK] = new short[6 * 64];
        PieceToHistory[WHITE] = new short[6 * 64];

        PawnHistory = new short[2][][];
        PawnHistory[BLACK] = new short[PAWN_HIST_SIZE][];
        PawnHistory[WHITE] = new short[PAWN_HIST_SIZE][];
        for (int p=0; p<PAWN_HIST_SIZE; p++)
        {
            PawnHistory[BLACK][p] = new short[6*64];
            PawnHistory[WHITE][p] = new short[6*64];
        }
    }

    /// <summary>
    /// Clears all the History Arrays
    /// </summary>
    public static void Reset()
    {
        Array.Fill(ButterflyHistory[BLACK], (short)0);
        Array.Fill(ButterflyHistory[WHITE], (short)0);

        Array.Fill(PieceToHistory[BLACK], (short)0);
        Array.Fill(PieceToHistory[WHITE], (short)0);

        for (int p=0; p<PAWN_HIST_SIZE; p++)
        {
            Array.Fill(PawnHistory[BLACK][p], (short)0);
            Array.Fill(PawnHistory[WHITE][p], (short)0);
        }
    }

    /// <summary>
    /// returns a reference to the Butterly-History-Value of the given Move
    /// </summary>
    public static ref short getButterflyHistVal(int stm, move m) 
        => ref ButterflyHistory[stm][m.FromTo];

    /// <summary>
    /// returns a reference to the Piece-To-History-Value of the given Piece-Movement
    /// </summary>
    public static ref short getPieceToHistVal(int stm, int pt, int sq) 
        => ref ButterflyHistory[stm][pt * 64 + sq];

    /// <summary>
    /// returns a reference to the Pawn-History-Value of the given Piece-Movement
    /// depending on the current Pawn Structure
    /// </summary>
    public static ref short getPawnHistVal(int stm, ulong key, int pt, int sq)
        => ref PawnHistory[stm][key % PAWN_HIST_SIZE][pt * 64 + sq];

    /// <summary>
    /// Calculates the delta value used to increase or decrease the History Scores of moves
    /// </summary>
    public static short calcHistDelta(int depth) => (short)(depth * depth);

    /// <summary>
    /// Updates the History Value of all moves that were played at this node
    /// Supported Histories: Butterfly, PieceTo
    /// </summary>
    public static unsafe void updateQuietHistValues(Span<move> moves, int lastMoveIdx, int depth, pos p)
    {
        short delta = calcHistDelta(depth);

        for (int i=0; i<lastMoveIdx; i++)
        {
            ref move m = ref moves[i];
            if (p.is_capture(m) || m.IsNull)
            {
                continue;
            }

            getButterflyHistVal(p.us, m) -= delta;
            getPieceToHistVal(p.us, p.piece_on(m.from), m.to) -= delta;
            getPawnHistVal(p.us, p.PawnKey, p.piece_on(m.from), m.to) -= delta;
        }

        ref move mv = ref moves[lastMoveIdx];
        getButterflyHistVal(p.us, mv) += delta;
        getPieceToHistVal(p.us, p.piece_on(mv.from), mv.to) += delta;
        getPawnHistVal(p.us, p.PawnKey, p.piece_on(mv.from), mv.to) += delta;
    }
}
