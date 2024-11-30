using static Constants;
using static Utils;


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
    private static short[] HiddenWeights = new short[INPUT_SIZE * HIDDEN_SIZE];
    private static short[] HiddenBias    = new short[HIDDEN_SIZE];

    private static short[] OutputWeight  = new short[HIDDEN_SIZE * 2];
    private static short   OutputBias    = 0;


    const int SCALE = 400;
    const int QA = 255;
    const int QB = 64;


    public struct Accumulator
    {
        public short[] AccumulatorWhite;
        public short[] AccumulatorBlack;

        public Accumulator(pos p)
        {
            AccumulatorWhite = new short[HIDDEN_SIZE];
            AccumulatorBlack = new short[HIDDEN_SIZE];
            accumulate_from_zero(p);
        }

        public Accumulator (Accumulator parent)
        {
            AccumulatorWhite = new short[HIDDEN_SIZE];
            AccumulatorBlack = new short[HIDDEN_SIZE];
            Array.Copy(parent.AccumulatorWhite, AccumulatorWhite, HIDDEN_SIZE);
            Array.Copy(parent.AccumulatorBlack, AccumulatorBlack, HIDDEN_SIZE);
        }

        public void activate(int color, int pt, int sq)
        {
            var (us_feat, them_feat) = get_768_idx(color, pt, sq);

            for (int i=0; i<HIDDEN_SIZE; i++)
            {
                AccumulatorWhite[i] += HiddenWeights[HIDDEN_SIZE * us_feat   + i];
                AccumulatorBlack[i] += HiddenWeights[HIDDEN_SIZE * them_feat + i];
            }
        }

        public void deactivate(int color, int pt, int sq)
        {
            var (us_feat, them_feat) = get_768_idx(color, pt, sq);

            for (int i=0; i<HIDDEN_SIZE; i++)
            {
                AccumulatorWhite[i] -= HiddenWeights[HIDDEN_SIZE * us_feat   + i];
                AccumulatorBlack[i] -= HiddenWeights[HIDDEN_SIZE * them_feat + i];
            }
        }

        /// <summary>
        /// resets the Accumulator, then activates according to the given position
        /// </summary>
        public void accumulate_from_zero(pos p)
        {
            Array.Copy(HiddenBias, AccumulatorWhite, HIDDEN_SIZE);
            Array.Copy(HiddenBias, AccumulatorBlack, HIDDEN_SIZE);

            // set all Pieces
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

        public static bool operator ==(Accumulator lhs, Accumulator rhs)
        {
            for (int i=0; i<HIDDEN_SIZE; i++)
            {                
                if (lhs.AccumulatorWhite[i] != rhs.AccumulatorWhite[i] ||
                    lhs.AccumulatorBlack[i] != rhs.AccumulatorBlack[i])
                {
                    return false;
                }
            }
            return true;
        }
        public static bool operator !=(Accumulator lhs, Accumulator rhs) => !(lhs==rhs);

    } // struct accumulator


    /// <summary>
    /// Clamped Rectified Linear Unit
    /// Activation Function for accumulated values
    /// </summary>
    private static int crelu(int x) => Math.Clamp(x, 0, QA);


    /// <summary>
    /// returns the index for the 768 input representation
    /// one for the view of side to move, and one for not side to move
    /// uses color=WHITE for stm and color=BLACK for not stm
    /// </summary>
    private static (int, int) get_768_idx(int color, int pt, int sq)
    {
        int wfeat = (1-color) * 384 + pt * 64 +  sq;
        int bfeat = (  color) * 384 + pt * 64 + (sq ^ 56);

        return (wfeat, bfeat);
    }

    public static int Evaluate(pos p) => Evaluate(p.accumulator, p.us);

    public static int Evaluate(Accumulator a, int stm)
    {
        //accumulate_from_zero(p);

        int sum = OutputBias;

        // Perspective - order Accumulators based on side to move
        var (our_acc, their_acc) = stm==WHITE ? (a.AccumulatorWhite, a.AccumulatorBlack) 
                                              : (a.AccumulatorBlack, a.AccumulatorWhite);

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

        return sum;
    }

    /// <summary>
    /// reads the binary file that has been trained by the bullet trainer
    /// https://github.com/jw1912/bullet
    /// thanks to jw1912!
    /// </summary>
    public static void init()
    {
        const string path = "C:\\Users\\nikol\\Desktop\\VS_Code_Dateien\\Schneckbert\\Schneckbert\\Evaluate\\";
        using (FileStream fs = new FileStream(path+"simple_quantized.bin", FileMode.Open, FileAccess.Read))
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

            OutputBias = reader.ReadInt16();
        }
    }
}
