using static Constants;
using static Utils;
using static NNUE;
using System.Runtime.CompilerServices;

public unsafe struct Accumulator
{
    public fixed short AccumulatorWhite[HIDDEN_SIZE];
    public fixed short AccumulatorBlack[HIDDEN_SIZE];

    public Accumulator(pos p)
    {
        accumulate_from_zero(p);
    }

    public void activate(int color, int pt, int sq)
    {
        var (us_feat, them_feat) = get_768_idx(color, pt, sq);

        for (int i=0; i<HIDDEN_SIZE; i++)
        {
            AccumulatorWhite[i] += HiddenWeights[HIDDEN_SIZE * us_feat   + i];
            AccumulatorBlack[i] += HiddenWeights[HIDDEN_SIZE * them_feat + i];
        }
    }

    public void deactivate(int color, int pt, int sq)
    {
        var (us_feat, them_feat) = get_768_idx(color, pt, sq);

        for (int i=0; i<HIDDEN_SIZE; i++)
        {
            AccumulatorWhite[i] -= HiddenWeights[HIDDEN_SIZE * us_feat   + i];
            AccumulatorBlack[i] -= HiddenWeights[HIDDEN_SIZE * them_feat + i];
        }
    }

    /// <summary>
    /// resets the Accumulator, then activates according to the given position
    /// </summary>
    public unsafe void accumulate_from_zero(pos p)
    {
        fixed (short* whiteDestPtr = AccumulatorWhite)
        fixed (short* blackDestPtr = AccumulatorBlack)
        fixed (short* biasPtr = HiddenBias)
        {
            Unsafe.CopyBlock(whiteDestPtr, biasPtr, sizeof(short)*HIDDEN_SIZE);
            Unsafe.CopyBlock(blackDestPtr, biasPtr, sizeof(short)*HIDDEN_SIZE);
        }

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
}
