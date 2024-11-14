
public struct SS
{
    public int StaticEval;

    public byte MovedPiece;
    public byte CapturedPiece;
    public move Move;

    public ulong[] AttackTable;
}

public static class SearchStack
{
    public static SS[] stack;
    
    public static void init(int size)
    {
        stack = new SS[size];
    }

    public static void Reset()
    {
        Array.Clear(stack);
    }

}
