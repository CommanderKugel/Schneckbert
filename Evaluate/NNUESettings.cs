public static class NNUESettings
{
    public const int INPUT_SIZE  = 768;
    public const int FT_SIZE     = 192;
    public const int OUTPUT_SIZE = 1;

    public const int IN_BUCKET_CNT = 1;
    
    public const int OUT_BUCKET_CNT = 8;
    public static int OUT_BUCK_DIVISOR = (int)Math.Ceiling(32.0d / (double)OUT_BUCKET_CNT);
    
    public const string net_name = "simple192_wdl50";
    
    public const int SCALE = 2;
    public const short QA = 255;
    public const short QB = 64;


    public static readonly int[] buckets = [
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
    ];
}