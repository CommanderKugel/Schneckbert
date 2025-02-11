using System.Runtime.CompilerServices;
using static Constants;

public static class History
{   
    // Main Butterfly History
    // color-from-to
    private static short[][] ButterflyHistory; // 2 * 64 * 64 

    // Main Piece-To-History
    // color-PieceType-to
    private static short[][] PieceToHistory; // 2 * 6 * 64

    /// <summary>
    /// determines the size fot the Pawn-History-Table
    /// </summary>
    private const int PAWN_HIST_SIZE = 256;

    // Secondary Piece-To-History
    // color-PawnKey-PieceType-to
    private static short[][][] PawnHistory; // 2 * 256 * 6*64

    // Primary Capture ButterflyHistory
    // color-Victim-Attacker-from-to
    private static short[][][] CaptureHistory; // 2 * 5*6 * 64*64

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

        CaptureHistory = new short[2][][];
        CaptureHistory[BLACK] = new short[6*6][];
        CaptureHistory[WHITE] = new short[6*6][];
        for (int i=0; i<6*6; i++)
        {
            CaptureHistory[BLACK][i] = new short[64*64];
            CaptureHistory[WHITE][i] = new short[64*64];
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

        for (int i=0; i<6*6; i++)
        {
            Array.Fill(CaptureHistory[BLACK][i], (short)0);
            Array.Fill(CaptureHistory[WHITE][i], (short)0);
        }
    }

    /// <summary>
    /// returns a reference to the Butterly-History-Value of the given Move
    /// </summary>
    public static ref short get_butterfly_histval(int stm, move m) 
        => ref ButterflyHistory[stm][m.FromTo];

    /// <summary>
    /// returns a reference to the Piece-To-History-Value of the given Piece-Movement
    /// </summary>
    public static ref short get_pieceTo_histval(int stm, int pt, int sq) 
        => ref ButterflyHistory[stm][pt * 64 + sq];

    /// <summary>
    /// returns a reference to the Pawn-History-Value of the given Piece-Movement
    /// depending on the current Pawn Structure
    /// </summary>
    public static ref short get_pawnhist_val(int stm, ulong key, int pt, int sq)
        => ref PawnHistory[stm][key % PAWN_HIST_SIZE][pt * 64 + sq];


    /// <summary>
    /// Returns a reference to the Capture-History-Value of the given capture
    /// Depends on butterfly histories and the moving and captured pieceTypes
    /// </summary>
    public static ref short get_capthist_val(int stm, int vict, int att, move m)
        => ref CaptureHistory[stm][att * 6 + vict][m.FromTo];


    /// <summary>
    /// Calculates the delta value used to increase or decrease the History Scores of moves
    /// </summary>
    public static short calcHistDelta(int depth) => (short)(depth * depth);

    /// <summary>
    /// Value used for History-gravity formula
    /// </summary>
    private const int HISTORY_DIVISOR = 512;
    
    /// <summary>
    /// Decreases the given History-Value for bad moves.
    /// The smaller the value already is, the less it is decreased
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void decreaseHistVal(int delta, ref short value)
        => value = (short)(value - delta - delta*value/HISTORY_DIVISOR);

    /// <summary>
    /// Increases the given History-Value for good moves.
    /// The bigger the value already is, the less it is decreased
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void increaseHistVal(int delta, ref short value)
        => value = (short)(value + delta - delta*value/HISTORY_DIVISOR);

    /// <summary>
    /// Updates the History Value of all moves that were played at this node
    /// Supported Histories: Butterfly, PieceTo, Pawn-PieceTo
    /// </summary> 
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe void decreaseQuietHistValues(ref Span<move> quiets, int lastIdx, int delta, ref pos p)
    {
        for (int i=0; i<lastIdx; i++)
        {
            ref move m = ref quiets[i];
            decreaseHistVal(delta, ref get_butterfly_histval(p.us, m));
            decreaseHistVal(delta, ref get_pieceTo_histval(p.us, p.piece_on(m.from), m.to));
            decreaseHistVal(delta, ref get_pawnhist_val(p.us, p.PawnKey, p.piece_on(m.from), m.to));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe void decreaseCaptureHistValues(ref Span<move> capts, int lastIdx, int delta, ref pos p)
    {
        for (int i=0; i<lastIdx; i++)
        {
            ref move m = ref capts[i];
            int attacker = p.piece_on(m.from);
            int victim   = p.get_captured_pt(m);
            decreaseHistVal(delta, ref get_capthist_val(p.us, victim, attacker, m));
        }
    }

    public static unsafe void increaseSingleHistValue(move m, int delta, ref pos p, bool isCapture)
    {
        if (isCapture)
        {
            int attacker = p.piece_on(m.from);
            int victim   = p.get_captured_pt(m);
            increaseHistVal(delta, ref get_capthist_val(p.us, victim, attacker, m));
        }
        else
        {
            int pt = p.piece_on(m.from);
            increaseHistVal(delta, ref get_butterfly_histval(p.us, m));
            increaseHistVal(delta, ref get_pieceTo_histval(p.us, pt, m.to));
            increaseHistVal(delta, ref get_pawnhist_val(p.us, p.PawnKey, pt, m.to));
        }
    }
}
