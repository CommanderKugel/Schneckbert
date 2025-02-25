using static Constants;
using static Utils;

using System.Diagnostics;


public enum Gameresult
{
    WinBlack = 0,
    Draw     = 1,
    WinWhite = 2,
    Ongoing  = 3,
}

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


    private static Random rng = new Random();

    public static unsafe void play_and_write(int games, int randomPly, int softnodes, int threadId, bool useUho)
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
                // read the next fen
                var fen = useUho ? UHOFile.ReadLine() : startpos;
                if (fen == null)
                {
                    UHOFile.DiscardBufferedData();
                    UHOFile.BaseStream.Seek(0, SeekOrigin.Begin);
                    continue;
                }

                // play the game and receive all the necessary information
                // alternate between white to move and balck to move
                var (root, moves, scores, result) = play(softnodes, randomPly + (cnt & 1), fen);

                // write the game to the file and count the newly added positions
                posCnt += WriteGame.write_game_as_txt(file, root, moves, scores, result);

                if (cnt % 10 == 0 && cnt > 0)
                {
                    long pps = posCnt * 1000 / watch.ElapsedMilliseconds;
                    Console.WriteLine($"time: {watch.Elapsed} total: {posCnt} pps: {pps}");
                }
            }
        }
        Console.WriteLine($"Done playing {games} games!");
    }


    public static unsafe (pos, List<move>, List<int>, Gameresult) play(int softnodes, int randomPly, string fen)
    {
        SearchStack.Reset();
        RepetitionTable.Reset();
        TranspositionTable.Reset();
        History.Reset();
        TimeManager.Reset(true);

        pos root = random_pos_after_n_ply(randomPly, fen);
        pos randoStartCopy = root;

        List<move> mainLine = new List<move>();
        List<int> mainLineScores = new List<int>();

        var result = Gameresult.Ongoing;
        SS ss = new SS();

        while (true)
        {
            bool hasLegalMoves = has_legal_moves(root);
            bool inCheck       = root.get_checkers() != 0;

            // check for illegal Positions
            if (!more_than_one(root.pieceBB[KING]))
            {
                result = root.get_pieces(KING, WHITE) == 0 ? Gameresult.WinBlack : Gameresult.WinWhite;
                break;
            }

            // checkmate detection
            if (!hasLegalMoves && inCheck)
            {
                result = root.us==WHITE ? Gameresult.WinBlack : Gameresult.WinWhite;
                break;
            }

            // draw detection
            if (!hasLegalMoves && !inCheck ||
                 RepetitionTable.IsRepeatedPosition(root) ||
                 root.IsInsufficientMaterial ||
                 root.IsFiftyMoveDraw)
            {
                result = Gameresult.Draw;
                break;
            }

            // search
            TimeManager.SetNewTimelimit(1000);
            move m = Search.iterativeDeepen(root, info: false, maxDepth: 32, maxNodes: softnodes);

            // check move legality
            bool isLegal = root.make_move(m, &ss);
            if (!isLegal)
            {
                result = root.us==WHITE ? Gameresult.WinBlack : Gameresult.WinWhite;
                break;
            }

            // save move and score
            mainLine.Add(m);
            mainLineScores.Add(Search.rootScore);

            // abort if on side has no pieces left and not everything will be traded off
            if ((!more_than_one(root.colorBB[BLACK]) || !more_than_one(root.colorBB[WHITE])) &&
                  Math.Abs(Search.rootScore) > 400)
            {
                result = Search.rootScore < 0 ? Gameresult.WinBlack : Gameresult.WinWhite;
                break;
            }

            // abort if the score becomes too high/low (includes mate-scores)
            if (Math.Abs(Search.rootScore) > 3000)
            {
                result = Search.rootScore > 0 ? (root.us == WHITE ? Gameresult.WinWhite : Gameresult.WinBlack)
                                              : (root.us == WHITE ? Gameresult.WinBlack : Gameresult.WinWhite);
                break;
            }
        }

        return (randoStartCopy, mainLine, mainLineScores, result);
    }


    public static unsafe bool has_legal_moves(pos p)
    {
        Span<move> moves = stackalloc move[MAX_MOVE_CNT];
        int mvCnt = MoveGen.GenerateMoves(ref moves, ref p, false, p.get_checkers());
        return has_legal_moves(ref moves, mvCnt, p);
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

    public static unsafe pos random_pos_after_n_ply (int n, string fen=startpos)
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

            // catch positions with no legal moves
            // mate after 4 plies can happen!
            if (cnt == 0)
            {
                return random_pos_after_n_ply(n, fen);
            }
        }

        return root;
    }
}
