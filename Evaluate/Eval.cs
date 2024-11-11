using static Constants;
using static Utils;
using static Pesto;

public static class Evaluation
{
    public static int S(int mg, int eg) => (mg << 16) + eg;
    
    private static readonly int[][] PestoTables = { 
        pawn_table, knight_table, bishop_table, rook_table, queen_table, king_table 
    };

    private static readonly int[] phaseValues = { 
        0, 1, 1, 2, 4, 0 
    };

    private static readonly int[] material = { 
        S(82, 94), S(337, 281), S(365, 297), S(477, 512), S(1025, 936), S(20_000, 20_000) 
    };
    

    public static int Evaluate(pos p) 
    {
        int eval = 0;
        int phase = 0;

        for (int us=WHITE; us>=BLACK; us--) 
        {
            for (int pt=PAWN; pt<=KING; pt++) 
            {
                ulong pieces = p.get_pieces(pt, us);
                while (pieces != 0) 
                {
                    int sq = popLsb(ref pieces);
                    if (us==BLACK) sq ^= 56;

                    // Material
                    eval  += material[pt];

                    // PSQT
                    eval  += PestoTables[pt][sq];

                    phase += phaseValues[pt];
                }
            }
            eval = -eval;
        }
        phase = Math.Min(phase, 24); // handle early promotions
        return ((eval >> 16) * phase + ((short)eval >> 16) * (24 - phase)) / (p.us == WHITE ? 24 : -24);
    }
}
