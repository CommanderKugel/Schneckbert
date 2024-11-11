using static Utils;
using static Constants;

public static class NNEval
{
    private const int HL_SIZE = 8;
    
    public static int Evaluate(pos p)
    {
        // Weights_L1_W -> 768 weights for upper accumulator
        // Bias_L1_W -> 8 biases for upper accumulator

        // Weights_L1_B -> 768 weights for lower accumulator
        // Bias_L1_B -> 8 biases for lower accumulator
        
        // Weights_L2 -> 16 weights for hidden layer
        // Bias_L2 -> 1 bias for hidden layer

        return 0;
    }

}

public struct Accumulator
{
    int[] values;
}
