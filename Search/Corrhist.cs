using static Constants;
using static Utils;

using static System.Math;


public static class CorrHist
{
    // motors impl is somewhat simple to understand/yoink
    // https://github.com/martinnovaak/motor/blob/main/search/tables/history_table.hpp

    // stockfish impl
    // Correct https://github.com/official-stockfish/Stockfish/blob/6ceaca4c7b2cc1fa87617b1b9e83d38d8e880924/src/search.cpp#L85
    // Update  https://github.com/official-stockfish/Stockfish/blob/6ceaca4c7b2cc1fa87617b1b9e83d38d8e880924/src/search.cpp#L1494
    //         https://github.com/official-stockfish/Stockfish/blob/6ceaca4c7b2cc1fa87617b1b9e83d38d8e880924/src/search.cpp#L132

    /*
    /   Reset between bench positions
    /   Reset on UCI Newgame
    /   Correct in regular Search
    /   Correct in Quiescense Search
    /   Update after regular Search
    */

    /*
    private const int SIZE  = 16384;

    private const int CORRECTION_DIVISOR = 131_072;

    private const int PAWN_WEIGHT = 7685;
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

    public static unsafe int get_correction_value(ref pos p, SS* ss)
    {
        int pawnCorr = PawnCorrHist[p.us][p.PieceKeys[PAWN] % SIZE];

        return (pawnCorr * PAWN_WEIGHT) / CORRECTION_DIVISOR;
    }

    public static unsafe void correct_static_eval(ref pos p, SS* ss)
    {
        int correctionValue = get_correction_value(ref p, ss);
        ss->StaticEval = Clamp(ss->RawStaticEval + correctionValue, -EVAL_SCORE_MAX, EVAL_SCORE_MAX);
    }

    
    public static unsafe void update_corrhist(ref pos p, SS* ss, int score, int depth)
    {
        int bonus = Clamp((score - ss->StaticEval) * depth / 8, -1024, 1024);

        update_single(ref PawnCorrHist[p.us][p.PieceKeys[PAWN] % SIZE], bonus);
    }

    private static void update_single(ref short entry, int bonus) 
        => entry += (short)(bonus - entry * Abs(bonus) / 1024);
    */
    
}
