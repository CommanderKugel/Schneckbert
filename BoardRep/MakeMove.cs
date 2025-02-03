using static Constants;
using static Utils;

using System.Runtime.CompilerServices;


public unsafe partial struct pos
{
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

        if (m.IsCastles) 
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
        
        bool IsLegal = get_checkers() == 0;
        

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


    public unsafe bool is_pseudo_legal(move m)
    {
        // obvious candidates
        if (m.IsNull ||
            color_on(m.from) != us) 
        {
            return false;
        }

        int pt = piece_on(m.from);

        return false;
    }

}
