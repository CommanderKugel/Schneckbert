using static Constants;
using static NNUESettings;
using static NNUEWeights;

using static System.Math;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

public unsafe partial struct Accumulator
{

    private static Vector256<short> VECTOR_QA;

    static Accumulator()
    {
        var temp = new short[VECTOR_SIZE];
        Array.Fill(temp, QA);
        fixed (short* ptr = temp)
        {
            VECTOR_QA = Avx.LoadVector256(ptr);
        }
    }

    /// <summary>
    /// Returns the static Evaluation of the position.
    /// Makes use of the positions Accumulators.
    /// QS = 255, QB = 64, SCALE = 400
    /// </summary>
    public unsafe int Evaluate(ref pos p)
    {
        int outBuck = get_material_bucket(ref p);
        var evalAccumulator = Vector256<int>.Zero;

        // fix the weights and accumulator arrays
        // we can use pointer arithmetic afterwards
        // necessary for loading Avx-Vectors.
        fixed (short* outWeightPtr = OutputWeight[outBuck][0])
        fixed (short* ptrAccWhite  = &AccWhite[0])
        fixed (short* ptrAccBlack  = &AccBlack[0])
        {
            // find the weight pointers
            short* ptrWhiteWeight = p.us==WHITE ? outWeightPtr : outWeightPtr + FT_SIZE;
            short* ptrBlackWeight = p.us==BLACK ? outWeightPtr : outWeightPtr + FT_SIZE;

            for (int i=0; i<ITERATIONS; i++)
            {
                // Clamp the accumulator to 0 and QA
                var activatedWhite = Avx2.Max(Vector256<short>.Zero, Avx2.Min(VECTOR_QA, Avx.LoadVector256(ptrAccWhite + i * 16)));
                var activatedBlack = Avx2.Max(Vector256<short>.Zero, Avx2.Min(VECTOR_QA, Avx.LoadVector256(ptrAccBlack + i * 16)));

                // load output weights
                var weightsWhite = Avx.LoadVector256(ptrWhiteWeight + i * 16);
                var weightsBlack = Avx.LoadVector256(ptrBlackWeight + i * 16);

                // multiply activated accumulator and weights
                var multAccWhite = Avx2.MultiplyLow(activatedWhite, weightsWhite);
                var multAccBlack = Avx2.MultiplyLow(activatedBlack, weightsBlack);

                // Lizard SCReLU: (x * x) * y = (x * y) * x
                // until now, 255 * 64 fits into a int16 value
                // because we want to square the 255 as part of the activation function, we delayed that until now
                // we use MultiplyAddAdjacent to transform into int32 to avoid overflows.
                // The add does not hurt, because we want to sum this result anyways.

                var sqrMultWhite = Avx2.MultiplyAddAdjacent(activatedWhite, multAccWhite);
                var sqrMultBlack = Avx2.MultiplyAddAdjacent(activatedBlack, multAccBlack);

                // add onto the output-accumulator 
                evalAccumulator = Avx2.Add(sqrMultWhite, evalAccumulator);
                evalAccumulator = Avx2.Add(sqrMultBlack, evalAccumulator);
            }
        }

        int eval = (Vector256.Sum(evalAccumulator) / QA + OutputBias[outBuck][0]) * SCALE / QAB;
        
        return Clamp(eval, -EVAL_SCORE_MAX, EVAL_SCORE_MAX);
    }


    public unsafe int Evaluate_wdl(ref pos p)
    {
        int outBuck = get_material_bucket(ref p);

        // output accumulator for WDL ouput nodes
        Vector256<int>[] outWdl = [Vector256<int>.Zero, Vector256<int>.Zero, Vector256<int>.Zero];
        
        fixed (Accumulator* accPtr = &this)
        fixed (short* winPtr  = OutputWeight[outBuck][2])
        fixed (short* drawPtr = OutputWeight[outBuck][1])
        fixed (short* lossPtr = OutputWeight[outBuck][0])
        {
            short* stmAccPtr = p.us==WHITE ? &accPtr->AccWhite[0] : &accPtr->AccBlack[0];
            short* ntmAccPtr = p.us==BLACK ? &accPtr->AccWhite[0] : &accPtr->AccBlack[0];

            short*[] weightPtrs = [winPtr, drawPtr, lossPtr];

            for (int i=0; i<ITERATIONS; i++)
            {
                int offset = i * VECTOR_SIZE;

                var clampedStm = Avx2.Max(Vector256<short>.Zero, Avx2.Min(VECTOR_QA, Avx.LoadVector256(stmAccPtr + offset)));
                var clampedNtm = Avx2.Max(Vector256<short>.Zero, Avx2.Min(VECTOR_QA, Avx.LoadVector256(ntmAccPtr + offset)));

                for (int wdl=0; wdl<3; wdl++)
                {
                    var weightsStm = Avx.LoadVector256(weightPtrs[wdl] + offset);
                    var weightsNtm = Avx.LoadVector256(weightPtrs[wdl] + offset + FT_SIZE);

                    var weightedStm = Avx2.MultiplyLow(weightsStm, clampedStm);
                    var weightedNtm = Avx2.MultiplyLow(weightsNtm, clampedNtm);

                    var sqrStm = Avx2.MultiplyAddAdjacent(clampedStm, weightedStm);
                    var sqrNtm = Avx2.MultiplyAddAdjacent(clampedNtm, weightedNtm);

                    outWdl[wdl] = Avx2.Add(sqrStm, outWdl[wdl]);
                    outWdl[wdl] = Avx2.Add(sqrNtm, outWdl[wdl]);
                }
            }
        }

        double win  = (double)(Vector256.Sum(outWdl[0]) / QA + OutputBias[outBuck][0]) / QAB;
        double draw = (double)(Vector256.Sum(outWdl[1]) / QA + OutputBias[outBuck][1]) / QAB;
        double loss = (double)(Vector256.Sum(outWdl[2]) / QA + OutputBias[outBuck][2]) / QAB;

        var max = Max(win, Max(draw, loss));
        win  = Exp(win  - max);
        draw = Exp(draw - max);
        loss = Exp(loss - max);

        var one_over_naive = (win + draw + loss) / (win + draw/2);
        var scoreCp = -(400/3) * Log2(one_over_naive - 1);

        //Console.WriteLine($"WDL% {win/total} : {draw/total} : {loss/total}");
        //Console.WriteLine($"bayes elo cp: {scoreCp}");

        return Clamp((int)scoreCp, -EVAL_SCORE_MAX, EVAL_SCORE_MAX);
    }

    /// <summary>
    /// Returns the output bucket for the given position.
    /// Depends on the total Piececount, including Pawns, excluding kings.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int get_material_bucket(ref pos p)
        => (Utils.popCount(p.get_blocker()) - 2) / OUT_BUCK_DIVISOR;

}
