using System.ComponentModel;
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


    // Bit Manipulation

    public static int popCount (ulong bb) => BitOperations.PopCount(bb);

    public static int lsb (ulong bb) => BitOperations.TrailingZeroCount(bb);

    public static int popLsb (ref ulong bb) {
        int lsb = BitOperations.TrailingZeroCount(bb);
        bb &= bb-1;
        return lsb;
    }

    public static bool more_than_one (ulong bb) => (bb & (bb-1)) != 0;


    // String manipulation

    public static int StringToSquare (string str) {
        if (str.Length != 2) 
            throw new Exception($"String has wrong Length, expected 2 but got {str.Length}");
        return CharsToSquare(str[0], str[1]);
    }
    public static int CharsToSquare (char letter, char number) {
        if ((byte)letter > (byte)'h' || (byte)letter < (byte)'a') 
            throw new Exception($"letter {letter} cant be converted into file");
        if (number > '8' || number < '1') 
            throw new Exception($"number {number} cant be converted into rank");
        return ((byte)letter - 'a') + 8 * ((byte)number - (byte)'1');
    }
    public static string SquareToString (int sq) {
        if (sq < 0 || sq >= 64) throw new Exception($"invalid square {sq}");
        return $"{(char)(byte)(sq % 8 + 'a')}{(char)(byte)(sq / 8 + '1')}";
    }

    // Printer Methods

    public static void print (pos p) {
        string[,] pieces = {{"p", "n", "b", "r", "q", "k", "-"}, {"P","N", "B", "R", "Q", "K", "-"}, {"-", "-", "-", "-", "-", "-", "-"}};
        Console.WriteLine("  | a b c d e f g h |\n--+-----------------+");
        for (int r=7; r>=0; r--) {
            string str = $"{r+1} | ";
            for (int f=0; f<8; f++)
            {
                int sq = r * 8 + f;
                int color = ((1ul << sq) & p.colorBB[WHITE]) != 0 ? WHITE : BLACK;
                str += pieces[color, p.piece_on(sq)] + " ";
            }
            str += "|";
            Console.WriteLine(str);
        }
        Console.WriteLine("--+-----------------+");
    }

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