using System.Text.RegularExpressions;

SearchStack.init();
Zobrist.init();
TranspositionTable.init(1 << 17); // should be 1MB (i hope)
History.init();
Attacks.init();

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
            NNUE.init();
            Utils.init();
            Console.WriteLine("readyok");
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
                    foreach (var moveStr in SkipPast("moves"))
                    {
                        moves += moveStr + " ";
                        move m = new move(moveStr, root);
                        root.make_move(m, ss);
                        RepetitionTable.Push(root.ZobristKey);
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
                : 7
            );
            long bench = 2017255; 
            int  nps   = 1220181; 
            Console.WriteLine("Previous Bench: " + bench + " Previous nps: " + nps);
            Console.WriteLine($"bench changed: {bench != TimeManager.TotalNodes}");
            break;
        }
        case "eval":
        {
            Utils.print(root);
            Console.WriteLine("NNUE evaluation: "+NNUE.Evaluate(ref root));
            break;
        }
        case "print":
        {
            Utils.print(root);
            break;
        }
        case "selfplay":
        {   
            string path = "C:\\Users\\nikol\\Desktop\\VS_Code_Dateien\\Ressources\\Selfplaydata_24-11-24.txt";
            if (tokens.Length == 3)
            {
                path = tokens[2];
            }

            int randomPly = 8;
            int games = 250_000;
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
