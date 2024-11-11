using System.Text.RegularExpressions;

Zobrist.init();
TranspositionTable.init(1 << 17); // should be 1MB (i hope)
History.init();
Attacks.init();


pos root = new pos(Perft.startpos);

try
{
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
            // init Board
            Console.WriteLine("readyok");
            break;
        }
        case "ucinewgame":
        {
            TranspositionTable.Reset();
            History.Reset();
            // Board
            TimeManager.Reset(resetTotalNodes: true);
            break;
        }
        case "position":
        {
            if (tokens[1] == "startpos")
            {
                root = new pos(Perft.startpos);
            }
            else
            {
                string fen = string.Join(' ', SkipPast("fen").Take(6));
                root = new pos(fen);
            }

            RepetitionTable.Reset();

            foreach (var moveStr in SkipPast("moves"))
            {
                move m = new move(moveStr, root);
                root.make_move(m);
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
            Bench.runBench();
            long bench = 4071904; 
            int  nps   = 433365; 
            Console.WriteLine("Previous Bench: " + bench + " Previous nps: " + nps);
            Console.WriteLine($"bench changed: {bench != TimeManager.TotalNodes}");
            break;
        }
        case "Perft" or "perft":
        {
            Perft.perft();
            break;
        }
        case "print":
        {
            Utils.print(root);
            break;
        }
        case "tune" or "Tune":
        {
            Tuner.Tune();
            break;
        }
        default:
        {
            Console.Error.WriteLine($"unknown uci command: {tokens[0]}");
            break;
        }
    }
}
}
catch
{
    Console.WriteLine("bestmove a1a1");
    return 1;
}
