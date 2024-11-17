using static Constants;

public static class Board
{
    public const int MAX_GAME_LENGTH = 1024;
    public const int MAX_SEARCH_DEPTH = 128;

    private static byte[] Mailbox;


    public static void init()
    {
        Mailbox = new byte[64];
    }

    public static void Reset()
    {
        Array.Fill(Mailbox, (byte) 0);
    }

    /// <summary>
    /// returns the Piece on the given Square
    /// Pieces contain a PieceType and a Color
    /// </summary>
    public static int piece_on(int sq)
    {
        return Mailbox[sq];
    }

    /// <summary>
    /// returns the PieceType of the Piece on the given Square
    /// </summary>
    public static int pieceType_on(int sq)
    {
        return pt_of(Mailbox[sq]);
    }
    /// <summary>
    /// returns the Color of the Piece on the given Square
    /// </summary>
    public static int color_on(int sq)
    {
        return Mailbox[sq] == PIECE_NONE ? COLOR_NONE : color_of(Mailbox[sq]);
    }

    public static void set_piece(byte piece, int sq)
    {
        Mailbox[sq] = piece;
    }

    public static void set_piece(int pt, int color, int sq)
    {
        Mailbox[sq] = make_piece(pt, color);
    }

}
