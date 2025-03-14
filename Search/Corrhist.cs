using static Constants;
using static Utils;

using static System.Math;


public static class CorrHist
{
    /*
    /   Reset between bench positions
    /   Reset on UCI Newgame
    /   Correct in regular Search
    /   Correct in Quiescense Search
    /   Update after regular Search
    */

    /*
    private const int SIZE  = 16384;

    private const short MIN_VALUE = -8_192;
    private const short MAX_VALUE =  8_192;

    private const int CORRECTION_DIVISOR = 300 * 256;

    private const int PAWN_WEIGHT = 200;
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
        var pawnVal = PawnCorrHist[p.us][p.PawnKey % SIZE];

        var correctionValue = (pawnVal * PAWN_WEIGHT) / CORRECTION_DIVISOR;
        ss->StaticEval = ss->RawStaticEval + correctionValue;
    }

    // stole this implementation from Motor
    // cant get it to work on my own :(
    // https://github.com/martinnovaak/motor/blob/main/search/tables/history_table.hpp
    public static unsafe void update_corrhist(ref pos p, SS* ss, int score, int depth)
    {
        int diff = (score - ss->StaticEval) * 256;
        int weight = Min(128, depth * 8);

        update_single(ref PawnCorrHist[p.us][p.PawnKey % SIZE]);

        void update_single(ref short entry)
        {
            entry = (short)((entry * (256 - weight) + diff * weight) / 256);
            entry = Clamp(entry, MIN_VALUE, MAX_VALUE);
        }
    }

    */

}
