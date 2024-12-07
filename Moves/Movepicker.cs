using static Constants;

public class MovePicker
{
    public byte mvCnt;
    private byte mvIdx;
    private move[] moves;
    private int[]  scores;


    public unsafe MovePicker(pos p, bool inQS, move ttMove, SS* ss)
    {
        moves  = new move[MAX_MOVE_CNT];
        scores = new int [MAX_MOVE_CNT];

        mvCnt = (byte)MoveGen.GenerateMoves(moves, ref p, inQS, ss->checkers);
        mvIdx = 0;

        ScoreMoves(p, ttMove, ss);
    }

    private unsafe void ScoreMoves(pos p, move ttMove, SS* ss)
    {
        for (int i=0; i<mvCnt; i++) 
        {
            ref move m = ref moves[i];

            if (m.IsNull)
            {
                continue;
            }

            int attacker = p.piece_on(m.from);
            int victim   = p.piece_on(m.to);

            // #1 TT Move
            // #2 Captures Mvv + Capture History
            // #3 Quiets Butterfly History
            scores[i] = m == ttMove          ? 2_000_000
                      : victim != PIECE_NONE ? 1_000_000 + victim * 100_000 - attacker
                      : m == ss->killerMove  ? 900_000
                                             : History.getButterflyHistVal(p.us, m)
                                             + History.getPieceToHistVal(p.us, attacker, m.to);
        }
    }

    public move next()
    {
        move m = partialInsertionSort();
        return m;
    }

    private move partialInsertionSort()
    {
        // might have to return a null move
        // this is just more readable code, it should still return a null 
        // move due to move[mvIdx] containing one once mvIdy > mvCnt
        if (mvIdx > mvCnt) 
        {
            return move.NullMove;
        }

        int bestIndex = mvIdx;
        int bestScore = scores[mvIdx];

        // loop over all moves and find the maximum score left
        for (int i=mvIdx+1; i<mvCnt; i++)
        {
            if (scores[i] > bestScore)
            {
                bestIndex = i;
                bestScore = scores[i];
            }
        }

        // swap the best score and move to the front
        // ctrl + c
        int copyScore = scores[mvIdx];
        move copyMove = moves[mvIdx];
        // overwrite
        scores[mvIdx] = scores[bestIndex];
        moves[mvIdx]  = moves[bestIndex];
        // ctrl + v
        scores[bestIndex] = copyScore;
        moves[bestIndex]  = copyMove;

        // dont forget to increment mvIdx in the end
        return moves[mvIdx++];
    }
}
