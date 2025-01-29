using static Constants;
using static NNUESettings;
using static NNUEWeights;

using static System.Numerics.Vector;
using System.Numerics;
using static System.Math;
using System.Runtime.CompilerServices;

public unsafe partial struct Accumulator
{
    /// <summary>
    /// Returns the static Evaluation of the position.
    /// Makes use of the positions Accumulators.
    /// QS = 255, QB = 64, SCALE = 400
    /// </summary>
    public unsafe int Evaluate(ref pos p)
    {
        var VECTOR_QA   = new Vector<short>(QA);
        var VECTOR_ZERO = Vector<short>.Zero;

        // evaluation here
        int sum = OutputBias;

        int outBuck = get_output_bucket(ref p);

        fixed (Accumulator* accPtr = &this)
        {
            Vector<short>* ptrWhite = &accPtr->AccWhite16;
            Vector<short>* prtBlack = &accPtr->AccBlack16;

            int iterations = FT_SIZE / VECTOR_SIZE;

            for (int i=0; i<iterations; i++)
            {
                // activate the accumulator
                // Squared Clipped Rectified Linear Unit
                // y = Pow(Clamp(x, 0, 181), 2);
                // Quantization targets QA=181 and QB=64 are choosen, because 
                // Pow(181, 2) * 64 still fits into the short datatype
                var activatedWhite = Max(VECTOR_ZERO, Min(VECTOR_QA, *(ptrWhite+i)));
                var activatedBlack = Max(VECTOR_ZERO, Min(VECTOR_QA, *(prtBlack+i)));
                activatedWhite = Multiply(activatedWhite, activatedWhite);
                activatedBlack = Multiply(activatedBlack, activatedBlack);

                // load output weights
                var weightsWhite = new Vector<short>(OutputWeight[outBuck], p.us==WHITE ? i*VECTOR_SIZE : FT_SIZE+i*VECTOR_SIZE);
                var weightsBlack = new Vector<short>(OutputWeight[outBuck], p.us==BLACK ? i*VECTOR_SIZE : FT_SIZE+i*VECTOR_SIZE);

                // widen the short Vectors
                // then calculate dot-product from weights and activated accumulator
                Vector<int> actLo, actHi, weightLo, weightHi;

                Widen(activatedWhite, out actLo, out actHi);
                Widen(weightsWhite, out weightLo, out weightHi);
                sum += Dot(actLo, weightLo) + Dot(actHi, weightHi);

                Widen(activatedBlack, out actLo, out actHi);
                Widen(weightsBlack, out weightLo, out weightHi);
                sum += Dot(actLo, weightLo) + Dot(actHi, weightHi);
            }
        }

        // Scaling from small original floating point numbers
        // comparable to ~centipawns now
        sum *= SCALE;

        // Remove Quantization
        sum /= QA * QB;

        return Clamp(sum, -EVAL_SCORE_MAX, EVAL_SCORE_MAX);
    }

    /// <summary>
    /// Returns the output bucket for the given position.
    /// Depends on the total Piececount, including Pawns, excluding kings.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int get_output_bucket(ref pos p) {
        var count = Utils.popCount(p.get_blocker()) - 2;
        var bucket = count / OUT_BUCK_DIVISOR;
        return bucket;
    }
}
