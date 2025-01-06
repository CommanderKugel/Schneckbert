using static Constants;
using static Utils;
using static NNUEWeights;
using static NNUESettings;

using System.Numerics;


public unsafe partial struct Accumulator
{
    public Vector<short> AccWhite;
    public Vector<short> AccBlack;

    int wflip;
    int bflip;
    
    int wbuck;
    int bbuck;


    public Accumulator(pos p)
    {
        wflip = get_hm<COL_WHITE>(ref p);
        bflip = get_hm<COL_BLACK>(ref p);

        wbuck = get_bucket<COL_WHITE>(ref p);
        bbuck = get_bucket<COL_BLACK>(ref p);

        accumulate_from_zero(ref p);
    }


    public void update_hm(int pt, int from, int to, ref pos p)
    {
        if (pt == KING && buckets[from] != buckets[to])
        {
            wflip = get_hm<COL_WHITE>(ref p);
            bflip = get_hm<COL_BLACK>(ref p);

            wbuck = get_bucket<COL_WHITE>(ref p);
            bbuck = get_bucket<COL_BLACK>(ref p);

            accumulate_from_zero(ref p);
        }
    }

    public void activate(int color, int pt, int sq)
    {
        var (us_feat, them_feat) = get_768_idx_hm(color, pt, sq);
        AccWhite += new Vector<short>(HiddenWeights[wbuck][us_feat]);
        AccBlack += new Vector<short>(HiddenWeights[bbuck][them_feat]);
    }

    public void deactivate(int color, int pt, int sq)
    {
        var (us_feat, them_feat) = get_768_idx_hm(color, pt, sq);
        AccWhite -= new Vector<short>(HiddenWeights[wbuck][us_feat]);
        AccBlack -= new Vector<short>(HiddenWeights[bbuck][them_feat]);
    }

    /// <summary>
    /// resets the Accumulator, then activates according to the given position
    /// </summary>
    public unsafe void accumulate_from_zero(ref pos p)
    {
        AccWhite = new Vector<short>(HiddenBias);
        AccBlack = new Vector<short>(HiddenBias);

        // set all Pieces
        for (int color=BLACK; color<=WHITE; color++)
        {
            for (int pt=PAWN; pt<=KING; pt++)
            {
                ulong pieces = p.get_pieces(pt, color);
                while (pieces != 0)
                {
                    int sq = popLsb(ref pieces);
                    activate(color, pt, sq);
                }
            }
        }
    }


    public (int, int) get_768_idx(int color, int pt, int sq)
    {
        int wfeat = (1-color) * 384 + pt * 64 + (sq);
        int bfeat =    color  * 384 + pt * 64 + (sq ^ 56);
        return (wfeat, bfeat);
    }

    public (int, int) get_768_idx_hm(int color, int pt, int sq)
    {
        int wfeat = (1-color) * 384 + pt * 64 + (sq ^ wflip);
        int bfeat =    color  * 384 + pt * 64 + (sq ^ bflip ^ 56);
        return (wfeat, bfeat);
    }

    public int get_bucket<Color>(ref pos p) where Color : COL
        => typeof(Color)==typeof(COL_WHITE) ? buckets[p.get_ksq(WHITE) ^ wflip] 
                                            : buckets[p.get_ksq(BLACK) ^ bflip ^ 56];

    public int get_hm<Color>(ref pos p) where Color : COL
        =>  file_of(p.get_ksq(typeof(Color)==typeof(COL_WHITE) ? WHITE : BLACK)) > 3 ? 7 : 0;



    public static bool operator ==(Accumulator lhs, Accumulator rhs)
    {
        for (int i=0; i<Vector<short>.Count; i++)
            if (lhs.AccWhite[i] != rhs.AccWhite[i] || lhs.AccBlack[i] != rhs.AccBlack[i])
                throw new Exception("Accumulated values don't match!");
        if (lhs.wflip != rhs.wflip || lhs.bflip != rhs.bflip)
            throw new Exception("flanks of white King dont match!");
        if (lhs.wbuck != rhs.wflip || lhs.bflip != rhs.bflip)
            throw new Exception("king buckets dont match!");
        return true;
    }
    public static bool operator !=(Accumulator lhs, Accumulator rhs) => !(lhs==rhs);
}
