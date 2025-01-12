using static Constants;
using static NNUESettings;
using static NNUEWeights;

using static System.Numerics.Vector;
using System.Numerics;
using static System.Math;

public unsafe partial struct Accumulator
{
    /// <summary>
    /// Returns the static Evaluation of the position.
    /// Makes use of the positions Accumulators.
    /// QS = 255, QB = 64, SCALE = 400
    /// </summary>
    public unsafe int Evaluate(ref pos p)
    {
        // CRelu activation function
        // Clamped Rectified Linear Unit
        var actWhiteLo = Max(Vector<short>.Zero, Min(new Vector<short>(QA), AccWhiteLo));
        var actWhiteHi = Max(Vector<short>.Zero, Min(new Vector<short>(QA), AccWhiteHi));
        var actBlackLo = Max(Vector<short>.Zero, Min(new Vector<short>(QA), AccBlackLo));
        var actBlackHi = Max(Vector<short>.Zero, Min(new Vector<short>(QA), AccBlackHi));

        // load output weights into Vectors
        var weightsWhiteLo = new Vector<short>(OutputWeight, p.us==WHITE ? 0  : HIDDEN_SIZE);
        var weightsWhiteHi = new Vector<short>(OutputWeight, p.us==WHITE ? 16 : HIDDEN_SIZE+16);
        var weightsBlackLo = new Vector<short>(OutputWeight, p.us==BLACK ? 0  : HIDDEN_SIZE);
        var weightsBlackHi = new Vector<short>(OutputWeight, p.us==BLACK ? 16 : HIDDEN_SIZE+16);

        // Multiply the Accumulator and the Weights
        // Dont compute the Dot Product yet to avoid overflow errors
        var mult = Multiply(actWhiteLo, weightsWhiteLo) + Multiply(actWhiteHi, weightsWhiteHi)
                 + Multiply(actBlackLo, weightsBlackLo) + Multiply(actBlackHi, weightsBlackHi);

        // widen the <short> datatype to <int>
        Vector<int> lower, upper;
        Widen(mult, out lower, out upper);
        int sum = Sum(lower) + Sum(upper) + OutputBias;

        // Scaling from small original floating point numbers
        // comparable to ~centipawns now
        sum *= SCALE;

        // Remove Quantization
        sum /= QA * QB;

        return Clamp(sum, -EVAL_SCORE_MAX, EVAL_SCORE_MAX);
    }
}
