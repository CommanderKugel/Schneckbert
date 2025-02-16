using static Constants;
using static Utils;

using static System.Math;


public static class CorrHist
{
    /*
    private const int SIZE  = 16384;
    
    private const short GRAIN = 256;
    private const short MAX = 32 * GRAIN;
    private const short MIN = -MAX;
    
    bugifx 
    GRAIN=1024, SCALE=1024, MAX=128*GRAIN
    newWeight = Min(depth * depth + depth + 1, 128)
    --------------------------------------------------
    Elo:  1.39 +/- 5.36, LLR: -0.03 (-2.25, 2.89) [0.00, 5.00] @  8+0.08
    Elo: -2.66 +/- 4.19, LLR: -2.26 (-2.25, 2.89) [0.00, 5.00] @ 20+0.20
    --------------------------------------------------

    GRAIN=256, MAX=32*GRAIN
    bonus = (score-eval) * depth/64
    classical Gravity formula
    --------------------------------------------------
    Elo:  1.05 +/- 7.69, LLR: -0.12 (-2.25, 2.89) [0.00, 5.00] @  8+0.08
    Elo: -1.60 +/- 4.71, LLR: -1.69 (-2.25, 2.89) [0.00, 5.00] @ 20+0.20 
    --------------------------------------------------
    */

    /*
    private static short[][] PawnCorrHist;


    static CorrHist()
    {
        PawnCorrHist = new short[2][];
        PawnCorrHist[WHITE] = new short[SIZE];
        PawnCorrHist[BLACK] = new short[SIZE];
    }

    public static void Reset()
    {
        Array.Clear(PawnCorrHist[BLACK]);
        Array.Clear(PawnCorrHist[WHITE]);
    }

    public static unsafe void correct_static_eval(ref pos p, SS* ss)
    {
        int pawnCorrVal = PawnCorrHist[p.us][p.PawnKey % SIZE];

        ss->StaticEval = ss->RawStaticEval + pawnCorrVal / GRAIN;
    }

    public static unsafe void update_corrhist(ref pos p, SS* ss, int depth, int score)
    {
        int bonus = (score - ss->StaticEval) * depth / 64;

        ref short pawnCorrEntry = ref PawnCorrHist[p.us][p.PawnKey % SIZE];

        update_single_corrhist(ref pawnCorrEntry, bonus);
    }

    private static void update_single_corrhist(ref short entry, int bonus)
    {
        entry += (short)(bonus - Abs(bonus) * entry / GRAIN);
        entry = Clamp(entry, MIN, MAX);
    }
    */
}
