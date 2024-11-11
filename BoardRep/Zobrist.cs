using Microsoft.VisualBasic;
using static Constants;
using static Utils;

public static class Zobrist
{
    public static ulong StmKey;
    public static ulong[][][] PieceSqKeys;
    public static ulong[] CastlingKeys;
    public static ulong[] EpFileKeys;


    public static void init() 
    {
        var rng = new Random(67065);

        StmKey = ((ulong) rng.NextInt64() << 32) + ((uint) rng.NextInt64());

        PieceSqKeys = new ulong[2][][];
        for (int c=BLACK; c<=WHITE; c++) {
            PieceSqKeys[c] = new ulong[6][];
            for (int pt=PAWN; pt<=KING; pt++) {
                PieceSqKeys[c][pt] = new ulong[64];
                for (int sq=0; sq<64; sq++) {
                    PieceSqKeys[c][pt][sq] = ((ulong) rng.NextInt64() << 32) + ((uint) rng.NextInt64());
                }
            }
        }

        CastlingKeys = new ulong[4];
        for (int i=0; i<4; i++) {
            CastlingKeys[i] = ((ulong) rng.NextInt64() << 32) + ((uint) rng.NextInt64());
        }

        EpFileKeys = new ulong[8];
        for (int i=0; i<8; i++) {
            EpFileKeys[i] = ((ulong) rng.NextInt64() << 32) + ((uint) rng.NextInt64());
        }
    }
    

    public static ulong CalcZobrist(pos p) {
        ulong key = p.us == WHITE ? StmKey : 0;

        for (int c=BLACK; c<=WHITE; c++) {
            for (int pt=PAWN; pt<=KING; pt++) {
                ulong pieces = p.get_pieces(pt, c);
                while (pieces != 0) {
                    int sq = popLsb(ref pieces);
                    key ^= PieceSqKeys[c][pt][sq];
                }
            }
        }

        for (int i=0; i<4; i++) {
            if (p.castling_rights[i])
                key ^= CastlingKeys[i];
        }

        if (p.ep != EPSQ_NONE) {
            key ^= EpFileKeys[file_of(p.ep)];
        }

        return key;
    }
}
