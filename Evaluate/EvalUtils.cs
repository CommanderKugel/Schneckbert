using static Constants;
using static Utils;

public static class EvalUtils
{

    private static ulong get_rankBB(int sq) => Rank1 << rank_of(sq) * 8;
    /// <summary>
    /// contains the rank bitboards for the corresponding rank index
    /// </summary>
    public static readonly ulong[] rankBB = {
        get_rankBB( 0), get_rankBB( 8), get_rankBB(16), get_rankBB(24), 
        get_rankBB(32), get_rankBB(40), get_rankBB(48), get_rankBB(56),
    };

    private static ulong get_fileBB(int sq) => FileA << file_of(sq);
    /// <summary>
    /// contains the file bitboards for the corresponding file index
    /// </summary>
    public static readonly ulong[] fileBB = {
        get_fileBB(0), get_fileBB(1), get_fileBB(2), get_fileBB(3), 
        get_fileBB(4), get_fileBB(5), get_fileBB(6), get_fileBB(7),
    };

    public static ulong get_neighbourFileBB(int sq)
    {
        ulong f = get_fileBB(sq);
        return east(f) | west(f);
    }
    /// <summary>
    /// contains the file bitboards of the neighbouring files next to the square
    /// </summary>
    public static readonly ulong[] neighborFileBB = {
        get_neighbourFileBB(0), get_neighbourFileBB(1), get_neighbourFileBB(2), get_neighbourFileBB(3), 
        get_neighbourFileBB(4), get_neighbourFileBB(5), get_neighbourFileBB(6), get_neighbourFileBB(7), 
    };

    /// <summary>
    /// returns the file and its neighbours of a pawn
    /// only contains squares that lie in front of the square
    /// is flipped from blacks POV
    /// </summary>
    public static ulong get_passer_mask(int sq, int color)
    {
        ulong tripleFile = fileBB[file_of(sq)];
        tripleFile |= east(tripleFile) | west(tripleFile);
        return color==WHITE ? north(tripleFile) << rank_of(sq) : south(tripleFile) >> rank_of(sq);
    }

    /// <summary>
    /// returns the file bitboard but only the squares that lays in front of the piece
    /// for blacks POV, the bitboard only contains squares that lay behind the given square 
    /// </summary>
    public static ulong foreward_fileBB(int sq, int color) => color==WHITE ? FileA << sq : FileH >> 63-sq;
    


}
