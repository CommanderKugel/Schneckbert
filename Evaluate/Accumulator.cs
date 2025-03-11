using static Constants;
using static Utils;
using static NNUEWeights;
using static NNUESettings;

using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct Accumulator
{
    [FieldOffset(FT_SIZE * 0)]
    private fixed short AccWhite[FT_SIZE];
    
    [FieldOffset(FT_SIZE * 2)]
    private fixed short AccBlack[FT_SIZE];

    [FieldOffset(FT_SIZE * 4 + 0)]
    int wflip = 0;
    [FieldOffset(FT_SIZE * 4 + 4)]
    int bflip = 0;
    
    [FieldOffset(FT_SIZE * 4 + 8)]
    int wbuck = 0;
    [FieldOffset(FT_SIZE * 4 + 12)]
    int bbuck = 0;


    public Accumulator(pos p)
    {
        wflip = get_hm<COL_WHITE>(ref p);
        bflip = get_hm<COL_BLACK>(ref p);

        wbuck = get_kingbucket<COL_WHITE>(ref p);
        bbuck = get_kingbucket<COL_BLACK>(ref p);

        accumulate_from_zero(ref p);
    }

    /// <summary>
    /// Returns the index of the inputfeature for the dual-perspective input scheme
    /// Should not be used, as all nets will use hm-input features!
    /// </summary>
    private (int, int) get_768_idx(int color, int pt, int sq)
    {
        int wfeat = (1-color) * 384 + pt * 64 + (sq);
        int bfeat =    color  * 384 + pt * 64 + (sq ^ 56);
        return (wfeat, bfeat);
    }

    /// <summary>
    /// Returns the index of the inputfeature for the dual-perspective and horizontally mirrored input scheme
    /// </summary>
    private (int, int) get_768_idx_hm(int color, int pt, int sq)
    {
        int wfeat = (1-color) * 384 + pt * 64 + (sq ^ wflip);
        int bfeat =    color  * 384 + pt * 64 + (sq ^ bflip ^ 56);
        return (wfeat, bfeat);
    }

    /// <summary>
    /// Returns the kingbucket of the given color.
    /// </summary>
    private int get_kingbucket<Color>(ref pos p) where Color : COL
        => typeof(Color)==typeof(COL_WHITE) ? buckets[p.get_ksq(WHITE) ^ wflip] 
                                            : buckets[p.get_ksq(BLACK) ^ bflip ^ 56];

    /// <summary>
    /// Returns 7 if the king crossed over to the other half of the board.
    /// XORing effectively flips all Pieces over to the other side, mirrored vertically.
    /// </summary>
    private int get_hm<Color>(ref pos p) where Color : COL
        =>  file_of(p.get_ksq(typeof(Color)==typeof(COL_WHITE) ? WHITE : BLACK)) > 3 ? 7 : 0;

}
