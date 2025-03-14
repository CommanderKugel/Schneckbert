using static Constants;

public unsafe class MovePicker
{
    private enum Stage {
        ttMove,
        generateMoves,
        pickMoves,
        done,
    }

    public byte mvCnt;
    public byte mvIdx;
    public bool onlyCaptures;

    private Stage stage;

    /// <summary>
    /// Generates and Scores all pseudolegal moves int the given position.
    /// Via the "next()" method, the next best scored move will be picked.
    /// </summary>
    public unsafe MovePicker(pos p, bool inQS, move ttMove, SS* ss, ref Span<move> moves, ref Span<int> scores)
    {
        stage = Stage.ttMove;
        onlyCaptures = inQS;

        mvCnt = 0;
        mvIdx = 0;
    }

    /// <summary>
    /// Fills the "scores" parameter according to the current move-ordering scheme.
    /// </summary>
    private unsafe void ScoreMoves(pos p, move ttMove, SS* ss, int ply, ref Span<move> moves, ref Span<int> scores)
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

            if (victim != PIECE_NONE)
            {
                scores[i] = (SEE.see_threshold(m, ref p, 0) ? 1_000_000 : -2_000_000) 
                          + victim * 100_000 - attacker
                          + History.get_capthist_val(p.us, victim, attacker, m);
            }
            else if (m == ss->killerMove)
            {
                scores[i] = 900_000;
            } 
            else
            {
                scores[i] = History.get_butterfly_histval(p.us, m) 
                          + History.get_pawnhist_val(p.us, p.PawnKey, attacker, m.to);

                if (ply > 0)
                {
                    scores[i] += History.get_conthist_val(ss-1, p.us, attacker, m);
                }
            }
        }
    }


    public bool stage_after_killermove(ref Span<int> scores)
        => stage == Stage.pickMoves && scores[mvIdx-1] < 900_000;

    /// <summary>
    /// Returns the next best scored move using partial insertion sort.
    /// </summary>
    public move next(ref pos p, SS* ss, int ply, ref Span<move> moves, ref Span<int> scores, move ttMove)
    {
        if (stage > Stage.generateMoves && mvIdx >= mvCnt)
            return move.NullMove;

        while (true)
        {
            switch (stage)
            {
                case Stage.ttMove:
                {
                    stage++;
                    if (p.is_pseudo_legal(ttMove))
                    {
                        return ttMove;
                    }

                    goto case Stage.generateMoves;
                }
                case Stage.generateMoves:
                {
                    stage++;

                    mvCnt = (byte)MoveGen.GenerateMoves(ref moves, ref p, onlyCaptures, ss->checkers);
                    ScoreMoves(p, ttMove, ss, ply, ref moves, ref scores);
                    goto case Stage.pickMoves;
                }
                case Stage.pickMoves:
                {
                    if (mvIdx >= mvCnt)
                    {
                        stage = Stage.done;
                        return move.NullMove;
                    }

                    int idx = get_best_idx(ref scores);
                    move m = moves[idx];
                    swap(idx, ref moves, ref scores);
                    mvIdx++;

                    if (m == ttMove)
                    {
                        continue;
                    }

                    return m;
                }
                default:
                case Stage.done:
                {
                    return move.NullMove;
                }
            }
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
