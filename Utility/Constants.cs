
using System.Runtime.CompilerServices;

public static class Constants
{

    public const int    // Piece Types
        PAWN   = 0,     // 0b000
        KNIGHT = 1,     // 0b001
        BISHOP = 2,     // 0b010
        ROOK   = 3,     // 0b011
        QUEEN  = 4,     // 0b100
        KING   = 5,     // 0b101
        PIECE_TYPE_NONE = 6; // 0b110

    public const int    // Colors
        WHITE = 1,
        BLACK = 0,
        COLOR_NONE = 2;

    public const int    // Pieces
        WHITE_PAWN   = (PAWN   << 1) + WHITE,
        WHITE_KNIGHT = (KNIGHT << 1) + WHITE,
        WHITE_BISHOP = (BISHOP << 1) + WHITE,
        WHITE_ROOK   = (ROOK   << 1) + WHITE,
        WHITE_QUEEN  = (QUEEN  << 1) + WHITE,
        WHITE_KING   = (KING   << 1) + WHITE,
        BLACK_PAWN   = (PAWN   << 1) + BLACK,
        BLACK_KNIGHT = (KNIGHT << 1) + BLACK,
        BLACK_BISHOP = (BISHOP << 1) + BLACK,
        BLACK_ROOK   = (ROOK   << 1) + BLACK,
        BLACK_QUEEN  = (QUEEN  << 1) + BLACK,
        BLACK_KING   = (KING   << 1) + BLACK,
        PIECE_NONE   =  PIECE_TYPE_NONE;

    public static int pt_of(int piece) => piece >> 1;
    public static int color_of(int piece) => piece & 1;
    public static byte make_piece(int pt, int color) => (byte)((pt << 1) | color);

    
    public const byte   // TT Flags
        BOUND_UPPER = 0,
        BOUND_LOWER = 1,
        BOUND_EXACT = 2;
        
    /// <summary>
    /// the maximum amount of legal moves known in a position is 213
    /// </summary>
    public const int MAX_MOVE_CNT = 213;

    public const int SCORE_MATE = 2_000_000_000;
    public const int EVAL_SCORE_MAX  = 1_000_000_000;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool score_is_terminal(int score) => Math.Abs(score) > EVAL_SCORE_MAX;

    public const int MAX_SEARCH_PLY = 128;
    public const int MAX_GAME_PLY = 1024;

    /// <summary>
    /// enumerates all squares to give them names
    /// </summary>
    public const byte 
        A1= 0, B1= 1, C1=2 , D1= 3, E1= 4, F1= 5, G1= 6, H1= 7, 
        A2= 8, B2= 9, C2=10, D2=11, E2=12, F2=13, G2=14, H2=15,   
        A3=16, B3=17, C3=18, D3=19, E3=20, F3=21, G3=22, H3=23, 
        A4=24, B4=25, C4=26, D4=27, E4=28, F4=29, G4=30, H4=31, 
        A5=32, B5=33, C5=34, D5=35, E5=36, F5=37, G5=38, H5=39, 
        A6=40, B6=41, C6=42, D6=43, E6=44, F6=45, G6=46, H6=47, 
        A7=48, B7=49, C7=50, D7=51, E7=52, F7=53, G7=54, H7=55, 
        A8=56, B8=57, C8=58, D8=59, E8=60, F8=61, G8=62, H8=63,
        SQ_NONE = 255;

    public const ulong
        Rank1 = 0xFFul <<  0,
        Rank2 = 0xFFul <<  8,
        Rank3 = 0xFFul << 16,
        Rank4 = 0xFFul << 24,
        Rank5 = 0xFFul << 32,
        Rank6 = 0xFFul << 40,
        Rank7 = 0xFFul << 48,
        Rank8 = 0xFFul << 56,
        FileA = 0x0101_0101_0101_0101ul << 0,
        FileB = 0x0101_0101_0101_0101ul << 1,
        FileC = 0x0101_0101_0101_0101ul << 2,
        FileD = 0x0101_0101_0101_0101ul << 3,
        FileE = 0x0101_0101_0101_0101ul << 4,
        FileF = 0x0101_0101_0101_0101ul << 5,
        FileG = 0x0101_0101_0101_0101ul << 6,
        FileH = 0x0101_0101_0101_0101ul << 7;

    /// <summary>
    /// provides the rank of the square-index
    /// equal to sq / 8
    /// </summary>
    public static int rank_of(int sq) => sq >> 3;   // same as sq / 8
    /// <summary>
    /// provides the rank of the square-index
    /// equal to sq % 8
    /// </summary>
    public static int file_of(int sq) => sq  & 7;   // same as sq % 8
    /// <summary>
    /// provides the rank of the square-index but flips it for blacks POV
    /// this is necessary for the distancy of passed pawns until promotion
    /// e.g. the square a8 would return the relative rank 0
    /// </summary>
    public static int relative_rank_of(int sq, int c) 
    {
        int rank = rank_of(sq);
        return c == WHITE ? rank : 7-rank;
    } 
    /// <summary>
    /// provides the file of the square-index but flips it for pieces on the right flank
    /// e.g. the square h1 would return the relative file 0
    /// </summary>
    public static int relative_file_of(int sq) 
    {
        int file = file_of(sq);
        return file < 4 ? file : file ^ 7;
    } 

    public const string startpos = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    /// <summary>
    /// maps a square index to its string representation
    /// starting at 0 = "a1" and ending at 63 = "h8"
    /// </summary>
    public static readonly string[] BoardNotation = {
        "a1", "b1", "c1", "d1", "e1", "f1", "g1", "h1", 
        "a2", "b2", "c2", "d2", "e2", "f2", "g2", "h2", 
        "a3", "b3", "c3", "d3", "e3", "f3", "g3", "h3",
        "a4", "b4", "c4", "d4", "e4", "f4", "g4", "h4",
        "a5", "b5", "c5", "d5", "e5", "f5", "g5", "h5",
        "a6", "b6", "c6", "d6", "e6", "f6", "g6", "h6",
        "a7", "b7", "c7", "d7", "e7", "f7", "g7", "h7",
        "a8", "b8", "c8", "d8", "e8", "f8", "g8", "h8",
    };

    public readonly static string[] PieceChars = { "pnbrqk-", "PNBRQK-" };

    /// <summary>
    /// contains the natural logarithm of all numbers up to 254.
    /// ln(0) was replaced with 0
    /// </summary>
    public static readonly int[] ln = {
        0, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 
        3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
    };

}

