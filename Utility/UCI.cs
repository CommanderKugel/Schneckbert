public static class UCI
{

    public static void handle_uci()
    {
        Console.WriteLine("id name Schneckbert v2.0");
        Console.WriteLine("id author CommanderKugel");

        Console.WriteLine($"option name Hash type spin default 16 min {TranspositionTable.MIN_SIZE} max {TranspositionTable.MAX_SIZE}");

        Console.WriteLine("uciok");
    }

    public static void handle_isready()
    {
        Console.WriteLine("readyok");
    }

    public static void handle_setoption(string[] tokens)
    {
        if (tokens.Length != 5)
        {
            Console.WriteLine($"Unexpected Token Count! Got {tokens.Length} but expected 5.");
            Console.WriteLine("Please format the command as follows:");
            Console.WriteLine("\'setoption name <NAME> value <VALUE>\'");
            Console.WriteLine("=nly ever change one setoption value at a time.");
        }

        for (int i=0; i<tokens.Length; i++)
        {
            tokens[i] = tokens[i].ToLower();
        }

        switch (tokens[2])
        {
            case "hash":
            {
                int sizeMB = SkipPast(tokens, "value").Select(int.Parse).FirstOrDefault();
                TranspositionTable.Resize(sizeMB);
                break;
            }
            default:
            {
                Console.WriteLine($"changing the internal value for {tokens[2]} is currently not supportet.");
                break;
            }
        }
    }

    public static void handle_newgame()
    {
        TranspositionTable.Reset();
        History.Reset();
        //Corrhist.Reset();
        TimeManager.Reset(resetTotalNodes: true);
    }

    public static pos position(string[] tokens)
    {
        pos p;

        // parse for the position to start from
        if (tokens[1] == "fen")
        {
            string fen = string.Join(' ', SkipPast(tokens, "fen").Take(6));
            p = new pos();
        }
        else
        {
            if (tokens[1] != "startpos")
            {
                Console.WriteLine("Expected 'startpos' or 'fen', defaulting to 'startpos' instead.");
            }
            p = new pos(Constants.startpos);
        }

        SearchStack.Reset();
        RepetitionTable.Reset();

        // make all moves played in the game, starting from the given position
        unsafe
        {
            SS ss = new SS();
            foreach (var moveStr in SkipPast(tokens, "moves"))
            {
                move m = new move(moveStr, ref p);
                p.make_move(m, &ss);

                if (RepetitionTable.needs_set_back())
                {
                    RepetitionTable.set_back_100();
                }
            }
        }

        return p;
    }

    public static void handle_go(string[] tokens)
    {
        if (tokens.Length == 1 || tokens[1] == "infinite" || tokens[1] == "ponder")
        {
            Console.WriteLine("'go', 'go infinite' and 'ponder' are currently not available.");
        }

        // initialize as max values
        int  depth = Constants.MAX_SEARCH_PLY;
        long nodes = long.MaxValue;

        int wtime = 0, btime = 0, winc = 0, binc = 0;

        for (int i=0; i<tokens.Length; i++)
        {
            
        }
    }

    private static IEnumerable<string> SkipPast(string[] tokens, string tok) 
        => tokens.SkipWhile(t => t != tok).Skip(1);

}