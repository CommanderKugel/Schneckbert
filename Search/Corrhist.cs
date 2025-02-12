using static Constants;
using static Utils;

using static System.Math;


public static class CorrHist
{
    private const int SIZE  = 16384;
    
    private const int GRAIN = 1024;
    private const int SCALE = 1024;
    private const int MAX   = 128 * GRAIN;
    
    /* 
    GRAIN=1024, SCALE=1024, MAX=128*GRAIN
    newWeight = Min(depth * depth + depth + 1, 128)
    --------------------------------------------------
    Elo: -5.88 +/- 6.55, LLR: -2.26 (-2.25, 2.89) [0.00, 5.00]
    --------------------------------------------------

    GRAIN=1024, SCALE=1024, MAX=64*GRAIN
    newWeight = Min(depth * depth + depth + 1, 128)
    --------------------------------------------------
    Elo: -9.67 +/- 8.16, LLR: -2.26 (-2.25, 2.89) [0.00, 5.00]
    --------------------------------------------------

    GRAIN=1024, SCALE=1024, MAX=64*GRAIN
    newWeight = Min(depth + 1, 16)
    --------------------------------------------------
    Elo: -17.55 +/- 10.33, LLR: -2.26 (-2.25, 2.89) [0.00, 5.00]
    --------------------------------------------------

    GRAIN=1024, SCALE=1024, MAX=32*GRAIN
    newWeight = Min(depth * depth + depth + 1, 128)
    --------------------------------------------------
    Elo: -60.72 +/- 18.35, LLR: -2.25 (-2.25, 2.89) [0.00, 5.00]
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

    public static unsafe void update_corrhist(ref pos p, SS* ss, int depth, int delta)
    {
        ref short pawnCorrEntry = ref PawnCorrHist[p.us][p.PawnKey % SIZE];
        int scaledDelta = delta * GRAIN;
        int newWeight   = Min(depth * depth + depth + 1, 128);

        update_single_corrhist(ref pawnCorrEntry, newWeight, scaledDelta);
    }

    private static void update_single_corrhist(ref short entry, int newWeight, int scaledDelta)
    {
        int update = entry * (SCALE - newWeight) - scaledDelta * newWeight;
        entry = (short)Clamp(update / SCALE, -MAX, MAX);
    }
    */

}
