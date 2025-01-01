using static Constants;
using static Utils;
using static NNUE;
using System.Runtime.CompilerServices;

public unsafe struct Accumulator
{
    public fixed short AccumulatorWhite[HIDDEN_SIZE];
    public fixed short AccumulatorBlack[HIDDEN_SIZE];

    public int wflip = 0;
    public int bflip = 0;

    public Accumulator(pos p)
    {
        wflip = file_of(p.get_ksq(WHITE)) > 3 ? 7 : 0;
        bflip = file_of(p.get_ksq(BLACK)) > 3 ? 7 : 0;
        accumulate_from_zero(ref p);
    }


    private static readonly int[] flanks = [ 0, 0, 0, 0, 1, 1, 1, 1 ];
    public void update_hm(int pt, int from, int to, ref pos p)
    {
        if (pt != KING)
        {
            return;
        }

        if (flanks[file_of(from)] != flanks[file_of(to)])
        {
            wflip = file_of(p.get_ksq(WHITE)) > 3 ? 7 : 0;
            bflip = file_of(p.get_ksq(BLACK)) > 3 ? 7 : 0;
            accumulate_from_zero(ref p);
        }
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
    public unsafe void accumulate_from_zero(ref pos p)
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

    public (int, int) get_768_idx(int color, int pt, int sq)
    {
        int wfeat = (1-color) * 384 + pt * 64 + (sq ^ wflip);
        int bfeat =    color  * 384 + pt * 64 + (sq ^ bflip ^ 56);
        return (wfeat, bfeat);
    }


    public static bool operator ==(Accumulator lhs, Accumulator rhs)
    {
        for (int i=0; i<HIDDEN_SIZE; i++)
        {
            if (lhs.AccumulatorWhite[i] != rhs.AccumulatorWhite[i] ||
                lhs.AccumulatorBlack[i] != rhs.AccumulatorBlack[i])
            {
                throw new Exception("Accumulated values don't match!");
            }
        }

        if (lhs.wflip != rhs.wflip)
            throw new Exception("flanks of white King dont match!");
        
        if (lhs.bflip != rhs.bflip)
            throw new Exception("flanks of black King dont match!");

        return true;
    }

    public static bool operator !=(Accumulator lhs, Accumulator rhs) => !(lhs==rhs);
}
