using System.Runtime.CompilerServices;

public static class RepetitionTable
{
    /// <summary>
    /// keeps track of the most recent entry in the Table.
    /// </summary>
    private static int ply = 0;

    /// <summary>
    /// Contains the Zobrist Keys of the Positions that were visited so far.
    /// Requires Pushing new and Popping old keys.
    /// </summary>
    private static ulong[] repTable = new ulong[Constants.MAX_GAME_PLY];

    /// <summary>
    /// Completely clears the repetition table.
    /// </summary>
    public static void Reset()
    {
        Array.Clear(repTable);
        ply = 0;
    }

    /// <summary>
    /// Inserts a new Zobrist-Key into the Repetitiontable.
    /// The position can now cause a two-fold-repetition-detection.
    /// </summary>
    public static void Push(ulong key) => repTable[ply++] = key;

    /// <summary>
    /// Removes the last entry from the Repetitiontable.
    /// The last position can no longer cause a two-fold-repetition
    /// </summary>
    public static void Pop() => repTable[ply--] = 0;

    /// <summary>
    /// Probes the Repetition Table for a 2-fold repetition.
    /// Returns true, if the Zobrist-Key of the position is already contained in the Table.
    /// </summary>
    public static bool IsRepeatedPosition(pos p)
    {
        // Just one full-move can not cause a repetition, so we take a quick exit. 
        if (p.FiftyMoveCnt < 4) 
        {
            return false;
        }

        // If the fifts-move-rule-counter is updated, a position can no longer repeat itself,
        // as Pawn-moves or Captures can never be undone.
        // So we dont need to check further than the fifty-move-counter.
        int earliestPossibleRepetition = Math.Max(ply-p.FiftyMoveCnt, 0);

        // Also, a Repetition can only occur after every full move,
        // so we only need to check every two half-moves -> i-=2
        for (int i = ply-2; i >= earliestPossibleRepetition; i -= 2) 
        { 
            if (repTable[i] == p.ZobristKey)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the game already goes on for many moves.
    /// Returns true, if the Repetition Table can be set back by 100 plies to prevent Indes-out-of-bounds errors.
    /// </summary>
    public static bool needs_set_back() => ply > 200;

    /// <summary>
    /// Moves the 100 top most entries 100 plies to the front.
    /// Makes use of the 50-(full-)move rule.
    /// Games are drawn if neither a piece was captured, nor a Pawn was moved in the last 100 plies of a game.
    /// As positions cant repeat once a Piece was taken or a Pawn was moved, we dont need to check earlier positions.
    /// </summary>
    public static unsafe void set_back_100()
    {
        fixed (ulong* ptr = repTable)
        {
            // take the 100 top most entries
            // and put them in the first most 100 slots
            Unsafe.CopyBlock(ptr, ptr+100, sizeof(ulong) * 100);
            ply -= 100;
        }
    }
}
