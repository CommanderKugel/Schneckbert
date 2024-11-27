using static Constants;

public class MovePicker
{
    private byte mvCnt;
    private byte mvIdx;
    private move[] moves;
    private int[]  scores;


    public MovePicker(pos p, bool inQS, move ttMove) 
    {
        moves  = new move[MAX_MOVE_CNT];
        scores = new int [MAX_MOVE_CNT];
        
        mvCnt = (byte)MoveGen.GenerateMoves(moves, p, inQS);
        mvCnt = Math.Min(mvCnt, (byte)(MAX_MOVE_CNT-1));
        mvIdx = 0;

        ScoreMoves(p, ttMove);
    }

    private void ScoreMoves(pos p, move ttMove) 
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
            // #2 Captures Mvv-Lva
            // #3 Quiets Butterfly History
            scores[i] = m      == ttMove     ? 2_000_000
                      : victim != PIECE_NONE ? 1_000_000 + victim * 100_000
                                             + History.getCaptureHistVal(p.us, attacker, victim, m.to)
                      : History.getButterflyHistVal(p.us, m);
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
        if (mvIdx > mvCnt) {
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
    
    public void updateQuietHistories(int depth, pos p)
    {
        History.updateQuietHistValues(moves, mvIdx-1, depth, p);
    }

    public void updateCaptHistories(int depth, pos p)
    {
        History.updateCaptureHistValues(moves, mvIdx-1, depth, p);
    }

}
