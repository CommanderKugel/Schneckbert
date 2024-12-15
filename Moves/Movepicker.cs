using static Constants;

public unsafe class MovePicker
{
    public byte mvCnt;
    private byte mvIdx;


    public unsafe MovePicker(pos p, bool inQS, move ttMove, SS* ss, ref Span<move> moves, ref Span<int> scores)
    {
        mvCnt = (byte)MoveGen.GenerateMoves(ref moves, ref p, inQS, ss->checkers);
        mvIdx = 0;

        ScoreMoves(p, ttMove, ss, ref moves, ref scores);
    }

    private unsafe void ScoreMoves(pos p, move ttMove, SS* ss, ref Span<move> moves, ref Span<int> scores)
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
            // #2 Captures: passed SEE + Mvv-Lva
            // #3 Quiet: Killer move
            // #4 Quiets: + Butterfly & PieceTo history
            // #5 Captures: !passed SEE + Mvv-Lva 
            scores[i] = m == ttMove          ? 2_000_000
                      : victim != PIECE_NONE ? 
                            (SEE.see_threshold(m, ref p, 0) ? 1_000_000 : -1_000_000) 
                            + victim * 100_000 - attacker
                      : m == ss->killerMove  ? 900_000
                      : History.getButterflyHistVal(p.us, m) 
                            + History.getPieceToHistVal(p.us, attacker, m.to);
        }
    }

    public bool try_see(ref Span<int> scores) => scores[mvIdx] < 1_000_000;

    public move next(ref Span<move> moves, ref Span<int> scores)
    {
        move m = partialInsertionSort(ref moves, ref scores);
        return m;
    }

    private move partialInsertionSort(ref Span<move> moves, ref Span<int> scores)
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
