using static Constants;

public class MovePicker
{
    private byte mvCnt;
    private byte mvIdx;
    private byte us;
    private move[] moves;
    private int[]  scores;
    
    public MovePicker(pos p, bool inQS, move ttMove, ref SS ss) 
    {
        moves  = new move[MAX_MOVE_CNT];
        scores = new int [MAX_MOVE_CNT];
        
        mvCnt = (byte)MoveGen.GenerateMoves(moves, p, inQS, ref ss);
        mvIdx = 0;
        us = p.us;

        ScoreMoves(p, ttMove, ref ss);
    }

    private void ScoreMoves(pos p, move ttMove, ref SS ss) 
    {
        ulong pawnDanger  = ss.AttackTable[PAWN];
        ulong minorDanger = ss.AttackTable[KNIGHT] | ss.AttackTable[BISHOP] | pawnDanger;
        ulong majorDanger = ss.AttackTable[ROOK] | ss.AttackTable[QUEEN] | minorDanger;

        ulong endangeredPieces = p.get_pieces(KNIGHT, BISHOP, p.us) & pawnDanger
                               | p.get_pieces(ROOK, p.us) & minorDanger
                               | p.get_pieces(QUEEN, p.us) & majorDanger;

        for (int i=0; i<mvCnt; i++) 
        {
            ref move m = ref moves[i];

            if (m.IsNull) {
                continue;
            }

            int attacker = p.piece_on(m.from);
            int victim   = p.piece_on(m.to);
                                             // #1 ttMove
            scores[i] = m == ttMove          ? 2_000_000
                                             // #2 Mvv-Lva                           
                      : victim != PIECE_NONE ? 1_000_000 + victim * 100_000 - attacker 
                                             // #3 Quiet History
                      : 1 + History.getHistVal(us, m);                            

            /*
            // #4 Attack Maps, bonus for moving when in danger
            if ((endangeredPieces & (1ul << m.from)) != 0)
            {
                scores[i] += fromDangerBonus[attacker];
            }

            // #5 Attack Maps, malus when moving into danger
            ulong toBB = 1ul << m.to;
            scores[i] -= ((attacker == KNIGHT || attacker == BISHOP) && (pawnDanger & toBB) != 0) ? 14_000
                       : (attacker == ROOK && (minorDanger & toBB) != 0) ? 24_000 
                       : (attacker == QUEEN && (majorDanger & toBB) != 0) ? 49_000
                       : 0;
            */
        }
    }
    private static readonly int[] fromDangerBonus = {
        0, 15_000, 15_000, 25_000, 50_000, 0
    };


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

    public void updateHistories(int depth)
    {
        History.updateHistValues(moves, mvIdx-1, depth, us);
    }

}
