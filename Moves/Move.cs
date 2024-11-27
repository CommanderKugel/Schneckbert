using static Constants;
using static Utils;

public struct move 
{
    public ushort value = 0;

    public move(int from, int to, ushort flag=NoFlag) 
    {
        value = (ushort) (from | (to << 6) |flag);
    }

    public readonly int from   =>  value & 0b11_1111;
    public readonly int to     => (value >> 6) & 0b11_1111;
    public readonly int flag   =>  value & 0xF000;
    public readonly int FromTo =>  value & 0x0FFF;

    public readonly bool IsPromo    => (value & KnightPromo) != 0;
    public readonly int  PromoPiece => ((value >> 12) & 0b11) + 1;
    public readonly bool IsEp       => (value & EpCapture) != 0;

    public readonly bool IsNull => value == 0;
    public static move NullMove => new move() { value = 0 };

    public const ushort 
        NoFlag      = 0b0000_0000_0000_0000,
        KnightPromo = 0b0100_0000_0000_0000,
        BishopPromo = 0b0101_0000_0000_0000,
        RookPromo   = 0b0110_0000_0000_0000,
        QueenPromo  = 0b0111_0000_0000_0000,
        EpCapture   = 0b1000_0000_0000_0000;


    /// <summary>
    /// constructs a move from a given uci-string
    /// providing the position is necessary for identifying En Passant captures
    /// </summary>
    public move(string mvstr, pos p) 
    {
        int from = CharsToSquare(mvstr[0], mvstr[1]);
        int to   = CharsToSquare(mvstr[2], mvstr[3]);
        ushort flag = 0;

        // read promotions directly from the string
        if (mvstr.Length == 5) 
        {
            flag = mvstr[4] switch {
                'n' => KnightPromo,
                'b' => BishopPromo,
                'r' => RookPromo,
                'q' or _ => QueenPromo,
            };
        }

        // read en passant from the context
        // ep square is the square the victim-pawn moved onto
        else if (p.ep != EPSQ_NONE 
             &&  to == (p.ep + (p.us==WHITE ? 8 : -8))
             &&  p.piece_on(from) == PAWN)
        {
            flag = EpCapture;
        }

        this.value = (ushort)(from | (to << 6) | flag);
    }

    public move(int from, int to, int promoPt)
    {
        ushort flag_ = promoPt switch {
            KNIGHT => KnightPromo,
            BISHOP => BishopPromo,
            ROOK   => RookPromo,
            QUEEN  => QueenPromo,
            _      => NoFlag,
        };
        this.value = (ushort)(from | (to << 6) | flag_);
    }

    public static bool operator ==(move m1, move m2) => m1.value == m2.value;
    public static bool operator !=(move m1, move m2) => m1.value != m2.value;

    /// <summary>
    /// returns the moves uci-string representation
    /// </summary>
    /// <returns></returns>
    public override string ToString() 
    {
        return IsPromo ? BoardNotation[from] + BoardNotation[to] + PieceChars[0][PromoPiece]
                       : BoardNotation[from] + BoardNotation[to];
    }
}