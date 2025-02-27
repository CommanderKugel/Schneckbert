using static Constants;
using static Utils;

using static NNUESettings;
using static NNUEWeights;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

public enum UpdateType 
{
    AddSub,
    AddSubSub,
    AddAddSubSub,
}

public unsafe partial struct Accumulator
{

    /// <summary>
    /// Efficiently Updates the Accumulator.
    /// If the king crosses the middle (hm) or moves into anothoer bucket,
    /// the Accumulator is fully refreshed.
    /// </summary>
    public void Update(UpdateType type, ref pos p, move m, int movingPt, int capturedPt)
    {

        // possibly full refresh for horizontal mirroring
        if (movingPt == KING && buckets[m.from] != buckets[m.to])
        {
            wflip = get_hm<COL_WHITE>(ref p);
            bflip = get_hm<COL_BLACK>(ref p);

            wbuck = get_kingbucket<COL_WHITE>(ref p);
            bbuck = get_kingbucket<COL_BLACK>(ref p);

            accumulate_from_zero(ref p);
            return;
        }

        // UE-part
        switch (type)
        {
            case UpdateType.AddSub: // quiet moves
            {
                AddSub(
                    p.us, 
                    m.IsPromo ? m.PromoPiece : movingPt, m.to,
                    movingPt, m.from
                );
                break;
            }
            case UpdateType.AddSubSub: // captuers & ep
            {
                AddSubSub(
                    p.us, m.IsPromo ? m.PromoPiece : movingPt, m.to,
                          movingPt,   m.from,
                          capturedPt, m.IsEp ? (p.us == WHITE ? m.to-8 : m.to+8) : m.to
                );
                break;
            }
            case UpdateType.AddAddSubSub: // castling
            {
                int rookFrom = (p.us==WHITE) ? H1 : H8;
                int rookTo   = (p.us==WHITE) ? F1 : F8;
                AddAddSubSub(p.us, m.from, m.to, rookFrom, rookTo);
                break;
            }
        }
    }


    public void AddSub(int color, int ptAdd, int sqAdd, int ptSub, int sqSub)
    {
        var (wFeatAdd, bFeatAdd) = get_768_idx_hm(color, ptAdd, sqAdd);
        var (wFeatSub, bFeatSub) = get_768_idx_hm(color, ptSub, sqSub);

        fixed (short* ptrWhite  = &AccWhite[0])
        fixed (short* ptrBlack  = &AccBlack[0])
        fixed (short* wWeightAdd = ftWeights[wbuck][wFeatAdd])
        fixed (short* bWeightAdd = ftWeights[bbuck][bFeatAdd])
        fixed (short* wWeightSub = ftWeights[wbuck][wFeatSub])
        fixed (short* bWeightSub = ftWeights[bbuck][bFeatSub])
        {
            for (int i=0; i<ITERATIONS; i++)
            {
                int offset = i*VECTOR_SIZE;

                var white = Vector256.Add(Avx.LoadVector256(ptrWhite + offset), Avx.LoadVector256(wWeightAdd + offset));
                var black = Vector256.Add(Avx.LoadVector256(ptrBlack + offset), Avx.LoadVector256(bWeightAdd + offset));
                    white = Vector256.Subtract(white, Avx.LoadVector256(wWeightSub + offset));
                    black = Vector256.Subtract(black, Avx.LoadVector256(bWeightSub + offset));

                Avx.Store(ptrWhite + offset, white);
                Avx.Store(ptrBlack + offset, black);
            }
        }
    }

    public void AddSubSub(int stm, int pt1, int sq1, 
                                   int pt2, int sq2, 
                                   int pt3, int sq3)
    {
        var (us_feat1, them_feat1) = get_768_idx_hm(  stm, pt1, sq1);
        var (us_feat2, them_feat2) = get_768_idx_hm(  stm, pt2, sq2);
        var (us_feat3, them_feat3) = get_768_idx_hm(1-stm, pt3, sq3);

        fixed (short* ptrWhite  = &AccWhite[0])
        fixed (short* ptrBlack  = &AccBlack[0])

        fixed (short* wWeights1 = ftWeights[wbuck][us_feat1  ])
        fixed (short* bWeights1 = ftWeights[bbuck][them_feat1])

        fixed (short* wWeights2 = ftWeights[wbuck][us_feat2  ])
        fixed (short* bWeights2 = ftWeights[bbuck][them_feat2])
        fixed (short* wWeights3 = ftWeights[wbuck][us_feat3  ])
        fixed (short* bWeights3 = ftWeights[bbuck][them_feat3])
        {
            for (int i=0; i<ITERATIONS; i++)
            {
                int offset = i*VECTOR_SIZE;

                var white = Vector256.Add(Avx.LoadVector256(ptrWhite + offset), Avx.LoadVector256(wWeights1 + offset));
                var black = Vector256.Add(Avx.LoadVector256(ptrBlack + offset), Avx.LoadVector256(bWeights1 + offset));
                    white = Vector256.Subtract(white, Avx.LoadVector256(wWeights2 + offset));
                    black = Vector256.Subtract(black, Avx.LoadVector256(bWeights2 + offset));
                    white = Vector256.Subtract(white, Avx.LoadVector256(wWeights3 + offset));
                    black = Vector256.Subtract(black, Avx.LoadVector256(bWeights3 + offset));

                Avx.Store(ptrWhite + offset, white);
                Avx.Store(ptrBlack + offset, black);
            }
        }
    }

    public void AddAddSubSub(int stm, int kf, int kt, int rf, int rt)
    {
        var (wKAdd, bKAdd) = get_768_idx_hm(stm, KING, kt);
        var (wKSub, bKSub) = get_768_idx_hm(stm, KING, kf);
        var (wRAdd, bRAdd) = get_768_idx_hm(stm, ROOK, rt);
        var (wRSub, bRSub) = get_768_idx_hm(stm, ROOK, rf);

        fixed (short* ptrWhite  = &AccWhite[0])
        fixed (short* ptrBlack  = &AccBlack[0])

        fixed (short* kingAddW = ftWeights[wbuck][wKAdd])
        fixed (short* kingAddB = ftWeights[bbuck][bKAdd])
        fixed (short* kingSubW = ftWeights[wbuck][wKSub])
        fixed (short* kingSubB = ftWeights[bbuck][bKSub])

        fixed (short* rookAddW = ftWeights[wbuck][wRAdd])
        fixed (short* rookAddB = ftWeights[bbuck][bRAdd])
        fixed (short* rookSubW = ftWeights[wbuck][wRSub])
        fixed (short* rookSubB = ftWeights[bbuck][bRSub])
        {
            for (int i=0; i<ITERATIONS; i++)
            {
                int offset = i*VECTOR_SIZE;

                var white = Vector256.Add     (Avx.LoadVector256(ptrWhite + offset), Avx.LoadVector256(kingAddW + offset));
                var black = Vector256.Add     (Avx.LoadVector256(ptrBlack + offset), Avx.LoadVector256(kingAddB + offset));

                    white = Vector256.Add     (white, Avx.LoadVector256(rookAddW + offset));
                    black = Vector256.Add     (black, Avx.LoadVector256(rookAddB + offset));

                    white = Vector256.Subtract(white, Avx.LoadVector256(kingSubW + offset));
                    black = Vector256.Subtract(black, Avx.LoadVector256(kingSubB + offset));

                    white = Vector256.Subtract(white, Avx.LoadVector256(rookSubW + offset));
                    black = Vector256.Subtract(black, Avx.LoadVector256(rookSubB + offset));

                Avx.Store(ptrWhite + offset, white);
                Avx.Store(ptrBlack + offset, black);
            }
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
        fixed (short* wWeights = ftWeights[wbuck][us_feat  ])
        fixed (short* bWeights = ftWeights[bbuck][them_feat])
        {
            short* ptrWhite = &acc->AccWhite[0];
            short* prtBlack = &acc->AccBlack[0];
            
            for (int i=0; i<ITERATIONS; i++)
            {
                int offset = i*VECTOR_SIZE;
                Avx.Store(ptrWhite + offset, Vector256.Add(Avx.LoadVector256(ptrWhite + offset), Avx.LoadVector256(wWeights + offset)));
                Avx.Store(prtBlack + offset, Vector256.Add(Avx.LoadVector256(prtBlack + offset), Avx.LoadVector256(bWeights + offset)));
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
        fixed (short* wWeights = ftWeights[wbuck][us_feat  ])
        fixed (short* bWeights = ftWeights[bbuck][them_feat])
        {
            short* ptrWhite = &acc->AccWhite[0];
            short* prtBlack = &acc->AccBlack[0];
            
            for (int i=0; i<ITERATIONS; i++)
            {
                int offset = i*VECTOR_SIZE;
                Avx.Store(ptrWhite + offset, Vector256.Subtract(Avx.LoadVector256(ptrWhite + offset), Avx.LoadVector256(wWeights + offset)));
                Avx.Store(prtBlack + offset, Vector256.Subtract(Avx.LoadVector256(prtBlack + offset), Avx.LoadVector256(bWeights + offset)));
            }
        }
    }

    /// <summary>
    /// Resets the Accumulator, then activates according to the given position
    /// </summary>
    public unsafe void accumulate_from_zero(ref pos p)
    {
        for (int i=0; i<FT_SIZE; i++)
        {
            AccWhite[i] = ftBias[i];
            AccBlack[i] = ftBias[i];
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
    
}
