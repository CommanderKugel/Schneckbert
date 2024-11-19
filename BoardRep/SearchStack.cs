using static Constants;

public struct SS
{
    public int StaticEval;

    public byte MovedPiece;
    public byte CapturedPiece;
    public move Move;

    public ulong checkers;
    public ulong[] AttackTable;

    public SS()
    {
        AttackTable = new ulong[6];
    }
}

public static class SearchStack
{
    public static SS[] stack;
    
    public static void init() 
    {
        stack = new SS[Board.MAX_SEARCH_DEPTH];
        Array.Fill(stack, new SS());
    }

    public static void Reset()
    {
        Array.Fill(stack, new SS());
    }

    public static void Push(ref SS ss, move m, pos p, int movingPieceType, int capturedPieceType)
    {
        ss.Move = m;
        ss.MovedPiece    = (byte)movingPieceType;
        ss.CapturedPiece = (byte)capturedPieceType;

        ss.checkers = p.get_checkers();
    }

}
