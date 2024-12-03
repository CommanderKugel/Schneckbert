using static Constants;

public struct SS
{
    public int StaticEval;

    public byte MovedPiece;
    public byte CapturedPiece;
    public move Move;

    public ulong checkers;
}

public static class SearchStack
{
    public static SS[] stack;
    
    public static void init() 
    {
        stack = new SS[MAX_SEARCH_PLY];
        Array.Fill(stack, new SS());
    }

    public static void Reset()
    {
        Array.Fill(stack, new SS());
    }

    /// <summary>
    /// Updates the current plies' SearchStack entry
    /// also sets the CounterHistory for the next Ply
    /// </summary>
    public static unsafe void Push(SS* ss, move m, pos p, int movingPieceType, int capturedPieceType)
    {
        ss->Move = m;
        ss->MovedPiece    = (byte)movingPieceType;
        ss->CapturedPiece = (byte)capturedPieceType;
    }
}
