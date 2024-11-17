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
        Array.Clear(stack);
    }

    public static void Push(move m, pos p, int movingPieceType, int capturedPieceType, int ply)
    {
        ref SS stack_entry = ref stack[ply];

        stack_entry.Move = m;
        stack_entry.MovedPiece    = (byte)movingPieceType;
        stack_entry.CapturedPiece = (byte)capturedPieceType;

        stack_entry.checkers = p.get_checkers();
    }

}
