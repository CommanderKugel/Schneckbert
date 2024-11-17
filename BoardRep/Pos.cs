using static Constants;
using static Utils;
using static Attacks;

public struct pos
{   
    // Board Representation
    public ulong[] 
        pieceBB = {
            0x00FF_0000_0000_FF00ul, // Pawns
            0x4200_0000_0000_0042ul, // knights
            0x2400_0000_0000_0024ul, // Bishops
            0x8100_0000_0000_0081ul, // Rooks
            0x0800_0000_0000_0008ul, // Queens
            0x1000_0000_0000_0010ul, // Kings
        }, 
        colorBB = { 
            0xFFFF, 0xFFFF_0000_0000_0000ul 
        };
    
    public bool[] castling_rights = [true, true, true, true];   // kqKQ

    public int   ep = EPSQ_NONE;
    public byte  us = WHITE;

    public byte FiftyMoveCnt = 0;
    public ulong ZobristKey = 0;


    /// <summary>
    /// copies all values from the parent position
    /// essential part of "copy/make"
    /// </summary>
    /// <param name="p"></param>
    public pos(pos p) 
    {
        Array.Copy(p.pieceBB, this.pieceBB, 6);
        Array.Copy(p.colorBB, this.colorBB, 2);
        Array.Copy(p.castling_rights, this.castling_rights, 4);
        this.ep  = p.ep;
        this.us = p.us;
        this.FiftyMoveCnt = p.FiftyMoveCnt;
        this.ZobristKey = p.ZobristKey;
    }

    /// <summary>
    /// parses a fen an returns the expected position
    /// </summary>
    public pos (string fen) 
    {
        colorBB = [0, 0];
        pieceBB = [0, 0, 0, 0, 0, 0];
        
        int r = 7;
        int f = 0;
        int idx = 0;
        char c = 'x';

        for (; c!=' '; idx++) 
        {
            c = fen[idx];
            int sq = 8 * r + f;
            f++;

            switch (c) {
                case 'P': set_piece(sq, PAWN,   WHITE); break;
                case 'N': set_piece(sq, KNIGHT, WHITE); break;
                case 'B': set_piece(sq, BISHOP, WHITE); break;
                case 'R': set_piece(sq, ROOK,   WHITE); break;
                case 'Q': set_piece(sq, QUEEN,  WHITE); break;
                case 'K': set_piece(sq, KING,   WHITE); break;
                case 'p': set_piece(sq, PAWN,   BLACK); break;
                case 'n': set_piece(sq, KNIGHT, BLACK); break;
                case 'b': set_piece(sq, BISHOP, BLACK); break;
                case 'r': set_piece(sq, ROOK,   BLACK); break;
                case 'q': set_piece(sq, QUEEN,  BLACK); break;
                case 'k': set_piece(sq, KING,   BLACK); break;
                case '/': f=0; r--; break;
                default: try{f += int.Parse(c.ToString())-1;} catch{} break;
            }
        }

        us = (byte)((fen[idx++] == 'w') ? 1 : 0);

        castling_rights = [false, false, false, false];
        for (char cr = fen[++idx]; cr != ' ' && cr != '-'; cr = fen[++idx]) 
        {
            switch (cr) 
            {
                case 'K': castling_rights[WHITE+WHITE  ] = true; break;
                case 'Q': castling_rights[WHITE+WHITE+1] = true; break;
                case 'k': castling_rights[BLACK+BLACK  ] = true; break;
                case 'q': castling_rights[BLACK+BLACK+1] = true; break;
                default: break;
            }
        }
        
        if (fen[++idx] != '-' && fen[idx] != ' ') 
        {
            ep = CharsToSquare(fen[idx], fen[++idx]);
        }

        ZobristKey = Zobrist.CalcZobrist(this);
    }

    private void set_piece(int sq, int piece, int c) 
    {
        pieceBB[piece] |= 1ul << sq;
        colorBB[c]     |= 1ul << sq;
    }

    // QOL Methods

    public ulong get_pieces(int pt) => pieceBB[pt];
    public ulong get_pieces(int pt, int c) => pieceBB[pt] & colorBB[c];
    public ulong get_pieces(int pt1, int pt2, int c) => (pieceBB[pt1] | pieceBB[pt2]) & colorBB[c];

    public int get_ksq(int c) => lsb(pieceBB[KING] & colorBB[c]);

    /// <summary>
    /// returns the PieceType of the Piece on the given Square-index
    /// </summary>
    public byte piece_on(int sq) 
    {
        for (byte i=PAWN; i<=KING; i++)
            if ((pieceBB[i] & (1ul << sq)) != 0)
                return i;
        return PIECE_TYPE_NONE;
    }
    public int color_on(int sq) 
    {
        if (((1ul << sq) & colorBB[WHITE]) != 0)
            return WHITE;
        if (((1ul << sq) & colorBB[BLACK]) != 0)
            return BLACK;
        return COLOR_NONE;
    }

    /// <summary>
    /// returns a bitboard containing all occupants of both WHITE and BLACK pieces
    /// </summary>
    public ulong get_blocker() => colorBB[WHITE] | colorBB[BLACK];

    /// <summary>
    /// returns a bitboard containing all pieces that attack a given square
    /// the pieces can be of WHITE or BLACK color
    /// </summary>
    public ulong attackers_to (int sq, ulong block) 
    {
        return PawnAttacks[WHITE][sq] & pieceBB[PAWN] & colorBB[BLACK] | 
               PawnAttacks[BLACK][sq] & pieceBB[PAWN] & colorBB[WHITE] | 
               KnightAttacks(sq) & pieceBB[KNIGHT] | 
               BishopAttacks(sq, block) & (pieceBB[BISHOP] | pieceBB[QUEEN]) | 
               RookAttacks  (sq, block) & (pieceBB[ROOK]   | pieceBB[QUEEN]) | 
               KingAttacks(sq) & pieceBB[KING];
    }

    /// <summary>
    /// returns a bitboard of all pieces that attack the side-to-move's king
    /// the pieces are of the opposing side's color
    /// </summary>
    public ulong get_checkers() => attackers_to(get_ksq(us), get_blocker()) & colorBB[1-us];


    public bool make_move(move m, int ply=0) 
    {
        int from, to, dist, movingPieceType, capturedPieceType;
        ulong fromBB, toBB, FromTo;
        bool wtm;

        from   = m.from;
        to     = m.to;
        fromBB = 1ul << from;
        toBB   = 1ul << to;
        FromTo = fromBB | toBB;
        dist   = to-from;
        wtm    = us == WHITE;

        movingPieceType   = piece_on(from);
        capturedPieceType = piece_on(to);

        // make quiet move
        pieceBB[movingPieceType] ^= FromTo;
        colorBB[us] ^= FromTo;
        FiftyMoveCnt++;

        // make capture
        if (capturedPieceType != PIECE_TYPE_NONE) 
        {
            pieceBB[capturedPieceType] ^= toBB;
            colorBB[1-us] ^= toBB;

            // captures reset the fifty move rule
            FiftyMoveCnt = 0;
        }

        // reset ep square, because was copyied from prev pos
        ep = EPSQ_NONE;

        if (movingPieceType == PAWN) 
        {
            // double pawn push
            if (Math.Abs(dist) == 16) 
            {
                ep = to;
            }

            // promotion
            else if (m.IsPromo) 
            {
                pieceBB[PAWN] ^= toBB;
                pieceBB[m.PromoPiece] ^= toBB;
            }

            // en passant capture
            else if (m.IsEp) 
            {
                capturedPieceType = PAWN;
                pieceBB[PAWN] ^= wtm ? south(toBB) : north(toBB);
                colorBB[1-us] ^= wtm ? south(toBB) : north(toBB);
            }
            
            // reset at every Pawn move
            FiftyMoveCnt = 0;
        }

        if (movingPieceType == KING) 
        {
            // Kingside Castling
            if (dist == 2) 
            {
                pieceBB[ROOK] ^= wtm ? 0x0000_0000_0000_00a0ul : 0xa000_0000_0000_0000ul;
                colorBB[us]   ^= wtm ? 0x0000_0000_0000_00a0ul : 0xa000_0000_0000_0000ul;
            }

            // Queenside Castling
            if (dist == -2) 
            {
                pieceBB[ROOK] ^= wtm ? 0x0000_0000_0000_0009ul : 0x0900_0000_0000_0000ul;
                colorBB[us]   ^= wtm ? 0x0000_0000_0000_0009ul : 0x0900_0000_0000_0000ul;
            }
        }

        us = (byte)(1-us);

        bool IsLegal = (attackers_to(get_ksq(1-us), get_blocker()) & colorBB[us]) == 0;

        if (IsLegal) 
        {
            // update castling rights
            // as soon as any piece on the relevant squares moves or gets captured,
            // the right will be removed
            castling_rights[0] &= (FromTo & 0x9000_0000_0000_0000ul) == 0;
            castling_rights[1] &= (FromTo & 0x1100_0000_0000_0000ul) == 0;
            castling_rights[2] &= (FromTo & 0x0000_0000_0000_0090ul) == 0;
            castling_rights[3] &= (FromTo & 0x0000_0000_0000_0011ul) == 0;

            // update Search Stack
            SearchStack.Push(m, this, movingPieceType, capturedPieceType, ply);

            // recalculate Zobrist Keys, incremental updates coming soon
            ZobristKey = Zobrist.CalcZobrist(this);
            RepetitionTable.Push(ZobristKey);
        }

        return IsLegal;
    }


    public bool IsPseudoLegal(move m) 
    {
        if (color_on(m.from) != us)
            return false;
        return (PieceAttacks(this, piece_on(m.from), m.from) & (1ul << m.to)) != 0;
    }

    public static bool operator ==(pos p1, pos p2) => p1.ZobristKey == p2.ZobristKey;
    public static bool operator !=(pos p1, pos p2) => p1.ZobristKey != p2.ZobristKey;

    public bool IsInsufficientMaterial => (
        (pieceBB[PAWN] | pieceBB[ROOK] | pieceBB[QUEEN]) == 0 &&
        !more_than_one(get_pieces(BISHOP, KNIGHT, WHITE)) &&
        !more_than_one(get_pieces(BISHOP, KNIGHT, BLACK))
    );

    public readonly bool IsFiftyMoveDraw => FiftyMoveCnt == 100;
}