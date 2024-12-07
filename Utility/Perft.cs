
using static MoveGen;

public static class Perft
{
    private struct testPosition 
    {
        public string name, fen;
        public int depth, nodes;
        public testPosition(string name, string fen, int depth, int nodes) {
            this.name  = name;
            this.fen   = fen;
            this.depth = depth;
            this.nodes = nodes;
        }
    }

    static readonly testPosition[] positions = {
        new ("startpos", "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 5, 4865609),
        new ("kiwipete", "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 5, 193690690),
        new ("pos 3", "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 7, 178633661),
        new ("pos 4", "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 5, 15833292),
        new ("pos 5", "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 5, 89941194),
        new ("pos 6", "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10", 5, 164075551),
    };

    public static unsafe void perft()
    {
        SS ss = SearchStack.stack[0];
        foreach (var tp in positions)
        {
            pos p = new(tp.fen);
            long nodes = recurse(tp.depth, p, &ss);
            Console.WriteLine($"{tp.name} : {nodes}/{tp.nodes} - {nodes == tp.nodes}");
        }
    }


    public static unsafe void goPerft(int depth, pos root) 
    {
        SS ss = SearchStack.stack[0];
        Span<move> moves = new move[213];
        int moveCnt = GenerateMoves(moves, ref root, false, root.get_checkers());


        for (int i=0; i<moveCnt; i++) 
        {
            ref move m = ref moves[i];
            pos copy = new(root);

            if (!copy.make_move(m, &ss)) {
                //Console.WriteLine($"{m} - illegal");
                continue;
            }
            RepetitionTable.Pop();

            long nodes = recurse(depth-1, copy, &ss);
            Console.WriteLine($"{m}- {nodes}");
        }
    }

    private static unsafe long recurse(int depth, pos p, SS* ss) 
    {
        if (depth <= 0)
            return 1;

        Span<move> moves = new move[213];
        int moveCnt = GenerateMoves(moves, ref p, false, p.get_checkers());
        long nodes = 0;

        for (int i=0; i<moveCnt; i++) 
        {
            ref move m = ref moves[i];
            pos copy = new(p);

            if (!copy.make_move(m, ss))
                continue;
            RepetitionTable.Pop();

            nodes += recurse(depth-1, copy, ss);
        }
        return nodes;
    }
}
