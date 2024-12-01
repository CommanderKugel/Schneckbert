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
    try
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
                foreach (var moveStr in SkipPast("moves"))
                {
                    moves += moveStr + " ";
                    move m = new move(moveStr, root);
                    root.make_move(m, ref SearchStack.stack[0]);
                    RepetitionTable.Push(root.ZobristKey);
                }
                break;
            }
            case "go":
            {  
                var wtime = SkipPast("wtime").Select(int.Parse).FirstOrDefault();
                var btime = SkipPast("btime").Select(int.Parse).FirstOrDefault();
                var winc  = SkipPast("winc").Select(int.Parse).FirstOrDefault();
                var binc  = SkipPast("binc").Select(int.Parse).FirstOrDefault();

                TimeManager.SetNewTimelimit(root.us == Constants.WHITE ? wtime : btime);

                move bestmove = Search.iterativeDeepen(root, info: false);
                Console.WriteLine($"bestmove {bestmove}");
                
                break;
            }
            case "quit":
            {
                return 0;
            }
            case "bench" or "Bench":
            {
                Bench.runBench(tokens.Length == 2
                    ? int.Parse(tokens[1])
                    : 5
                );
                long bench = 1028220; 
                int  nps   = 1187321; 
                Console.WriteLine("Previous Bench: " + bench + " Previous nps: " + nps);
                Console.WriteLine($"bench changed: {bench != TimeManager.TotalNodes}");
                break;
            }
            case "eval" or "evaluate":
            {
                Utils.print(root);
                int eval = Evaluation.Evaluate(root);
                Console.WriteLine(eval);
                break;
            }
            case "print":
            {
                Utils.print(root);
                break;
            }
            case "load":
            {
                NNUE.init();
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
            default:
            {
                Console.Error.WriteLine($"unknown uci command: {tokens[0]}");
                break;
            }
        }
    }

    catch (Exception e)
    {
        using (StreamWriter file = new StreamWriter("C:\\Users\\nikol\\Desktop\\fastchess\\error_dev.txt"))
        {
            file.WriteLine("dev");
            file.WriteLine(moves);
            file.WriteLine(e.Message);
            file.WriteLine(e.StackTrace);

            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            Console.WriteLine("Du catch'st immernoch alles! so debuggst du falsch!");
        }
        Console.WriteLine("bestmove a1a1");
        continue;
    }
}
