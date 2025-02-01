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

        int outBuck = get_material_bucket(ref p);

        // evaluation here
        int sum = OutputBias;

        fixed (Accumulator* accPtr = &this)
        {
            Vector<short>* ptrWhite = &accPtr->AccWhite16;
            Vector<short>* prtBlack = &accPtr->AccBlack16;

            int iterations = FT_SIZE / VECTOR_SIZE;

            for (int i=0; i<iterations; i++)
            {
                // Clamp the accumulator to 0 and QA
                var activatedWhite = Max(VECTOR_ZERO, Min(VECTOR_QA, *(ptrWhite+i)));
                var activatedBlack = Max(VECTOR_ZERO, Min(VECTOR_QA, *(prtBlack+i)));

                // load output weights
                var weightsWhite = new Vector<short>(OutputWeight[outBuck], p.us==WHITE ? i*VECTOR_SIZE : FT_SIZE+i*VECTOR_SIZE);
                var weightsBlack = new Vector<short>(OutputWeight[outBuck], p.us==BLACK ? i*VECTOR_SIZE : FT_SIZE+i*VECTOR_SIZE);

                // multiply activated accumulator and weights
                var multAccWhite = Multiply(activatedWhite, weightsWhite);
                var multAccBlack = Multiply(activatedBlack, weightsBlack);

                // widen the short Vectors because the next step might overflow 16 bits
                // multiply multAcc and actAcc for Lizard SCRELU
                // Pow(acc, 2) * w = (acc * acc) * w = (acc * w) * acc
                // then make the dot-product
                Vector<int> actLo, actHi, mulLo, mulHi;

                Widen(activatedWhite, out actLo, out actHi);
                Widen(multAccWhite, out mulLo, out mulHi);
                sum += Dot(actLo, mulLo) + Dot(actHi, mulHi);

                Widen(activatedBlack, out actLo, out actHi);
                Widen(multAccBlack, out mulLo, out mulHi);
                sum += Dot(actLo, mulLo) + Dot(actHi, mulHi);
            }
        }

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
