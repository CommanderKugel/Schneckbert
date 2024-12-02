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

    public NNUE.Accumulator accumulator;


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
        this.ep = p.ep;
        this.us = p.us;
        this.FiftyMoveCnt = p.FiftyMoveCnt;
        this.ZobristKey = p.ZobristKey;
        this.accumulator = new NNUE.Accumulator(p.accumulator);
    }

    /// <summary>
    /// parses a fen an returns the expected position
    /// </summary>
    public pos (string fen) 
    {
        colorBB = [0, 0];
        pieceBB = [0, 0, 0, 0, 0, 0];
        this.accumulator = new NNUE.Accumulator(this);
        
        int r = 7;
        int f = 0;
        int idx = 0;
        char c = 'x';

        for ( ; c!=' '; idx++) 
        {
            c = fen[idx];
            int sq = 8 * r + f;
            f++;

            switch (c) 
            {
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

    /// <summary>
    /// returns the Color of the Piece on the given Square-index
    /// </summary>
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
        return (PawnAttacks[WHITE][sq] & get_pieces(PAWN, BLACK)) | 
               (PawnAttacks[BLACK][sq] & get_pieces(PAWN, WHITE)) | 
               (KnightAttacks(sq) & pieceBB[KNIGHT]) | 
               (BishopAttacks(sq, block) & (pieceBB[BISHOP] | pieceBB[QUEEN])) | 
               (RookAttacks  (sq, block) & (pieceBB[ROOK]   | pieceBB[QUEEN])) | 
               (KingAttacks(sq) & pieceBB[KING]);
    }

    /// <summary>
    /// returns a bitboard of all pieces that attack the side-to-move's king
    /// the pieces are of the opposing side's color
    /// </summary>
    public ulong get_checkers() => attackers_to(get_ksq(us), get_blocker()) & colorBB[1-us];


    private void set_piece(int sq, int pt, int color) 
    {
        pieceBB[pt]    ^= 1ul << sq;
        colorBB[color] ^= 1ul << sq;

        //ZobristKey ^= Zobrist.get_piece_key(color, pt, sq);
        accumulator.activate(color, pt, sq);
    }

    private void pop_piece(int sq, int pt, int color)
    {
        pieceBB[pt]    ^= 1ul << sq;
        colorBB[color] ^= 1ul << sq;

        //ZobristKey ^= Zobrist.get_piece_key(color, pt, sq);
        accumulator.deactivate(color, pt, sq);
    }

    private void move_piece(int color, int pt, int from, int to)
    {
        pieceBB[pt] ^= (1ul << from) | (1ul << to);
        colorBB[color] ^= (1ul << from) | (1ul << to);

        //ZobristKey ^= Zobrist.get_piece_key(color, pt, from)
        //           ^  Zobrist.get_piece_key(color, pt, to);
        accumulator.activate(color, pt, to);
        accumulator.deactivate(color, pt, from);
    }

    public bool is_legal(move m)
    {
        if (m.IsNull)
        {
            return false;
        }

        int from = m.from;
        int to   = m.to;
        int pt   = piece_on(from);

        int ksq = pt==KING ? to : get_ksq(us);
        ulong block = (get_blocker() ^ (1ul << from)) | (1ul << to);

        if (m.IsEp)
        {
            block ^= us==WHITE ? south(1ul << to) : north(1ul << to);
            return (attackers_to(ksq, block) & colorBB[1-us] & block) == 0;
        }
        if (pt == KING)
        {
            return (attackers_to(to, block) & colorBB[1-us]) == 0;
        }
        if (Rays[ksq][from] == 0)
        {
            return true;
        }
        return (attackers_to(ksq, block) & ~(1ul << to) & colorBB[1-us]) == 0;
    }

    public unsafe bool make_move(move m, SS* ss) 
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
        move_piece(us, movingPieceType, from, to);
        FiftyMoveCnt++;

        // make capture
        if (capturedPieceType != PIECE_TYPE_NONE) 
        {
            pop_piece(to, capturedPieceType, 1-us);

            // captures reset the fifty move rule
            FiftyMoveCnt = 0;
        }

        // reset ep square, because was copyied from prev pos
        if (ep != EPSQ_NONE) 
        {
            //ZobristKey ^= Zobrist.get_ep_key(ep);
            ep = EPSQ_NONE;
        }

        if (movingPieceType == PAWN) 
        {
            // double pawn push
            if (Math.Abs(dist) == 16) 
            {
                ep = to;
                //ZobristKey ^= Zobrist.get_ep_key(to);
            }

            // promotion
            else if (m.IsPromo) 
            {
                pop_piece(to, PAWN, us);
                set_piece(to, m.PromoPiece, us);
            }

            // en passant capture
            else if (m.IsEp) 
            {
                capturedPieceType = PAWN;
                pop_piece(wtm ? to-8 : to+8, PAWN, 1-us);
            }
            
            // reset at every Pawn move
            FiftyMoveCnt = 0;
        }

        if (movingPieceType == KING) 
        {
            // Kingside Castling
            if (dist == 2) 
            {
                int rookFrom = wtm ? H1 : H8;
                int rookTo   = wtm ? F1 : F8;
                move_piece(us, ROOK, rookFrom, rookTo);
            }

            // Queenside Castling
            if (dist == -2) 
            {
                int rookFrom = wtm ? A1 : A8;
                int rookTo   = wtm ? D1 : D8;
                move_piece(us, ROOK, rookFrom, rookTo);
            }
        }

        bool IsLegal = (attackers_to(get_ksq(us), get_blocker()) & colorBB[1-us]) == 0;
        us = (byte)(1-us);


        if (IsLegal) 
        {
        // update castling rights
        // as soon as any piece on the relevant squares moves or gets captured,
        // the right will be removed
        for (int cr=0; cr<4; cr++)
        {
            if (castling_rights[cr] && (FromTo & CastlingRightModifiers[cr]) != 0)
            {
                castling_rights[cr] = false;
                ZobristKey ^= Zobrist.get_castling_key(cr);
            }
        }

        // update Search Stack
        SearchStack.Push(ss, m, this, movingPieceType, capturedPieceType);

        // recalculate Zobrist Keys, incremental updates coming soon
        ZobristKey = Zobrist.CalcZobrist(this);

        RepetitionTable.Push(ZobristKey);
        }

        return IsLegal;
    }

    private static readonly ulong[] CastlingRightModifiers = {
        0x9000_0000_0000_0000ul, 0x1100_0000_0000_0000ul, 0x0000_0000_0000_0090ul, 0x0000_0000_0000_0011ul
    };

    public unsafe void force_null_move(SS* ss)
    {
        ep = EPSQ_NONE;
        us = (byte)(1-us);
        FiftyMoveCnt++;
        ZobristKey = Zobrist.CalcZobrist(this);
        RepetitionTable.Push(ZobristKey);
        SearchStack.Push(ss, move.NullMove, this, PIECE_TYPE_NONE, PIECE_TYPE_NONE);
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

    public string get_fen()
    {
        string fen = "";

        // Piece Representation
        for (int rank=7; rank>=0; rank--)
        {
            // count squares between pieces on a rank
            int cnt=0;

            for (int file=0; file<8; file++)
            {
                int sq = 8 * rank + file;
                int pt = piece_on(sq);

                if (pt != PIECE_TYPE_NONE)
                {
                    // if there are 
                    if (cnt > 0)
                    {
                        fen += (char)(cnt + '0');
                    }
                    fen += PieceChars[color_on(sq)][pt];
                    cnt = 0;
                }
                else
                {
                    cnt++;
                }
            }

            if (cnt > 0)
            {
                fen += (char)(cnt + '0');
            }

            fen += rank == 0 ? ' ' : '/';
        }

        // stm
        fen += us==WHITE ? "w " : "b ";

        // castling rights (kqKQ)
        if (castling_rights[2]) fen += 'K';
        if (castling_rights[3]) fen += 'Q';
        if (castling_rights[0]) fen += 'k';
        if (castling_rights[1]) fen += 'q';

        if (!castling_rights.Contains(true))
        {
            fen += "-";
        }

        fen += " ";

        if (ep != EPSQ_NONE)
        {
            int epsq = ep + (us==WHITE ? 8 : -8);
            fen += BoardNotation[epsq];
        }
        else
        {
            fen += '-';
        }

        return fen + " 0 0";
    }
}