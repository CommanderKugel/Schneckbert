
using System.Diagnostics;
using static Constants;
using static Utils;

public static class Selfplay
{

    public static unsafe void play_and_write(int games, int randomPly, string path)
    {
        using (StreamWriter file = new StreamWriter(path, true))
        {
            var watch = new Stopwatch();
            SS ss = new SS();
            long posCnt = 0;

            watch.Start();
            for (int cnt=1; cnt<=games; cnt++)
            {
                // play the game and receive all the necessary information
                var (root, moves, scores, result) = play(randomPly);

                if (result == "stalemate")
                {
                    Console.WriteLine(result);
                    Console.WriteLine(root.get_fen());
                    foreach (move m in moves)
                    {
                        Console.Write(m.ToString() + " ");
                    }
                }

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
                        file.WriteLine(fen + " | " + score + " | " + result);
                    }
                }

                if (cnt % 10 == 0 && cnt > 0)
                {
                    long pps = posCnt * 1000 / watch.ElapsedMilliseconds;
                    long spg = watch.ElapsedMilliseconds / cnt / 1000;
                    long eta = (games-cnt) * spg;
                    Console.WriteLine($"total positions {posCnt} total games {cnt}");
                    Console.WriteLine($"pps {pps} eta {eta}s");
                }
            }
        }
        Console.WriteLine("Done playing "+games+" games!");
    }

    public static unsafe (pos, List<move>, List<int>, string) play(int randomPly)
    {
        SearchStack.Reset();
        RepetitionTable.Reset();
        TranspositionTable.Reset();
        History.Reset();
        TimeManager.Reset(true);

        pos root = RandomPosition.RandAfterNPly(randomPly);
        pos randoStartCopy = root;

        List<move> mainLine = new List<move>();
        List<int> mainLineScores = new List<int>();

        string result = "ongoing";
        SS ss = new SS();
        TimeManager.SetNewTimelimit(int.MaxValue);

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
            if (!hasLegalMoves && !inCheck)
            {
                result = "0.5";
                break;
            }
            if (root.IsFiftyMoveDraw)
            {
                result = "0.5";
                break;
            }
            if (RepetitionTable.IsRepeatedPosition(root))
            {
                result = "0.5";
                break;
            }
            if (root.IsInsufficientMaterial)
            {
                result = "0.5";
                break;
            }

            move m = Search.iterativeDeepen(root, info: false, maxNodes: 5000);

            bool isLegal = root.make_move(m, &ss);
            if (!isLegal)
            {
                result = root.us==WHITE ? "0.0" : "1.0";
                break;
            }

            mainLine.Add(m);
            mainLineScores.Add(Search.rootScore);
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
}
