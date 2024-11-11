
public struct TTEntry
{
    public byte key, depth, flag;
    public int score;
    public move move;
    
    public TTEntry(ulong key, int score, int depth, int flag, move move)
    {
        this.key   = (byte)(key >> 48);
        this.score =        score;
        this.depth = (byte) depth;
        this.flag  = (byte) flag;
        this.move  =        move;
    }
    public TTEntry(byte key, int score, int depth, int flag, move move)
    {
        this.key   =       key;
        this.score =       score;
        this.depth = (byte)depth;
        this.flag  = (byte)flag;
        this.move  =       move;
    }
}

public static class TranspositionTable
{

    private static TTEntry[] TT;
    private static ulong TTSize;

    public static void init(int size)
    {
        TTSize = (ulong) size;
        TT = new TTEntry[TTSize];
        Reset();
    }

    public static void Reset()
    {
        Array.Fill(TT, new TTEntry(0, 0, 0, 0, move.NullMove));
    }

    public static ref TTEntry Probe(ulong key)
    {
        return ref TT[key % TTSize];
    }

    public static bool isTTHit(ulong ZobristKey, ref TTEntry entry)
    {
        return entry.key == (byte)(ZobristKey >> 56);
    }

    // just always overwrite
    public static void Push(ref TTEntry entry, ulong key, int score, int depth, int flag, move move)
    {
        entry.key   = (byte)(key >> 56);
        entry.score =        score;
        entry.depth = (byte) depth;
        entry.flag  = (byte) flag;
        entry.move  =        move;
    }

}