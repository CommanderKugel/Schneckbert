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
        var VECTOR_QA = new Vector<short>(QA);
        var actWhite16 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccWhite16));
        var actWhite32 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccWhite32));
        var actWhite48 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccWhite48));
        var actWhite64 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccWhite64));
        var actWhite80 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccWhite80));
        var actWhite96 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccWhite96));

        var actBlack16 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccBlack16));
        var actBlack32 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccBlack32));
        var actBlack48 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccBlack48));
        var actBlack64 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccBlack64));
        var actBlack80 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccBlack80));
        var actBlack96 = Max(Vector<short>.Zero, Min(VECTOR_QA, AccBlack96));

        // load output weights into Vectors
        var weightsWhite16 = new Vector<short>(OutputWeight, p.us==WHITE ? 0  : HIDDEN_SIZE);
        var weightsWhite32 = new Vector<short>(OutputWeight, p.us==WHITE ? 16 : HIDDEN_SIZE+16);
        var weightsWhite48 = new Vector<short>(OutputWeight, p.us==WHITE ? 32 : HIDDEN_SIZE+32);
        var weightsWhite64 = new Vector<short>(OutputWeight, p.us==WHITE ? 48 : HIDDEN_SIZE+48);
        var weightsWhite80 = new Vector<short>(OutputWeight, p.us==WHITE ? 64 : HIDDEN_SIZE+64);
        var weightsWhite96 = new Vector<short>(OutputWeight, p.us==WHITE ? 80 : HIDDEN_SIZE+80);

        var weightsBlack16 = new Vector<short>(OutputWeight, p.us==BLACK ? 0  : HIDDEN_SIZE);
        var weightsBlack32 = new Vector<short>(OutputWeight, p.us==BLACK ? 16 : HIDDEN_SIZE+16);
        var weightsBlack48 = new Vector<short>(OutputWeight, p.us==BLACK ? 32 : HIDDEN_SIZE+32);
        var weightsBlack64 = new Vector<short>(OutputWeight, p.us==BLACK ? 48 : HIDDEN_SIZE+48);
        var weightsBlack80 = new Vector<short>(OutputWeight, p.us==BLACK ? 64 : HIDDEN_SIZE+64);
        var weightsBlack96 = new Vector<short>(OutputWeight, p.us==BLACK ? 80 : HIDDEN_SIZE+80);

        // Multiply the Accumulator and the Weights
        // Dont compute the Dot Product yet to avoid overflow errors
        var mult16 = Multiply(actWhite16, weightsWhite16) + Multiply(actBlack16, weightsBlack16);
        var mult32 = Multiply(actWhite32, weightsWhite32) + Multiply(actBlack32, weightsBlack32);
        var mult48 = Multiply(actWhite48, weightsWhite48) + Multiply(actBlack48, weightsBlack48);
        var mult64 = Multiply(actWhite64, weightsWhite64) + Multiply(actBlack64, weightsBlack64);
        var mult80 = Multiply(actWhite80, weightsWhite80) + Multiply(actBlack80, weightsBlack80);
        var mult96 = Multiply(actWhite96, weightsWhite96) + Multiply(actBlack96, weightsBlack96);

        // widen the <short> datatype to <int>
        int sum = OutputBias;
        Vector<int> lower, upper;

        Widen(mult16, out lower, out upper);
        sum += Sum(lower) + Sum(upper);

        Widen(mult32, out lower, out upper);
        sum += Sum(lower) + Sum(upper);

        Widen(mult48, out lower, out upper);
        sum += Sum(lower) + Sum(upper);

        Widen(mult64, out lower, out upper);
        sum += Sum(lower) + Sum(upper);

        Widen(mult80, out lower, out upper);
        sum += Sum(lower) + Sum(upper);

        Widen(mult96, out lower, out upper);
        sum += Sum(lower) + Sum(upper);

        // Scaling from small original floating point numbers
        // comparable to ~centipawns now
        sum *= SCALE;

        // Remove Quantization
        sum /= QA * QB;

        return Clamp(sum, -EVAL_SCORE_MAX, EVAL_SCORE_MAX);
    }
}
