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
        var temp = new short[] { QA, QA, QA, QA, QA, QA, QA, QA, QA, QA, QA, QA, QA, QA, QA, QA, };
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
        fixed (short* outWeightPtr = OutputWeight[outBuck])
        fixed (short* ptrAccWhite  = &AccWhite[0])
        fixed (short* ptrAccBlack  = &AccBlack[0])
        {
            // find the weight pointers
            short* ptrWhiteWeight = p.us==WHITE ? outWeightPtr : outWeightPtr + FT_SIZE;
            short* ptrBlackWeight = p.us==BLACK ? outWeightPtr : outWeightPtr + FT_SIZE;

            int iterations = FT_SIZE / VECTOR_SIZE;

            for (int i=0; i<iterations; i++)
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

        int sum = Vector256.Sum(evalAccumulator) + OutputBias;

        // Scaling from small original floating point numbers
        // comparable to ~centipawns now
        sum *= SCALE;

        // Remove Quantization
        sum /= QA * QB;

        // SCALE should be 400 insead of 2
        // and QA * QB should be QA * QA * QB
        // *SPRT COMING SOON*

        return Clamp(sum, -EVAL_SCORE_MAX, EVAL_SCORE_MAX);
    }

    /// <summary>
    /// Returns the output bucket for the given position.
    /// Depends on the total Piececount, including Pawns, excluding kings.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int get_material_bucket(ref pos p)
        => (Utils.popCount(p.get_blocker()) - 2) / OUT_BUCK_DIVISOR;

}
