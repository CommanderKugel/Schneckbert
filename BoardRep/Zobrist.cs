using System.Runtime.CompilerServices;
using static Constants;
using static Utils;

public static class Zobrist
{
    private static ulong StmKey;
    private static ulong[][][] PieceSqKeys;
    private static ulong[] CastlingKeys;
    private static ulong[] EpFileKeys;


    public static void init() 
    {

        var rng = new Random(67065);
        ulong random_ulong() => ((ulong) rng.NextInt64() << 32) + ((uint) rng.NextInt64());

        StmKey = random_ulong();

        PieceSqKeys = new ulong[2][][];
        for (int c=BLACK; c<=WHITE; c++) 
        {
            PieceSqKeys[c] = new ulong[6][];
            for (int pt=PAWN; pt<=KING; pt++) 
            {
                PieceSqKeys[c][pt] = new ulong[64];
                for (int sq=0; sq<64; sq++) 
                {
                    PieceSqKeys[c][pt][sq] = random_ulong();
                }
            }
        }

        CastlingKeys = new ulong[4];
        for (int i=0; i<4; i++) 
        {
            CastlingKeys[i] = random_ulong();
        }

        EpFileKeys = new ulong[8];
        for (int i=0; i<8; i++) 
        {
            EpFileKeys[i] = random_ulong();
        }
    }
    
    /// <summary>
    /// calculates the ZobristKey of a given position from zero
    /// </summary>
    public static unsafe ulong CalcZobrist(pos p) 
    {
        ulong key = p.us == WHITE ? StmKey : 0;

        for (int c=BLACK; c<=WHITE; c++) 
        {
            for (int pt=PAWN; pt<=KING; pt++) 
            {
                ulong pieces = p.get_pieces(pt, c);
                while (pieces != 0) 
                {
                    int sq = popLsb(ref pieces);
                    key ^= PieceSqKeys[c][pt][sq];
                }
            }
        }

        for (int i=0; i<4; i++) 
        {
            if (p.castlingRights[i])
                key ^= CastlingKeys[i];
        }

        if (p.ep != SQ_NONE) 
        {
            key ^= EpFileKeys[file_of(p.ep)];
        }

        return key;
    }

    /// <summary>
    /// returns the ZobristKey of a given piece
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong get_piece_key(int color, int pt, int sq) => PieceSqKeys[color][pt][sq];

    /// <summary>
    /// returns the ZobristKey of a given castling right
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong get_castling_key(int cr) => CastlingKeys[cr];

    /// <summary>
    /// returns the ZobristKey representing the En-Passant rights for a given square
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong get_ep_key(int sq) => EpFileKeys[file_of(sq)];
    
    /// <summary>
    /// returns the ZobristKey representing the side to move
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong get_stm_key() => StmKey;

}
