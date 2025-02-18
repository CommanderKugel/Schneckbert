using static Constants;
using static Utils;
using static Attacks;
using System.Runtime.CompilerServices;

public unsafe partial struct pos
{
    // Board Representation
    public fixed ulong pieceBB[6]; 
    public fixed ulong colorBB[2];
    public fixed bool  castlingRights[4]; // kqKQ

    public int  ep;
    public byte us;
    public byte FiftyMoveCnt;

    public ulong ZobristKey;
    public ulong PawnKey;

    public Accumulator accumulator;


    // QOL Methods

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
    /// returns the PieceType of the captured Piece.
    /// Does not check if a move is indeed a capture or what color the captured Piece has.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int get_captured_pt(move m) => m.IsEp ? PAWN : piece_on(m.to);

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
            SearchStack.Push(ss, m, ref this, movingPieceType, capturedPieceType);
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
        SearchStack.Push(ss, move.NullMove, ref this, PIECE_TYPE_NONE, PIECE_TYPE_NONE);
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

}