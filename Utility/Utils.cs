using System.Diagnostics;
using System.Numerics;
using static Constants;

public static class Utils
{

    // Bitboard Shifts

    public static ulong north(ulong bb) => bb << 8 & 0xFFFF_FFFF_FFFF_FF00;
    public static ulong south(ulong bb) => bb >> 8 & 0x00FF_FFFF_FFFF_FFFF;
    public static ulong west (ulong bb) => bb >> 1 & 0x7f7f7f7f7f7f7f7f;
    public static ulong east (ulong bb) => bb << 1 & 0xfefefefefefefefe;

    public static ulong nw (ulong bb) => north(west(bb));
    public static ulong sw (ulong bb) => south(west(bb));
    public static ulong ne (ulong bb) => north(east(bb));
    public static ulong se (ulong bb) => south(east(bb));

    public static ulong up(ulong bb, int c) => c==WHITE ? north(bb) : south(bb);

    public static ulong Ray(ulong bb, ulong blocker, Func<ulong, ulong> dir) {
        bb  = dir(bb);
        bb |= dir(bb & ~blocker);
        bb |= dir(bb & ~blocker);
        bb |= dir(bb & ~blocker);
        bb |= dir(bb & ~blocker);
        bb |= dir(bb & ~blocker);
        bb |= dir(bb & ~blocker);
        return bb;
    }

    public static ulong[][] Rays;

    public static void init()
    {
        Rays = new ulong[64][];
        for (int x=0; x<64; x++)
        {
            Rays[x] = new ulong[64];
            for (int y=0; y<64; y++)
            {
                ulong xbb = 1ul << x;
                ulong ybb = 1ul << y;
                ulong block = xbb | ybb;

                if (file_of(x) == file_of(y) || rank_of(x) == rank_of(y))
                {
                    Rays[x][y] = 
                        Ray(xbb, block, north) & Ray(ybb, block, south) |
                        Ray(xbb, block, south) & Ray(ybb, block, north) |
                        Ray(xbb, block, west)  & Ray(ybb, block, east) |
                        Ray(xbb, block, east)  & Ray(ybb, block, west);
                }
                else if ((Attacks.BishopAttacks(x, block) & Attacks.BishopAttacks(y, block)) != 0)
                {
                    Rays[x][y] =
                        Ray(xbb, block, nw) & Ray(ybb, block, se) |
                        Ray(xbb, block, se) & Ray(ybb, block, nw) |
                        Ray(xbb, block, sw) & Ray(ybb, block, ne) |
                        Ray(xbb, block, ne) & Ray(ybb, block, sw);
                }

                if (Rays[x][y] != 0 || (Attacks.KingAttacks(x) & ybb) != 0)
                {
                    Rays[x][y] |= 1ul << y;
                }
            }
        }
    }


    // Bit Manipulation

    public static int popCount (ulong bb) => BitOperations.PopCount(bb);

    public static int lsb (ulong bb) => BitOperations.TrailingZeroCount(bb);

    public static int popLsb (ref ulong bb) {
        int lsb = BitOperations.TrailingZeroCount(bb);
        bb &= bb-1;
        return lsb;
    }

    public static bool more_than_one (ulong bb) => (bb & (bb-1)) != 0;

    public static int get_ep_victim(int epSq, int color)
        => color == WHITE ? epSq-8 : epSq+8;


    // String manipulation

    public static int StringToSquare (string str) 
    {
        Debug.Assert(str.Length == 2, $"String has wrong Length, expected 2 but got {str.Length}");
        return CharsToSquare(str[0], str[1]);
    }

    public static int CharsToSquare (char letter, char number) 
    {
        Debug.Assert(letter <= 'h' || letter >= 'a', $"letter {letter} can not be converted into file");
        Debug.Assert(number <= '8' || number >= '1', $"number {number} can not be converted into rank");
        return ((byte)letter - 'a') + 8 * ((byte)number - (byte)'1');
    }
    
    public static string SquareToString (int sq) 
    {
        Debug.Assert(sq >= 0 || sq < 64, $"invalid square {sq}");
        return $"{(char)(byte)(sq % 8 + 'a')}{(char)(byte)(sq / 8 + '1')}";
    }


    /// <summary>
    /// prints the board in the console
    /// </summary>
    public static void print (pos p) {
        string[,] pieces = {
            {"p", "n", "b", "r", "q", "k", "-"}, 
            {"P", "N", "B", "R", "Q", "K", "-"}, 
            {"-", "-", "-", "-", "-", "-", "-"}
        };
        Console.WriteLine("  | a b c d e f g h |\n--+-----------------+");
        for (int r=7; r>=0; r--) {
            string str = $"{r+1} | ";
            for (int f=0; f<8; f++)
            {
                int sq = r * 8 + f;
                str += pieces[p.color_on(sq), p.piece_on(sq)] + " ";
            }
            str += "|";
            Console.WriteLine(str);
        }
        Console.WriteLine("--+-----------------+");
        Console.WriteLine(p.get_fen());
    }

    /// <summary>
    /// prints the bitboard in the console
    /// </summary>
    public static void print (ulong bb) {
        Console.WriteLine("  | a b c d e f g h |\n--+-----------------+");
        for (int r=7; r>=0; r--) {
            string str = $"{r+1} | ";
            for (int f=0; f<8; f++)
                str += ((1ul << (r * 8 + f) & bb) != 0) ? "X " : "- ";
            str += "|";
            Console.WriteLine(str);
        } 
        Console.WriteLine("--+-----------------+");
    }
}