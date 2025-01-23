public static class NNUESettings
{
    public const int INPUT_SIZE = 768;
    public const int HIDDEN_SIZE = 96;
    public const int OUTPUT_SIZE = 1;

    public const int IN_BUCKET_CNT = 1;
    
    public const string net_name = "simple96_hm_screlu";
    
    public const string bucket = "simple16_hm_buckets";

    public const int SCALE = 2;
    public const short QA = 181;
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