using static NNUESettings;


public static class NNUEWeights
{
    public static short[][][] HiddenWeights;
    public static short[]     HiddenBias;

    public static short[] OutputWeight;
    public static short   OutputBias;


    static NNUEWeights() 
    {
        HiddenWeights = new short[IN_BUCKET_CNT][][];
        HiddenBias = new short[HIDDEN_SIZE];
        for (int i=0; i<IN_BUCKET_CNT; i++)
        {
            HiddenWeights[i] = new short[INPUT_SIZE][];
            for (int j=0; j<INPUT_SIZE; j++)
                HiddenWeights[i][j] = new short[HIDDEN_SIZE];
        }
        OutputWeight = new short[HIDDEN_SIZE * 2];
    }

    /// <summary>
    /// reads the binary file that has been trained by the open source bullet trainer
    /// https://github.com/jw1912/bullet
    /// thanks to jw1912!
    /// </summary>
    public static void init() 
    {
        const string path = "C:\\Users\\nikol\\Desktop\\VS_Code_Dateien\\Schneckbert\\Schneckbert\\Evaluate\\Nets\\";
        using (var fs     = new FileStream(path+net_name+".bin", FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(fs))
        {

            // read hidden weights
            for (int buck=0; buck<IN_BUCKET_CNT; buck++)
                for (int feat=0; feat<INPUT_SIZE; feat++)
                    for (int h=0; h<HIDDEN_SIZE; h++)
                    {
                        HiddenWeights[buck][feat][h] = reader.ReadInt16();
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
