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
        var activatedWhite = Max(Vector<short>.Zero, Min(new Vector<short>(QA), p.accumulator.AccWhite));
        var activatedBlack = Max(Vector<short>.Zero, Min(new Vector<short>(QA), p.accumulator.AccBlack));

        // load output weights into Vectors
        var weightsWhite = new Vector<short>(OutputWeight, p.us==WHITE ? 0 : HIDDEN_SIZE);
        var weightsBlack = new Vector<short>(OutputWeight, p.us==BLACK ? 0 : HIDDEN_SIZE);

        // Multiply the Accumulator and the Weights
        // Dont compute the Dot Product yet to avoid overflow errors
        var mult = Multiply(activatedWhite, weightsWhite)
                 + Multiply(activatedBlack, weightsBlack);

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
