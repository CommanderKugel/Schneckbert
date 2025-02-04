using static Constants;

public unsafe class MovePicker
{
    public byte mvCnt;
    private byte mvIdx;

    /// <summary>
    /// Generates and Scores all pseudolegal moves int the given position.
    /// Via the "next()" method, the next best scored move will be picked.
    /// </summary>
    public unsafe MovePicker(pos p, bool inQS, move ttMove, SS* ss, ref Span<move> moves, ref Span<int> scores)
    {
        mvCnt = (byte)MoveGen.GenerateMoves(ref moves, ref p, inQS, ss->checkers);
        mvIdx = 0;

        ScoreMoves(p, ttMove, ss, ref moves, ref scores);
    }

    /// <summary>
    /// Fills the "scores" parameter according to the current move-ordering scheme.
    /// </summary>
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
            int victim   = p.get_captured_pt(m);

            // #1 TT Move
            // #2 Captures: passed SEE + Mvv-Lva
            // #3 Quiet: Killer move
            // #4 Quiets: + Butterfly & PieceTo history
            // #5 Captures: !passed SEE + Mvv-Lva 
            scores[i] = m == ttMove          ? 2_000_000
                      : victim != PIECE_NONE ? 
                            (SEE.see_threshold(m, ref p, 0) ? 1_000_000 : -2_000_000) 
                            + History.getCaptHistVal(p.us, victim, attacker, m)
                            + victim * 100_000 - attacker
                      : m == ss->killerMove  ? 900_000
                      : History.getButterflyHistVal(p.us, m) 
                            + History.getPieceToHistVal(p.us, attacker, m.to)
                            + History.getPawnHistVal(p.us, p.PawnKey, attacker, m.to);
        }
    }

    /// <summary>
    /// Returns the score of the last move that was picked.
    /// Does not equal the Hostory score for quiet moves, 
    /// because TT & Killer moves are scored separately.
    /// </summary>
    public int curr_score(ref Span<int> scores) => scores[mvIdx-1];

    /// <summary>
    /// Returns the next best scored move using partial insertion sort.
    /// </summary>
    public move next(ref pos p, ref Span<move> moves, ref Span<int> scores)
    {
        if (mvIdx >= mvCnt)
            return move.NullMove;

        while (true)
        {
            int idx = get_best_idx(ref scores);

            /*
            if ( false &&
                 scores[idx] != 2_000_000 &&
                 scores[idx] >  1_000_000 &&
                !SEE.see_threshold(moves[idx], ref p, 0))
            {
                scores[idx] -= 2_000_000;
                continue;
            }
            */

            swap(idx, ref moves, ref scores);
            return moves[mvIdx++];
        }
    }

    /// <summary>
    /// Finds the index of the highest score remaining.
    /// </summary>
    private int get_best_idx(ref Span<int> scores)
    {
        int best = mvIdx;

        for (int i=mvIdx+1; i<mvCnt; i++)
            if (scores[best] < scores[i])
                best = i;
        
        return best;
    }

    /// <summary>
    /// Swaps the move and the score at the given index with the current mvIdx.
    /// When used in combination with get_best_idx(), insertion sort will be performed.
    /// </summary>
    private void swap(int x, ref Span<move> moves, ref Span<int> scores)
    {
        (moves[x],  moves[mvIdx])  = (moves[mvIdx],  moves[x]);
        (scores[x], scores[mvIdx]) = (scores[mvIdx], scores[x]);
    }
}
