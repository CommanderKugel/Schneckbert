
#define IsTapered


using static Constants;
using static Utils;

using static System.Math;
using System.Diagnostics;


public struct coef_entry 
{
    public sbyte value;
    public short index;
    public coef_entry(int value, int index) {
        this.value = (sbyte)value; 
        this.index = (short)index;
    }
}

class Datapoint
{
    public List<coef_entry> coefficients = new List<coef_entry>();
    public double label;
    public sbyte wtm;
    public value const_term;

#if IsTapered
    public byte phase = 0;
#endif


    public Datapoint(string fen)
    {
        pos p = new pos(fen);

    #if IsTapered
        phase = (byte)Min(
            1 * popCount(p.get_pieces(KNIGHT)) +
            1 * popCount(p.get_pieces(BISHOP)) +
            2 * popCount(p.get_pieces(ROOK)  ) +
            4 * popCount(p.get_pieces(QUEEN) ),
            24);
    #endif

        // start implementing your evaluation here


        // Mobility only
        for (int c=WHITE; c>=BLACK; c--) 
        {
            sbyte sign = (sbyte)(c==WHITE ? 1 : -1);
            ulong enemyOrEmpty = ~p.colorBB[c];

            for (int pt=KNIGHT; pt<=QUEEN; pt++)
            {
                ulong piece_bb = p.get_pieces(pt, WHITE);
                while (piece_bb != 0)
                {
                    int sq = popLsb(ref piece_bb);

                    // mobility
                    int mob = popCount(Attacks.PieceAttacks(p, pt, sq) & enemyOrEmpty);

                    int idx = pt==KNIGHT ? 0
                            : pt==BISHOP ? 9
                            : pt==ROOK   ? 9+14
                            : pt==QUEEN  ? 9+14+15
                            : 0;
                    
                    add_coef(idx + mob, sign);
                }
            }
        }

        // stop implementing your evaluation here

        void add_coef(int idx, sbyte val) {
            for (int i=0; i<coefficients.Count; i++)
            {
                var coef = coefficients[i];
                if (coef.index == idx) {
                    coef.value += val;
                    return;
                }
            }
            coefficients.Add(new coef_entry(val, idx));
        }

        if      (fen.Contains("0.0")) label = 0.0d;
        else if (fen.Contains("0.5")) label = 0.5d;
        else if (fen.Contains("1.0")) label = 1.0d;
        else throw new Exception("cant find a game result in fen " + fen);

        wtm = (sbyte)(p.us==WHITE ? 1 : -1);
    }
}


public struct value
{
#if IsTapered
    public double mg;
    public double eg;
    public value (int mg, int eg) {
        this.mg = mg;
        this.eg = eg;
    }
    public string ts(double PawnEg) => $"S({Round(mg, 0)}, {Round(eg, 0)}), ";
#else
    public double val;
    public value (int mg, int eg) => val = (mg + eg) / 2;
    public value (int val)        => this.val = val;
    public override string ToString() => $"{(int)val}, ";
    public string ts(double PawnEg) => $"{val}, ";
#endif
}

public static class Tuner
{        

    const string source_path = "C:\\Users\\nikol\\Desktop\\VS_Code_Dateien\\lichess-big3-resolved.book";
    
    static double K           = 2.5; // if K <= 0 a new K value will be calculated
    static double lr          = 1000;
    const int    LR_DROP      = 1_000;
    const double LR_DROP_RATE = 1;
    const int    MAX_EPOCH    = 5_000;

    // Print info and values after REPORT amount of epochs
    const int REPORT = 50;

    const int LICHESS_POS_CNT = 7_153_653;
    const int TUNE_POS_CNT    = 1_000_000;


    const int KNIGHT_MOB_NB = 9;
    const int BISHOP_MOB_NB = 14;
    const int ROOK_MOB_NB = 15;
    const int QUEEN_MOB_NB = 28;

    static value[] my_values;
    static void init_values()
    {
        my_values = new value[KNIGHT_MOB_NB + BISHOP_MOB_NB + ROOK_MOB_NB + QUEEN_MOB_NB];
        return;
    }

    static void print_values(value[] values, bool pretty=false)
    {
    #if IsTapered
        double pawnVal = values[0].eg / 100;
    #else
        double pawnVal = values[0].val / 100;
    #endif

        for (int i=0; i<values.Length; i++)
        {
            if (i == 0) 
                Console.WriteLine("\nKnight Mobility:");
            if (i == KNIGHT_MOB_NB)     
                Console.WriteLine("\nBishop Mobility:");
            if (i == KNIGHT_MOB_NB+BISHOP_MOB_NB) 
                Console.WriteLine("\nRook Mobility:");
            if (i == KNIGHT_MOB_NB+BISHOP_MOB_NB+ROOK_MOB_NB) 
                Console.WriteLine("\nQueen Mobility:");
            Console.Write(values[i].ts(pawnVal));
        }
        Console.WriteLine("\n");
    }


    public static void Tune(string path=source_path)
    {
        var watch = new Stopwatch();
        init_values();

        // #1 load the training data
        Datapoint[] data = read_training_data(path);

        // #2 get tuning parameters
        value[] values = my_values;

        // #4 Load or Compute K Value
        if (K <= 0)
        {
            watch.Start();
            K = comute_optimal_K(values, data);
            Console.WriteLine("new K value is " + Round(K, 4).ToString());
            Console.WriteLine($"\t computed in {(int)(watch.ElapsedMilliseconds / 1000)}s");
        }
        else
        {
            Console.WriteLine("Using precomputed K value.");
            Console.WriteLine($"K is {Round(K, 4)}");
        }     

        // #5 prepare main loop
        // lets not use optimizers for now

        // #6 epoch-loop
        watch.Restart();
        for (int epoch=1; epoch<=MAX_EPOCH; epoch++)
        {
            
            // #7 compute the gradient
            value[] gradient = new value[values.Length];
            calculate_gradient(K, data, my_values, gradient);

            // #8 gradient descent step
            step(K, my_values, gradient);

            // #9 occasional data printing
            if (epoch % REPORT == 0) 
            {
                double error = get_mean_squared_error(K, my_values, data);
                double eps = (double)epoch * 1000.0d / (double)watch.ElapsedMilliseconds;
                double eta = (MAX_EPOCH-epoch) / eps / 60;
                
                Console.Clear();
                Console.WriteLine($"\n\nTraining Data size {data.Length} positions");
                Console.WriteLine($"K value {Round(K, 4)}\n");
                Console.WriteLine($"epoch {epoch}, error {error}");
                Console.WriteLine($"\t{Round(eps, 2)} epochs/s");
                Console.WriteLine($"\tmin left {Round(eta, 2)}");;

                print_values(values);
            }

            if (epoch % LR_DROP == 0)
            {
                lr *= LR_DROP_RATE;
            }
        }

        // #10 stop tuning @max epoch & save parameters
        Console.WriteLine("Finished Tuning");
        print_values(values, pretty: true);
    }

    static Datapoint[] read_training_data(string path)
    {
        List<Datapoint> data = new List<Datapoint>();
        int cnt = Min(TUNE_POS_CNT, LICHESS_POS_CNT);
        Console.WriteLine($"Starting to read {cnt} fens");

        using (StreamReader file = new StreamReader(path))
        {
            for (int i=0; i<cnt; i++) 
            {
                string fen = file.ReadLine();
                Datapoint dp = new Datapoint(fen);
                data.Add(dp);

                if (i % 1_000_000 == 0) 
                {
                    Console.WriteLine($"read through {i} fens");
                }
            }
        }
        Console.WriteLine($"{cnt} fens were read from the dataset\n");
        return data.ToArray();
    }

    static void calculate_gradient(double K, Datapoint[] data, value[] values, value[] gradient)
    {
        foreach (Datapoint dp in data)
        {
            calculate_single_gradient(K, dp, values, gradient);
        }
    }

    static void calculate_single_gradient(double K, Datapoint dp, value[] values, value[] gradient) 
    {
        double eval = evaluate_linearly(values, dp);
        double sigm = sigmoid(eval, K);
        double error = (dp.label - sigm) * sigm * (1 - sigm);

        #if IsTapered
        double mg_base = error * (double)(   dp.phase) / 24.0d;
        double eg_base = error * (double)(24-dp.phase) / 24.0d;
        #endif

        foreach (var coef in dp.coefficients)
        {
            #if IsTapered
            gradient[coef.index].mg += mg_base * coef.value;
            gradient[coef.index].eg += eg_base * coef.value;
            #else
            gradient[coef.index].val += error * coef.value;
            #endif
        }
    }
    
    static void step(double K, value[] values, value[] gradient)
    {
        for (int val_idx=0; val_idx<values.Length; val_idx++)
        {
            #if IsTapered
            values [val_idx].mg += (K / 200.0) * (gradient[val_idx].mg / TUNE_POS_CNT) * lr;
            values [val_idx].eg += (K / 200.0) * (gradient[val_idx].eg / TUNE_POS_CNT) * lr; 
            #else
            values [val_idx].val += (K / 200.0) * (gradient[val_idx].val / TUNE_POS_CNT) * lr / TUNE_POS_CNT; 
            #endif
        }
    }

    static double get_mean_squared_error(double K, value[] values, Datapoint[] data)
    {
        double total_error = 0;
        foreach (Datapoint dp in data) 
        {
            double eval = evaluate_linearly(values, dp);
            double sigm = sigmoid(eval, K);
            double diff = dp.label - sigm;
            double error = Pow(diff, 2);
            total_error += error;
        }
        return total_error / data.Length;
    }

    static double evaluate_linearly(value[] values, Datapoint dp)
    {
    #if IsTapered
        double mg  = dp.const_term.mg;
        double eg  = dp.const_term.eg;
        foreach (coef_entry entry in dp.coefficients)
        {
            mg += values[entry.index].mg * entry.value;
            eg += values[entry.index].eg * entry.value;
        }
        return (mg * dp.phase + eg * (24 - dp.phase)) / (dp.wtm * 24);
    #else
        double eval = 0;
        foreach (coef_entry entry in dp.coefficients)
        {
            eval += values[entry.index].val * entry.value;
        }
        return eval * dp.wtm;
    #endif
    }

    static double sigmoid(double x, double K) => 1.0d / (1.0d + Exp(-K * x / 400.0d));

    public static value S(int mg, int eg) => new value(mg, eg);

    static double comute_optimal_K (value[] values, Datapoint[] data) 
    {
        const double rate     = 10;
        const double delta    = 0.000_01;
        const double dev_goal = 0.000_001;
        double K   = 2.5;
        double dev = 1;

        Console.WriteLine("Starting to compute optimal K value");
        while (Abs(dev) > dev_goal)
        {
            double up   = get_mean_squared_error(K + delta, values, data);
            double down = get_mean_squared_error(K - delta, values, data);
            dev = (up - down) / (2 * delta);
            K -= dev * rate;
        }
        return K;
    }
}
