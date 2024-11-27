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

    public static pos RandAfterNPly(int n)
    {
        pos root = new pos(startpos);
        int ply = 0;
        SS ss = new SS();

        while (ply < n)
        {
            move[] moves = new move[MAX_MOVE_CNT];
            int cnt = MoveGen.GenerateMoves(moves, root, false);

            while (cnt > 0)
            {
                int idx = rng.Next(cnt);
                pos copy = new pos(root);
                move m = moves[idx];

                // if move is illegal - remove it & get next one
                if (!copy.make_move(m, ref ss))
                {
                    cnt--;
                    swap(moves, idx, cnt);
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

    private static void swap(move[] moves, int a, int b)
    {
        move copy = moves[a];
        moves[a] = moves[b];
        moves[b] = copy;
    }

}
