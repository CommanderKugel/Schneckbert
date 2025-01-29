using static Constants;
using static Utils;
using static NNUEWeights;
using static NNUESettings;

using System.Numerics;


public unsafe partial struct Accumulator
{
    public Vector<short> AccWhite16,  AccWhite32,  AccWhite48,  AccWhite64,  AccWhite80,  AccWhite96,
                         AccWhite112, AccWhite128, AccWhite144, AccWhite160, AccWhite176, AccWhite192;
    public Vector<short> AccBlack16,  AccBlack32,  AccBlack48,  AccBlack64,  AccBlack80,  AccBlack96,
                         AccBlack112, AccBlack128, AccBlack144, AccBlack160, AccBlack176, AccBlack192;

    int wflip;
    int bflip;
    
    int wbuck;
    int bbuck;


    public Accumulator(pos p)
    {
        wflip = get_hm<COL_WHITE>(ref p);
        bflip = get_hm<COL_BLACK>(ref p);

        wbuck = get_kingbucket<COL_WHITE>(ref p);
        bbuck = get_kingbucket<COL_BLACK>(ref p);

        accumulate_from_zero(ref p);
    }

    /// <summary>
    /// Checks if the king moved to a new bucket.
    /// If it did, the Accumulator is refreshed completely.
    /// </summary>
    public void update_hm(int pt, int from, int to, ref pos p)
    {
        if (pt == KING && buckets[from] != buckets[to])
        {
            wflip = get_hm<COL_WHITE>(ref p);
            bflip = get_hm<COL_BLACK>(ref p);

            wbuck = get_kingbucket<COL_WHITE>(ref p);
            bbuck = get_kingbucket<COL_BLACK>(ref p);

            accumulate_from_zero(ref p);
        }
    }

    /// <summary>
    /// Computes the changes in the affine layer of the feature transformer,
    /// if a binary input of the sparse input layer is activated from 0 to 1.
    /// </summary>
    public void activate(int color, int pt, int sq)
    {
        var (us_feat, them_feat) = get_768_idx_hm(color, pt, sq);

        fixed (Accumulator* acc = &this)
        {
            Vector<short>* ptrWhite = &acc->AccWhite16;
            Vector<short>* prtBlack = &acc->AccBlack16;
            
            int iters = FT_SIZE / VECTOR_SIZE;

            for (int i=0; i<iters; i++)
            {
                *(ptrWhite+i) += new Vector<short>(ftWeights[wbuck][us_feat  ], i * VECTOR_SIZE);
                *(prtBlack+i) += new Vector<short>(ftWeights[bbuck][them_feat], i * VECTOR_SIZE);
            }
        }
    }

    /// <summary>
    /// Computes the changes in the affine layer of the feature transformer,
    /// if a binary input of the sparse input layer is deactivated from 1 to 0.
    /// </summary>
    public void deactivate(int color, int pt, int sq)
    {
        var (us_feat, them_feat) = get_768_idx_hm(color, pt, sq);

        fixed (Accumulator* acc = &this)
        {
            Vector<short>* ptrWhite = &acc->AccWhite16;
            Vector<short>* prtBlack = &acc->AccBlack16;
            
            int iters = FT_SIZE / VECTOR_SIZE;

            for (int i=0; i<iters; i++)
            {
                *(ptrWhite+i) -= new Vector<short>(ftWeights[wbuck][us_feat  ], i * VECTOR_SIZE);
                *(prtBlack+i) -= new Vector<short>(ftWeights[bbuck][them_feat], i * VECTOR_SIZE);
            }
        }
    }

    /// <summary>
    /// Resets the Accumulator, then activates according to the given position
    /// </summary>
    public unsafe void accumulate_from_zero(ref pos p)
    {
        // load the bias
        fixed (Accumulator* acc = &this)
        {
            Vector<short>* ptrWhite = &acc->AccWhite16;
            Vector<short>* prtBlack = &acc->AccBlack16;
            
            int iters = FT_SIZE / VECTOR_SIZE;

            for (int i=0; i<iters; i++)
            {
                *(ptrWhite+i) = new Vector<short>(ftBias, i * VECTOR_SIZE);
                *(prtBlack+i) = new Vector<short>(ftBias, i * VECTOR_SIZE);
            }
        }

        // activate all active features
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
