
public struct TTEntry
{
    public byte depth = 0;
    public byte flag  = 0;
    public ulong key  = 0ul;
    public int score  = 0;
    public move move  = move.NullMove;

    public TTEntry() {}
}

public static class TranspositionTable
{

    private static TTEntry[] TT;
    private static long TTSize;

    private const int MAX_SIZE = 4 * 1024; // max 4 GB
    private const int MIN_SIZE = 1;        // min 1 MB

    /// <summary>
    /// Creates a new TranspositionTable for the given size.
    /// Calls the Garbage Collector to clean up any old Transposition Tables.
    /// </summary>
    public static unsafe int Resize(int sizeMB)
    {
        sizeMB = Math.Clamp(sizeMB, MIN_SIZE, MAX_SIZE);
        int  entrySizeByte = sizeof(TTEntry);
        long TTSizeByte = sizeMB * 1024 * 1024;
        
        TTSize = TTSizeByte / entrySizeByte;
        TT = new TTEntry[TTSize];
        Reset();

        // clean the garbage rather sooner than later
        GC.Collect();

        return sizeMB;
    }

    /// <summary>
    /// Clears all Entries from the Transposition Table
    /// </summary>
    public static void Reset()
    {
        Array.Fill(TT, new TTEntry());
    }

    /// <summary>
    /// returns the entry of the Transposition Table for the given key
    /// the key is provided by the position
    /// </summary>
    public static ref TTEntry Probe(ulong key) => ref TT[key % (ulong)TTSize];

    /// <summary>
    /// compares the stored bits of the entry with the provided key
    /// compares the biggest 8 bits of the stored and to be stored position's keys
    /// </summary>
    public static bool isTTHit(ulong ZobristKey, ref TTEntry entry) => ZobristKey == entry.key;

    /// <summary>
    /// enters a new entry in the Transposition Table
    /// always overwrites the old entry
    /// </summary>
    public static void Push(ref TTEntry entry, ulong key, int score, int depth, int flag, move move)
    {
        entry.key   = key;
        entry.score = score;
        entry.move  = move;
        entry.depth = (byte) depth;
        entry.flag  = (byte) flag;
    }
}
