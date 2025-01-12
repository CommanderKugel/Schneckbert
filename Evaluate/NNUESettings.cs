public static class NNUESettings
{
    public const int INPUT_SIZE = 768;
    public const int HIDDEN_SIZE = 32;
    public const int OUTPUT_SIZE = 1;

    public const int IN_BUCKET_CNT = 1;
    
    public const string net_name = main;
    public const string main = "simple32_hm";
    public const string simple16_hm = "simple16_hm";
    public const string bucket = "simple16_hm_buckets";

    public const int SCALE = 400;
    public const short QA = 255;
    public const short QB = 64;


    public static readonly int[] buckets = net_name != bucket ? [
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
    ] : [ // scheme for simple16_hm_buckets3
        0, 0, 1, 1, 4, 4, 3, 3,
        2, 2, 2, 2, 5, 5, 5, 5,
        2, 2, 2, 2, 5, 5, 5, 5,
        2, 2, 2, 2, 5, 5, 5, 5,
        2, 2, 2, 2, 5, 5, 5, 5,
        2, 2, 2, 2, 5, 5, 5, 5,
        2, 2, 2, 2, 5, 5, 5, 5,
        2, 2, 2, 2, 5, 5, 5, 5,
    ];
}