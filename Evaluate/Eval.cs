using static Constants;
using static Utils;

using static Pesto;
using static EvalUtils;
using static Attacks;

public static class Evaluation
{
    /// <summary>
    /// packs tapered evaluation terms into a single 32-bit integer
    /// </summary>
    public static int S(int mg, int eg) => (mg << 16) + eg;
    /// <summary>
    /// extracts the mg value from a packed score
    /// </summary>
    public static int get_mg(int score) => (short)(score >> 16);
    /// <summary>
    /// extracts the eg value from a packed score
    /// </summary>
    public static int get_eg(int score) => (short)score;

    
    private static readonly int[][] PestoTables = { 
        pawn_table, knight_table, bishop_table, rook_table, queen_table, king_table 
    };

    private static readonly int[] PhaseValues = { 
        0, 1, 1, 2, 4, 0 
    };

    private static readonly int[] Material = { 
        S(82, 94), S(337, 281), S(365, 297), S(477, 512), S(1025, 936), S(20_000, 20_000) 
    };


    
    /// <summary>
    /// evaluates a position and returns a 32-bit Integer
    /// Positive numbers suggest WHITE has the advantage, negative BLACK's
    /// </summary>
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
                    int relSq = us==WHITE ? sq : sq ^ 56;
                    phase += PhaseValues[pt];

                    // Material
                    eval  += Material[pt];

                    // PSQT
                    eval  += PestoTables[pt][relSq];
                    
                }
            }
            eval = -eval;
        }
        // handle early promotions
        phase = Math.Min(phase, 24); 

        return (get_mg(eval) * phase + get_eg(eval) * (24 - phase)) / (p.us == WHITE ? 24 : -24);
    }
}
