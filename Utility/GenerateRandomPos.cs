using System.Diagnostics;
using static Constants;

public static class RandomPosition
{
    public static Random rng = new Random();

    public static void GenerateRandomStartpositions(int ply, int cnt)
    {
        var watch = new Stopwatch();

        const string path = "C:\\Users\\nikol\\Desktop\\VS_Code_Dateien\\Ressources\\RandomBook.epd";
        using (StreamWriter file = new StreamWriter(path))
        {
            watch.Start();
            for (int n=1; n<=cnt; n++)
            {
                pos p = RandAfterNPly(ply);
                string fen = p.get_fen();

                file.WriteLine(fen);

                if (n % 1_000_000 == 0)
                {
                    long elapsed = watch.ElapsedMilliseconds / 1000;
                    long pps = n / elapsed;
                    long eta = (cnt-n) / pps;

                    Console.WriteLine("generated " + n + " positions");
                    Console.WriteLine("pps: "+pps+", eta: "+eta+"s");
                }
            }
        }
    }

    public static unsafe pos RandAfterNPly(int n)
    {
        pos root = new pos(startpos);
        int ply = 0;
        SS ss = new SS();
        SS* ps = &ss;

        while (ply < n)
        {
            move[] moves_ = new move[MAX_MOVE_CNT];
            Span<move> moves = moves_;
            int cnt = MoveGen.GenerateMoves(ref moves, ref root, false, root.get_checkers());

            while (cnt > 0)
            {
                int idx = rng.Next(cnt);
                pos copy = root;
                move m = moves[idx];

                // if move is illegal - remove it & get next one
                if (!copy.make_move(m, ps))
                {
                    cnt--;
                    (moves[idx], moves[cnt]) = (moves[cnt], moves[idx]);
                }
                else
                {
                    ply++;
                    root = copy;
                    break;
                }
            }

            if (cnt == 0)
            {
                return RandAfterNPly(n);
            }
        }

        return root;
    }
}
