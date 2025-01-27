
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using static Constants;
using static Utils;

public static class Selfplay
{
    private static readonly string[] UHO_paths = [
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+085_+094\UHO_2024_6mvs_+085_+094.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+085_+094\UHO_2024_8mvs_+085_+094.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+090_+099\UHO_2024_6mvs_+090_+099.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+090_+099\UHO_2024_8mvs_+090_+099.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+095_+104\UHO_2024_6mvs_+095_+104.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+095_+104\UHO_2024_8mvs_+095_+104.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+100_+109\UHO_2024_6mvs_+100_+109.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+100_+109\UHO_2024_8mvs_+100_+109.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+105_+114\UHO_2024_6mvs_+105_+114.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+105_+114\UHO_2024_8mvs_+105_+114.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+110_+119\UHO_2024_6mvs_+110_+119.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+110_+119\UHO_2024_8mvs_+110_+119.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+115_+124\UHO_2024_6mvs_+115_+124.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+115_+124\UHO_2024_8mvs_+115_+124.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+120_+129\UHO_2024_6mvs_+120_+129.epd",
        @"c:\Users\nikol\Desktop\Schneckbert\uho_2024\UHO_2024_+120_+129\UHO_2024_8mvs_+120_+129.epd",
    ];


    private static Stopwatch gameWatch = new Stopwatch();
    private static Stopwatch writeWatch = new Stopwatch();

    private static Random rng = new Random();

    public static unsafe void play_and_write(int games, int randomPly, int softnodes, int threadId)
    {
        var outPath = $"C:/Users/nikol/Desktop/Schneckbert/selfplaydata/{threadId}.txt";
        var inPath = UHO_paths[threadId % UHO_paths.Length];

        using (StreamReader UHOFile = new StreamReader(inPath))
        using (StreamWriter file = new StreamWriter(outPath, true))
        {
            var watch = new Stopwatch();
            SS ss = new SS();
            long posCnt = 0;

            watch.Start();
            for (int cnt=1; cnt<=games; cnt++)
            {
                // fetch the next fen
                var uhoFen = UHOFile.ReadLine();
                if (uhoFen == null)
                {
                    UHOFile.DiscardBufferedData();
                    UHOFile.BaseStream.Seek(0, SeekOrigin.Begin);
                    continue;
                }

                gameWatch.Start();
                // play the game and receive all the necessary information
                // alternate between white to move and balck to move
                var (root, moves, scores, result) = play(softnodes, randomPly + (cnt & 1), uhoFen);
                gameWatch.Stop();

                writeWatch.Start();
                StringBuilder builder = new StringBuilder();
                for (int i=0; i<moves.Count; i++)
                {
                    move m     = moves[i];
                    int  score = root.us==WHITE ? scores[i] : -scores[i];

                    // make the move and determine if its a quiet-position
                    root.make_move(m, &ss);
                    RepetitionTable.Pop();
                    bool isQuiet = ss.CapturedPiece == PIECE_NONE   // no capture
                                && Math.Abs(score) < 1_000_000      // no mating scores
                                && root.get_checkers() == 0;        // no checks

                    // always make the move, but filter out checks and captures
                    if (isQuiet)
                    {
                        posCnt++;
                        string fen = root.get_fen();
                        builder.Append(fen);
                        builder.Append(" | ");
                        builder.Append(score);
                        builder.Append(" | ");
                        builder.Append(result);
                        builder.Append('\n');
                    }
                }
                file.Write(builder.ToString());
                writeWatch.Stop();

                if (cnt % 10 == 0 && cnt > 0)
                {
                    long pps = posCnt * 1000 / watch.ElapsedMilliseconds;
                    long spg = watch.ElapsedMilliseconds / cnt / 1000;
                    long eta = (games-cnt) * spg;
                    Console.WriteLine($"total: {posCnt} pps: {pps}");
                }
            }
        }
        Console.WriteLine("Done playing "+games+" games!");
        Console.WriteLine($"game: {gameWatch.Elapsed}");
        Console.WriteLine($"write: {writeWatch.Elapsed}");
    }

    public static unsafe (pos, List<move>, List<int>, string) play(int softnodes, int randomPly, string fen)
    {
        SearchStack.Reset();
        RepetitionTable.Reset();
        TranspositionTable.Reset();
        History.Reset();
        TimeManager.Reset(true);

        pos root = RandAfterNPly(randomPly, fen);
        pos randoStartCopy = root;

        List<move> mainLine = new List<move>();
        List<int> mainLineScores = new List<int>();

        string result = "ongoing";
        SS ss = new SS();

        while (true)
        {
            Span<move> moves = new move[MAX_MOVE_CNT];
            int mvCnt = MoveGen.GenerateMoves(ref moves, ref root, false, root.get_checkers());

            bool hasLegalMoves = has_legal_moves(ref moves, mvCnt, root);
            bool inCheck = root.get_checkers() != 0;

            // check for illegal Positions
            if (!more_than_one(root.pieceBB[KING]))
            {
                if (root.get_pieces(KING, WHITE) == 0)
                {
                    result = "0.0";
                }
                if (root.get_pieces(KING, BLACK) == 0)
                {
                    result = "1.0";
                }
                break;
            }

            // checkmate detection
            if (!hasLegalMoves && inCheck)
            {
                result = root.us==WHITE ? "0.0" : "1.0";
                break;
            }

            // draw detection
            if (!hasLegalMoves && !inCheck ||
                 RepetitionTable.IsRepeatedPosition(root) ||
                 root.IsInsufficientMaterial ||
                 root.IsFiftyMoveDraw)
            {
                result = "0.5";
                break;
            }

            TimeManager.SetNewTimelimit(1000);
            move m = Search.iterativeDeepen(root, info: false, maxNodes: softnodes);

            bool isLegal = root.make_move(m, &ss);
            if (!isLegal)
            {
                result = root.us==WHITE ? "0.0" : "1.0";
                break;
            }

            mainLine.Add(m);
            mainLineScores.Add(Search.rootScore);


            // abort if on side has no pieces left and not everything will be traded off
            if ((!more_than_one(root.colorBB[BLACK]) || !more_than_one(root.colorBB[WHITE])) &&
                  Math.Abs(Search.rootScore) > 400)
            {
                result = Search.rootScore > 0 ? "1.0" : "0.0";
                break;
            }

            // abort if the score becomes too high
            if (Math.Abs(Search.rootScore) > 3000)
            {
                result = Search.rootScore > 0 ? "1.0" : "0.0";
                break;
            }
        }

        // Game is played out now
        return (randoStartCopy, mainLine, mainLineScores, result);
    }

    public static unsafe bool has_legal_moves(ref Span<move> moves, int mvCnt, pos p)
    {
        SS ss = new SS();
        for (int i=0; i<mvCnt; i++)
        {
            pos copy = p;
            if (copy.make_move(moves[i], &ss))
            {
                RepetitionTable.Pop();
                return true;
            }
        }
        return false;
    }

    public static unsafe pos RandAfterNPly(int n, string fen)
    {
        pos root = new pos(fen);
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
                return RandAfterNPly(n, fen);
            }
        }

        return root;
    }
}
