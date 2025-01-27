
using System.Drawing;

public struct TTEntry
{
    public ulong key  = 0ul;

    public byte depth = 0;
    public byte flag  = 0;
    
    public short score = 0;
    public short eval  = 0;

    public move move = move.NullMove;

    public TTEntry() {}
}

public static class TranspositionTable
{

    private static TTEntry[] TT;
    private static ulong TTSize;

    private const int MAX_SIZE = 4 * 1024; // max 4 GB
    private const int MIN_SIZE = 1;        // min 1 MB

    /// <summary>
    /// Creates a new TranspositionTable for the given size.
    /// Calls the Garbage Collector to clean up any old Transposition Tables.
    /// </summary>
    public static unsafe int Resize(int sizeMB)
    {
        sizeMB = Math.Clamp(sizeMB, MIN_SIZE, MAX_SIZE);
        ulong entrySizeByte = (ulong)sizeof(TTEntry);
        ulong TTSizeByte = (ulong)sizeMB * 1024 * 1024;
        
        TTSize = TTSizeByte / entrySizeByte;
        TT = new TTEntry[TTSize];
        Reset();

        //Console.WriteLine(sizeof(TTEntry));

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
    public static TTEntry get_entry(ulong key) => TT[key % (ulong)TTSize];

    /// <summary>
    /// compares the stored bits of the entry with the provided key
    /// compares the biggest 8 bits of the stored and to be stored position's keys
    /// </summary>
    public static bool is_tt_hit(ulong ZobristKey, ref TTEntry entry) => ZobristKey == entry.key;

    /// <summary>
    /// enters a new entry in the Transposition Table
    /// always overwrites the old entry
    /// </summary>
    public static void Push(ulong key, int score, int depth, int flag, move move)
    {
        ref var entry = ref TT[key % TTSize];

        entry.key   = key;
        entry.score = (short) score;
        entry.move  = move;
        entry.depth = (byte) depth;
        entry.flag  = (byte) flag;
    }
}
