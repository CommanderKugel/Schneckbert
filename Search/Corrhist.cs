using static Constants;
using static Utils;

using static System.Math;


public static class CorrHist
{
    /*
    // NOT IMPLEMENTED cuz is worth ~ -300 rn
    // yoinked the implementation from Caissa but
    // no way i could implement this shit without some yoinkage
    // https://github.com/Witek902/Caissa/blob/master/src/backend/Search.cpp

    // methods will be needed in:
    // Reset:               Program.cs, Bench.cs
    // correct_static_eval: Search.cs, QuiescenseSearch.cs
    // update_corrhist:     Search.cs

    private const int CORRECTION_SCALE = 256;
    private const int UPDATE_SCALE     = 256;
    private const int MAX_DELTA        = 128;

    private const int SIZE             = 16384;

    private const int PAWN_WEIGHT = 32;
    private static short[][] PawnCorrHist;


    static CorrHist()
    {
        PawnCorrHist = new short[2][];
        PawnCorrHist[BLACK] = new short[SIZE];
        PawnCorrHist[WHITE] = new short[SIZE];
    }

    public static void Reset()
    {
        Array.Clear(PawnCorrHist[BLACK]);
        Array.Clear(PawnCorrHist[WHITE]);
    }
    
    // only when !inCheck
    public static unsafe void correct_static_eval(ref pos p, SS* ss)
    {
        int pawnCorrection = PAWN_WEIGHT * PawnCorrHist[p.us][p.PawnKey % SIZE];
        int correction = pawnCorrection / CORRECTION_SCALE;

        ss->StaticEval = Clamp(ss->RawStaticEva + correction, -EVAL_SCORE_MAX, EVAL_SCORE_MAX);
    }

    
    //in Search only:    
    //(!inCheck &&
    // !inSingularity && 
    // (locBestMove.IsNull || !p.is_capture(locBestMove) && !locBestMove.IsPromo) &&
    // (flag == BOUND_EXACT ||
    //  flag == BOUND_LOWER && bestScore > ss->RawStaticEva ||
    //  flag == BOUND_UPPER && bestScore < ss->RawStaticEva))
    
    public static unsafe void update_corrhist(ref pos p, SS* ss, int score, int depth)
    {
        short delta = (short)Clamp((score - ss->RawStaticEva) / UPDATE_SCALE, -MAX_DELTA, MAX_DELTA);
        update_single_history(ref PawnCorrHist[p.us][p.PawnKey % SIZE], delta);
    }


    // ToDo: Gravity
    private static void update_single_history(ref short value, short delta)
        => value += delta;
    */
}
