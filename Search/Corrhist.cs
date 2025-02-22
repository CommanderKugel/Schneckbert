using static Constants;
using static Utils;

using static System.Math;


public static class CorrHist
{
   
    /*
    CORR_DIV=256, BONUS_DIV=512, MAX~32 -> -5
    CORR_DIV=256, BONUS_DIV=256, MAX~32 -> -3.82
    CORR_DIV=256, BONUS_DIV=128, MAX~32 -> -0.69 & -2 @ 20+0.2
    CORR_DIV=256, BONUS_DIV= 64, MAX~32 -> -2.08

    CORR_DIV=128, BONUS_DIV=128, MAX~32 -> -13.69
    CORR_DIV=512, BONUS_DIV=128, MAX~32 ->  -3.40
    */

    /*
    private const int SIZE  = 16384;

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

    const int PAWN_CORRHIST_WEIGHT   = 1;
    const int FINAL_CORRHIST_DIVISOR = 512;

    const short MAX =  32 * FINAL_CORRHIST_DIVISOR;
    const short MIN = -32 * FINAL_CORRHIST_DIVISOR;

    const int BONUS_DIVISOR = 256;


    public static unsafe void correct_static_eval(ref pos p, SS* ss)
    {      
        int pawnCorrVal = PAWN_CORRHIST_WEIGHT * PawnCorrHist[p.us][p.PawnKey % SIZE];

        ss->StaticEval = ss->RawStaticEval + (pawnCorrVal) / FINAL_CORRHIST_DIVISOR;
    }


    public static unsafe void update_corrhist(ref pos p, SS* ss, int depth, int score)
    {
        int bonus = (score - ss->StaticEval) * depth / BONUS_DIVISOR;

        update_single_corrhist(ref PawnCorrHist[p.us][p.PawnKey % SIZE], bonus);
    }

    private static void update_single_corrhist(ref short entry, int bonus)
    {
        entry += (short)(bonus - Abs(bonus) * entry / 512);
        entry = Clamp(entry, MIN, MAX);
    }
    */
}
