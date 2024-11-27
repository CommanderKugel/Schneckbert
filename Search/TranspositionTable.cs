
public struct TTEntry
{
    public byte key, depth, flag;
    public int score;
    public move move;

    public const int TT_SHIFT = 56;
    
    public TTEntry(ulong key, int score, int depth, int flag, move move)
    {
        this.key   = (byte)(key >> TT_SHIFT);
        this.score =        score;
        this.depth = (byte) depth;
        this.flag  = (byte) flag;
        this.move  =        move;
    }
    
    public TTEntry(byte key, int score, int depth, int flag, move move)
    {
        this.key   =        key;
        this.score =        score;
        this.depth = (byte) depth;
        this.flag  = (byte) flag;
        this.move  =        move;
    }
}

public static class TranspositionTable
{

    private static TTEntry[] TT;
    private static int TTSize;

    /// <summary>
    /// initializes the Transposition Table as a big array
    /// the size sets the amonunt of entries
    /// one TTEntry is 70 bytes in size
    /// </summary>
    public static void init(int size)
    {
        TTSize = size;
        TT = new TTEntry[TTSize];
        Reset();
    }

    /// <summary>
    /// Cleats all Entries from the Transposition Table
    /// </summary>
    public static void Reset() => Array.Fill(TT, new TTEntry(0, 0, 0, 0, move.NullMove));

    /// <summary>
    /// returns the entry of the Transposition Table for the given key
    /// the key is provided by the position
    /// </summary>
    public static ref TTEntry Probe(ulong key) => ref TT[key % (ulong)TTSize];

    /// <summary>
    /// compares the stored bits of the entry with the provided key
    /// compares the biggest 8 bits of the stored and to be stored position's keys
    /// </summary>
    public static bool isTTHit(ulong ZobristKey, ref TTEntry entry) => entry.key == (byte)(ZobristKey >> TTEntry.TT_SHIFT);

    /// <summary>
    /// enters a new entry in the Transposition Table
    /// always overwrites the old entry
    /// </summary>
    public static void Push(ref TTEntry entry, ulong key, int score, int depth, int flag, move move)
    {
        entry.key   = (byte)(key >> TTEntry.TT_SHIFT);
        entry.score =        score;
        entry.depth = (byte) depth;
        entry.flag  = (byte) flag;
        entry.move  =        move;
    }
}