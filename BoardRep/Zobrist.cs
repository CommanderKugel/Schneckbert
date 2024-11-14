using Microsoft.VisualBasic;
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

        StmKey = ((ulong) rng.NextInt64() << 32) + ((uint) rng.NextInt64());

        PieceSqKeys = new ulong[2][][];
        for (int c=BLACK; c<=WHITE; c++) 
        {
            PieceSqKeys[c] = new ulong[6][];
            for (int pt=PAWN; pt<=KING; pt++) 
            {
                PieceSqKeys[c][pt] = new ulong[64];
                for (int sq=0; sq<64; sq++) {
                    PieceSqKeys[c][pt][sq] = ((ulong) rng.NextInt64() << 32) + ((uint) rng.NextInt64());
                }
            }
        }

        CastlingKeys = new ulong[4];
        for (int i=0; i<4; i++) 
        {
            CastlingKeys[i] = ((ulong) rng.NextInt64() << 32) + ((uint) rng.NextInt64());
        }

        EpFileKeys = new ulong[8];
        for (int i=0; i<8; i++) 
        {
            EpFileKeys[i] = ((ulong) rng.NextInt64() << 32) + ((uint) rng.NextInt64());
        }
    }
    
    /// <summary>
    /// calculates the ZobristKey of a given position from zero
    /// </summary>
    public static ulong CalcZobrist(pos p) 
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
            if (p.castling_rights[i])
                key ^= CastlingKeys[i];
        }

        if (p.ep != EPSQ_NONE) 
        {
            key ^= EpFileKeys[file_of(p.ep)];
        }

        return key;
    }

    /// <summary>
    /// returns the ZobristKey of a given piece
    /// </summary>
    public static ulong get_piece_key(int color, int pt, int sq)
    {
        return PieceSqKeys[color][pt][sq];
    }

    /// <summary>
    /// returns the ZobristKey of a given castling right
    /// </summary>
    public static ulong get_castling_key(int cr)
    {
        return CastlingKeys[cr];
    }

    /// <summary>
    /// returns the ZobristKey representing the En Passant rights for a given square
    /// </summary>
    public static ulong get_ep_key(int sq)
    {
        return EpFileKeys[file_of(sq)];
    }
}
