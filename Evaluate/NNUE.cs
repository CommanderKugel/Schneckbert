using static Constants;

using System.Runtime.CompilerServices;


public static class NNUE
{
    public const int INPUT_SIZE = 768;
    public const int HIDDEN_SIZE = 16;
    public const int OUTPUT_SIZE = 1;

    /*
    Accumulator     = short[HIDDNE_SIZE]        16 * 2
    feature weights = Accumulator[768]          16 * 2 * 768 = 24.576
    feature bias    = Accumulator               16 * 2       = 32
    output Weights  = short[2 * HIDDEN_SIZE]    16 * 2 * 2   = 64
    output bias     = short                     2            = 2
    */ 
    public static short[] HiddenWeights = new short[INPUT_SIZE * HIDDEN_SIZE];
    public static short[] HiddenBias    = new short[HIDDEN_SIZE];

    public static short[] OutputWeight  = new short[HIDDEN_SIZE * 2];
    public static short   OutputBias    = 0;


    const int SCALE = 400;
    const int QA = 255;
    const int QB = 64;


    /// <summary>
    /// Clamped Rectified Linear Unit
    /// Activation Function for accumulated values
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int crelu(int x) => Math.Clamp(x, 0, QA);


    

    /// <summary>
    /// Returns the static Evaluation of the position.
    /// Makes use of the positions Accumulators.
    /// QS = 255, QB = 64, SCALE = 400
    /// </summary>
    public static unsafe int Evaluate(ref pos p)
    {
        fixed (short* wptr = p.accumulator.AccumulatorWhite)
        fixed (short* bptr = p.accumulator.AccumulatorBlack)
        {
            int sum = OutputBias;

            // Perspective - order Accumulators based on side to move
            short*   our_acc = p.us==WHITE ? wptr : bptr;
            short* their_acc = p.us==WHITE ? bptr : wptr;

            // first activate the values, then sum up the accumulators
            for (int i=0; i<HIDDEN_SIZE; i++)
            {
                sum += crelu(  our_acc[i]) * OutputWeight[i];
                sum += crelu(their_acc[i]) * OutputWeight[i+HIDDEN_SIZE];
            }

            // Scale from small original floating point numbers
            // about centipawns now
            sum *= SCALE;

            // Remove Quantization
            sum /= QA * QB;

            return Math.Clamp(sum, -EVAL_SCORE_MAX, EVAL_SCORE_MAX);
        }
    }

    
    public static readonly string NET_NAME = main;
    public const string main = "simple16_hm";

    /// <summary>
    /// reads the binary file that has been trained by the open source bullet trainer
    /// https://github.com/jw1912/bullet
    /// thanks to jw1912!
    /// </summary>
    public static void init()
    {
        
        const string path = "C:\\Users\\nikol\\Desktop\\VS_Code_Dateien\\Schneckbert\\Schneckbert\\Evaluate\\Nets\\";
        using (FileStream fs = new FileStream(path+NET_NAME+".bin", FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fs))
        {

            // read hidden weights
            for (int i=0; i<HIDDEN_SIZE*INPUT_SIZE; i++)
            {
                HiddenWeights[i] = reader.ReadInt16();
            }

            // read hidden bias
            for (int i=0; i<HIDDEN_SIZE; i++)
            {
                HiddenBias[i] = reader.ReadInt16();
            }

            // read output weights
            for (int i=0; i<HIDDEN_SIZE*2; i++)
            {
                OutputWeight[i] = reader.ReadInt16();
            }

            // read output bias
            OutputBias = reader.ReadInt16();
        }
    }
}
