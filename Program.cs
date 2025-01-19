using System.Text.RegularExpressions;

SearchStack.init();
Zobrist.init();
TranspositionTable.Resize(16); // initializes the TT to 16MB
History.init();
Attacks.init();
NNUEWeights.load();
Utils.init();

pos root = new pos(Constants.startpos);
string moves = "";

while (true)
{

    var command = Console.ReadLine() ?? "quit";
    var tokens = Regex.Split(command, @"\s+");
    IEnumerable<string> SkipPast(string tok) => tokens.SkipWhile(t => t != tok).Skip(1);

    switch (tokens[0])
    {
        case "uci":
        {
            Console.WriteLine("id name Schneckbert");
            Console.WriteLine("id author CommanderKugel");
            Console.WriteLine("uciok");
            break;
        }
        case "isready":
        {
            Console.WriteLine("readyok");
            break;
        }
        case "setoption":
        {
            // setoption name Hash value MBValue
            if (tokens.Length >= 5 && tokens[2] == "Hash")
            {
                int sizeMB = SkipPast("value").Select(int.Parse).FirstOrDefault();
                try
                {
                    Console.WriteLine($"info string set Hash to {TranspositionTable.Resize(sizeMB)} mb");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Could not use {sizeMB} as hash size!");
                    Console.Error.WriteLine(e.Message);
                }
            }

            break;
        }
        case "ucinewgame":
        {
            TranspositionTable.Reset();
            History.Reset();
            SearchStack.Reset();
            TimeManager.Reset(resetTotalNodes: true);
            break;
        }
        case "position":
        {
            if (tokens[1] == "startpos")
            {
                root = new pos(Constants.startpos);
            }
            else if (tokens[1] == "fen")
            {
                string fen = string.Join(' ', SkipPast("fen").Take(6));
                root = new pos(fen);
            }
            else 
            {
                Console.Error.WriteLine($"unknown uci command: {tokens[1]}");
                break;
            }

            SearchStack.Reset();
            RepetitionTable.Reset();

            moves = "";
            unsafe
            {
                fixed (SS* ss = SearchStack.stack)
                {
                    int ply=0;
                    foreach (var moveStr in SkipPast("moves"))
                    {
                        ply++;
                        moves += moveStr + " ";

                        move m = new move(moveStr, root);
                        root.make_move(m, ss);

                        if (RepetitionTable.needs_set_back())
                        {
                            RepetitionTable.set_back_100();
                        }
                    }
                }
            }
            break;
        }
        case "go":
        {  

            int depth = Constants.MAX_SEARCH_PLY;
            long nodes = long.MaxValue;

            if (tokens[1] == "wtime")
            {
                var wtime = SkipPast("wtime").Select(int.Parse).FirstOrDefault();
                var btime = SkipPast("btime").Select(int.Parse).FirstOrDefault();
                var winc  = SkipPast("winc").Select(int.Parse).FirstOrDefault();
                var binc  = SkipPast("binc").Select(int.Parse).FirstOrDefault();

                TimeManager.SetNewTimelimit(root.us == Constants.WHITE ? wtime : btime);
            }

            else if (tokens[1] == "nodes")
            {
                nodes = SkipPast("nodes").Select(int.Parse).FirstOrDefault();
                TimeManager.SetNewTimelimit(int.MaxValue);
            }

            else if (tokens[1] == "movetime")
            {
                var movetime = SkipPast("movetime").Select(int.Parse).FirstOrDefault();
                TimeManager.SetMaxTimelimit(movetime);

                if (tokens.Length == 5 && tokens[3] == "nodes")
                {
                    nodes = SkipPast("nodes").Select(int.Parse).FirstOrDefault();
                }
            }

            else if (tokens[1] == "depth")
            {
                depth = SkipPast("depth").Select(int.Parse).FirstOrDefault();
                TimeManager.SetNewTimelimit(int.MaxValue);
            }

            move bestmove = Search.iterativeDeepen(
                root, info: false, maxDepth: depth, maxNodes: nodes
            );
            Console.WriteLine($"bestmove {bestmove}");
                                
            break;
        }
        case "bench":
        {
            Bench.runBench(tokens.Length == 2
                ? int.Parse(tokens[1])
                : 10
            );
            long bench = 3418651; 
            int  nps   = 1199982; 
            Console.WriteLine("Previous Bench: " + bench + " Previous nps: " + nps);
            Console.WriteLine($"bench changed: {bench != TimeManager.TotalNodes}");
            break;
        }
        case "eval":
        {
            if (root == (new pos(Constants.startpos)))
            {
                foreach (var fen in new string[] {"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                                                    "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
                                                    "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1",
                                                    "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8",
                                                    "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1" })
                {
                    pos p = new pos(fen);
                    int eval = p.accumulator.Evaluate(ref p);
                    Console.WriteLine("FEN: "+fen);
                    Console.WriteLine("EVAL: "+eval);
                }
            }
            else 
            {
                Utils.print(root);
                Console.WriteLine("NNUE evaluation: "+root.accumulator.Evaluate(ref root));
            }
            break;
        }
        case "print":
        {
            Utils.print(root);
            break;
        }
        case "selfplay":
        {   
            string path = "C:\\Users\\nikol\\Desktop\\tuning data\\self_play_data\\simple48_17-1-25\\";
            if (tokens.Length == 2)
            {
                path += tokens[1] + ".txt";
            }

            int randomPly = 8;
            int games = 1_000_000;
            Selfplay.play_and_write(games, randomPly, path);
            break;
        }
        case "perft":
        {
            Perft.perft();
            break;
        }
        case "quit":
        {
            return 0;
        }
        default:
        {
            Console.Error.WriteLine($"unknown uci command: {tokens[0]}");
            break;
        }
    }
}

