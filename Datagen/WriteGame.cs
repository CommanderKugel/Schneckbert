using System.Text;
using static Constants;
using static Utils;

public static class WriteGame
{

    public static unsafe long write_game_as_txt(StreamWriter file, pos p, List<move> moves, List<int> scores, Gameresult result)
    {
        SS ss = new SS();
        var myStringbuilder = new StringBuilder(100 * moves.Count);

        long posCnt = 0;

        string res = result switch {
            Gameresult.Draw     => "0.5",
            Gameresult.WinWhite => "1.0",
            Gameresult.WinBlack => "0.0",
            _                   => "?.?"
        };
        
        for (int ply=0; ply<moves.Count; ply++)
        {
            move m = moves[ply];
            int score = scores[ply];

            if (!p.is_capture(m) && p.get_checkers() == 0 && !score_is_terminal(score))
            {
                posCnt++;
                myStringbuilder.Append(p.get_fen());
                myStringbuilder.Append(" | ");
                myStringbuilder.Append(score);
                myStringbuilder.Append(" | ");
                myStringbuilder.Append(res);
                myStringbuilder.Append('\n');
            }

            p.make_move(m, &ss);
            RepetitionTable.Pop();
        }

        file.Write(myStringbuilder.ToString());
        return posCnt;
    }


    public static unsafe int write_game_as_bulletformat(BinaryWriter file, pos p, List<move> moves, List<int> scores, Gameresult result)
    {
        SS ss = new SS();
        int posCnt = 0;

        for (int ply=0; ply<moves.Count; ply++)
        {
            move m = moves[ply];
            int score = scores[ply];

            p.make_move(m, &ss);
            RepetitionTable.Pop();

            if (!p.is_capture(m) && p.get_checkers() == 0 && !score_is_terminal(score))
            {
                posCnt++;
                var myBullet = new bullet(ref p, score, result);
                myBullet.write_to_file(file);
            }
        }
        return posCnt;
    }

}
