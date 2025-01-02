using static Constants;
using static Utils;
using static Attacks;
using System.Runtime.CompilerServices;

public unsafe struct pos
{   
    // Board Representation
    public fixed ulong pieceBB[6], 
                       colorBB[2];
    public fixed bool  castlingRights[4]; // kqKQ

    public int  ep;
    public byte us;

    public byte FiftyMoveCnt;
    public ulong ZobristKey,
                 PawnKey;

    public Accumulator accumulator;



    /// <summary>
    /// parses a fen an returns the expected position
    /// </summary>
    public pos (string fen) 
    {
        for (int pt=PAWN;  pt<=KING;  pt++) pieceBB[pt] = 0;
        for (int cl=BLACK; cl<=WHITE; cl++) colorBB[cl] = 0;
        for (int cr=0;     cr<4;      cr++) castlingRights[cr] = false;
        this.accumulator = new Accumulator();
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
                case ' ': break;
                default: try{f += int.Parse(c.ToString())-1;} catch{} break;
            }
        }

        us = (byte)((fen[idx++] == 'w') ? WHITE : BLACK);

        for (char cr = fen[++idx]; cr != ' ' && cr != '-'; cr = fen[++idx]) 
        {
            switch (cr) 
            {
                case 'K': castlingRights[WHITE+WHITE  ] = true; break;
                case 'Q': castlingRights[WHITE+WHITE+1] = true; break;
                case 'k': castlingRights[BLACK+BLACK  ] = true; break;
                case 'q': castlingRights[BLACK+BLACK+1] = true; break;
                default: break;
            }
        }
        
        if (fen[++idx] != '-' && fen[idx] != ' ') 
        {
            ep = CharsToSquare(fen[idx], fen[++idx]);
        }
        else
        {
            ep = SQ_NONE;
        }

        ZobristKey = Zobrist.calc_zobrist_key(ref this);
        PawnKey    = Zobrist.calc_pawn_key(ref this);
        FiftyMoveCnt = 0;

        accumulator = new Accumulator(this);
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

    /// <summary>
    /// Applies a pseudolegal move to the position.
    /// Returns true if the move is legal, false if the king is left in check.
    /// Also updates the Search Stack and Repetiton Table.
    /// </summary>
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

            if (capturedPieceType == PAWN)
            {
                PawnKey ^= Zobrist.get_piece_key(1-us, PAWN, to);
            }
        }

        // reset ep square, because was copyied from prev pos
        if (ep != SQ_NONE) 
        {
            ZobristKey ^= Zobrist.get_ep_key(ep);
            ep = SQ_NONE;
        }

        if (movingPieceType == PAWN) 
        {
            PawnKey ^= Zobrist.get_piece_key(us, PAWN, from)
                    ^  Zobrist.get_piece_key(us, PAWN, to);

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
                PawnKey ^= Zobrist.get_piece_key(us, PAWN, to);
            }

            // en passant capture
            else if (m.IsEp) 
            {
                capturedPieceType = PAWN;
                int sq = wtm ? to-8 : to+8;
                pop_piece(sq, PAWN, 1-us);
                PawnKey ^= Zobrist.get_piece_key(1-us, PAWN, sq);
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
        

        if (IsLegal) 
        {

            accumulator.update_hm(movingPieceType, from, to, ref this);

            //var testAcc = new Accumulator(this);
            //var _ = accumulator == testAcc;

            us = (byte)(1-us);
            ZobristKey ^= Zobrist.get_stm_key();

            // update castling rights
            // as soon as any piece on the relevant squares moves or gets captured,
            // the right will be removed
            ulong FromTo = (1ul << from) | (1ul << to);
            for (int cr=0; cr<4; cr++)
            {
                if (castlingRights[cr] && (FromTo & CastlingRightModifiers[cr]) != 0)
                {
                    castlingRights[cr] = false;
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

    /// <summary>
    /// Applies a null-move to the position without checking for anything!
    /// Also updates the Repetition Table and Search Stack.
    /// </summary>
    public unsafe void force_null_move(SS* ss)
    {
        ep = SQ_NONE;
        us = (byte)(1-us);
        FiftyMoveCnt++;
        ZobristKey = Zobrist.calc_zobrist_key(ref this);
        RepetitionTable.Push(ZobristKey);
        SearchStack.Push(ss, move.NullMove, this, PIECE_TYPE_NONE, PIECE_TYPE_NONE);
    }

    public static bool operator ==(pos p1, pos p2) => p1.ZobristKey == p2.ZobristKey;
    public static bool operator !=(pos p1, pos p2) => p1.ZobristKey != p2.ZobristKey;
    
    /// <summary>
    /// A Game ends in a draw, if there is not enough material to force a Checkmate for any Color.
    /// Returns true, if there are no Rooks, Queen or Pawns, or not at least two Minor Pieces for either Color.
    /// Ignores, that Two Knights alone are not enough material to force Checkmate in most Positions.
    /// </summary>
    public bool IsInsufficientMaterial => (
        (pieceBB[PAWN] | pieceBB[ROOK] | pieceBB[QUEEN]) == 0 &&
        !more_than_one(get_pieces(BISHOP, KNIGHT, WHITE)) &&
        !more_than_one(get_pieces(BISHOP, KNIGHT, BLACK))
    );

    /// <summary>
    /// A Game ends in a draw, if 50 full moves were played out, without moving a Pawn or capturing a Piece.
    /// Returns true, if the halfmovecounter for the fifty-move-rule is bigger than 100.
    /// </summary>
    public readonly bool IsFiftyMoveDraw => FiftyMoveCnt > 99;

    /// <summary>
    /// Returns a FEN (string) representation of the position.
    /// </summary>
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

        // castling rights convention: kqKQ
        if (castlingRights[2]) fen += 'K';
        if (castlingRights[3]) fen += 'Q';
        if (castlingRights[0]) fen += 'k';
        if (castlingRights[1]) fen += 'q';
        if (!(castlingRights[0] || castlingRights[1] || 
              castlingRights[2] || castlingRights[3]))
        {
            fen += "-";
        }

        fen += " ";

        if (ep != SQ_NONE)
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