using static System.Math;


public static class SPRT
{   /// <summary>
    /// defines the llr-bounds, 0,05 gives the well known +/-2.94
    /// if llr surpasses this threshhold, we can draw a conclusion
    /// </summary>
    static double a (int alpha, int beta) => Log(beta  / (1-alpha));
    static double b (int alpha, int beta) => Log(alpha / (1-beta));

    // helper methods
    public static double BayesElo (double x) => 1 / (1 + Pow(10, -x/400));
    public static double DrawElo (double n, double w, double l) {
        return 200 * Log10(((1/(w/n)) - 1) * ((1/(l/n)) - 1));
    }

    /// <summary>
    /// ub (upper bound) is the expected elo-gain of the change
    /// lb (lower bound) is the opposite hypothesis, that elo didnt increase
    /// returns the log-likelyhood-ratio that either hypothesis is correct
    /// </summary>
    public static double calc_llr (int N, int W, int L, int ub=5, int lb=0) {
        int D = N - W - L;
        double y = DrawElo(N, W, L);

        double w1 = BayesElo(-y + ub);
        double w0 = BayesElo(-y + lb);
        double l1 = BayesElo(-y - ub);
        double l0 = BayesElo(-y - lb);
        double d1 = 1 - w1 - l1;
        double d0 = 1 - w0 - l0;

        return W*Log(w1/w0) + L*Log(l1/l0) + D*Log(d1/d0);
    }
}
