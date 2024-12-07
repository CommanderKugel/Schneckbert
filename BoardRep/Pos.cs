using static Constants;
using static Utils;
using static Attacks;
using System.Runtime.CompilerServices;

public unsafe struct pos
{   
    // Board Representation
    public fixed ulong pieceBB[6], colorBB[2];
    public fixed bool castling_rights[4];   // kqKQ

    public int   ep = EPSQ_NONE;
    public byte  us = WHITE;

    public byte FiftyMoveCnt = 0;
    public ulong ZobristKey = 0;

    public Accumulator accumulator;


    /// <summary>
    /// copies all values from the parent position
    /// essential part of "copy/make"
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public pos(pos p) 
    {
        this = p;
    }

    /// <summary>
    /// parses a fen an returns the expected position
    /// </summary>
    public pos (string fen) 
    {
        for (int pt=PAWN;  pt<=KING;  pt++) pieceBB[pt] = 0;
        for (int cl=BLACK; cl<=WHITE; cl++) colorBB[cl] = 0;
        for (int cr=0;     cr<4;      cr++) castling_rights[cr] = false;
        this.accumulator = new Accumulator(this);
        SearchStack.stack[0] = new SS();
        
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong get_pieces(int pt) => pieceBB[pt];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong get_pieces(int pt, int c) => pieceBB[pt] & colorBB[c];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong get_pieces(int pt1, int pt2, int c) => (pieceBB[pt1] | pieceBB[pt2]) & colorBB[c];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int get_ksq(int c) => lsb(pieceBB[KING] & colorBB[c]);

    /// <summary>
    /// returns the PieceType of the Piece on the given Square-index
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int color_on(int sq) 
    {
        if (((1ul << sq) & colorBB[WHITE]) != 0)
            return WHITE;
        if (((1ul << sq) & colorBB[BLACK]) != 0)
            return BLACK;
        return COLOR_NONE;
    }

    /// <summary>
    /// returns true if the opponents colorBB has a bit set on the moves to-square
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool is_capture(move m) => m.IsEp || (colorBB[1-us] & (1ul << m.to)) != 0;

    /// <summary>
    /// returns a bitboard containing all occupants of both WHITE and BLACK pieces
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong get_checkers() => attackers_to(get_ksq(us), get_blocker()) & colorBB[1-us];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void set_piece(int sq, int pt, int color) 
    {
        pieceBB[pt]    ^= 1ul << sq;
        colorBB[color] ^= 1ul << sq;

        ZobristKey ^= Zobrist.get_piece_key(color, pt, sq);
        accumulator.activate(color, pt, sq);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void pop_piece(int sq, int pt, int color)
    {
        pieceBB[pt]    ^= 1ul << sq;
        colorBB[color] ^= 1ul << sq;

        ZobristKey ^= Zobrist.get_piece_key(color, pt, sq);
        accumulator.deactivate(color, pt, sq);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void move_piece(int color, int pt, int from, int to)
    {
        pieceBB[pt] ^= (1ul << from) | (1ul << to);
        colorBB[color] ^= (1ul << from) | (1ul << to);

        ZobristKey ^= Zobrist.get_piece_key(color, pt, from)
                   ^  Zobrist.get_piece_key(color, pt, to);
        accumulator.activate(color, pt, to);
        accumulator.deactivate(color, pt, from);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe bool make_move(move m, SS* ss) 
    {
        int from = m.from;
        int to   = m.to;
        int dist = to-from;
        bool wtm = us == WHITE;

        int movingPieceType   = piece_on(from);
        int capturedPieceType = piece_on(to);

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
            ZobristKey ^= Zobrist.get_ep_key(ep);
            ep = EPSQ_NONE;
        }

        if (movingPieceType == PAWN) 
        {
            // double pawn push
            if (Math.Abs(dist) == 16) 
            {
                ep = to;
                ZobristKey ^= Zobrist.get_ep_key(to);
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
        ZobristKey ^= Zobrist.get_stm_key();


        if (IsLegal) 
        {
            // update castling rights
            // as soon as any piece on the relevant squares moves or gets captured,
            // the right will be removed
            ulong FromTo = (1ul << from) | (1ul << to);
            for (int cr=0; cr<4; cr++)
            {
                if (castling_rights[cr] && (FromTo & CastlingRightModifiers[cr]) != 0)
                {
                    castling_rights[cr] = false;
                    ZobristKey ^= Zobrist.get_castling_key(cr);
                }
            }

            // update Search Stack and Repetition Table
            SearchStack.Push(ss, m, this, movingPieceType, capturedPieceType);
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
        if (!(castling_rights[0] || castling_rights[1] || 
              castling_rights[2] || castling_rights[3]))
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