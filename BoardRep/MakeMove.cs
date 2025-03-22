using static Constants;
using static Utils;
using static Attacks;

using System.Runtime.CompilerServices;
using System.Diagnostics;


public unsafe partial struct pos
{

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

        UpdateType type = UpdateType.AddSub;

        // make quiet move
        move_piece(us, movingPieceType, from, to);
        FiftyMoveCnt++;
        
        // make capture
        if (capturedPieceType != PIECE_TYPE_NONE) 
        {
            type = UpdateType.AddSubSub;
            pop_piece(to, capturedPieceType, 1-us);

            // captures reset the fifty move rule
            FiftyMoveCnt = 0;
        }

        // reset ep square, because was copyied from prev pos
        if (ep != SQ_NONE) 
        {
            ZobristKey ^= Zobrist.get_ep_key(ep);
            ep = SQ_NONE;
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
                type = UpdateType.AddSubSub;
                capturedPieceType = PAWN;
                int sq = wtm ? to-8 : to+8;
                pop_piece(sq, PAWN, 1-us);
            }
            
            // reset at every Pawn move
            FiftyMoveCnt = 0;
        }

        if (m.IsCastling) 
        {
            // Kingside Castling
            if (from < to) 
            {
                type = UpdateType.AddAddSubSub;
                int rookFrom = wtm ? H1 : H8;
                int rookTo   = wtm ? F1 : F8;
                move_piece(us, ROOK, rookFrom, rookTo);
            }

            // Queenside Castling
            if (from > to) 
            {
                type = UpdateType.AddAddSubSub;
                int rookFrom = wtm ? A1 : A8;
                int rookTo   = wtm ? D1 : D8;
                move_piece(us, ROOK, rookFrom, rookTo);
            }
        }

        bool IsLegal = (attackers_to(get_ksq(us), get_blocker()) & colorBB[1-us]) == 0;

        if (IsLegal) 
        {

            accumulator.Update(type, ref this, m, movingPieceType, capturedPieceType);

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

    private static readonly ulong[] CastlingRightModifiers = { 0x9000_0000_0000_0000ul, 0x1100_0000_0000_0000ul, 0x0000_0000_0000_0090ul, 0x0000_0000_0000_0011ul };



    public bool is_legal (move m)
    {
        return false;
    }

    private bool normal_is_legal (move m)
    {
        return false;
    }

    private bool ep_is_legal (move m)
    {
        return false;
    }

    private bool castling_is_legal (move m)
    {
        return false;
    }


    /// <summary>
    /// Returns true if the given move abides to the fundamental laws of piece movement.
    /// Does not test if the king is left in check afterwads.
    /// </summary>
    public bool is_pseudo_legal(move m)
    {
        int pt = piece_on(m.from);

        // catch obviously illegal cases
        if (m.IsNull || 
            pt == PIECE_NONE ||
            color_on(m.from) != us || 
            color_on(m.to  ) == us)
        {
            return false;
        }

        // why did i just assume legality for castling moves??? obviously wrong wtf
        if (m.IsCastling)
        {
            bool kingside = m.from < m.to;
            int s = m.from;
            ulong blocker = kingside ? 1ul << s+1 | 1ul << s+2 : 1ul << s-1 | 1ul << s-2 | 1ul << s-3;

            return pt == KING &&
                   castlingRights[us + us + (kingside ? 0 : 1)] &&
                  (blocker & get_blocker()) == 0 &&
                  (attackers_to(kingside?s+1:s-1, get_blocker()) & colorBB[1-us]) == 0;
        }

        // test if the destination is accessible to the Piece
        return (PieceAttacks(ref this, pt, m.from) & (1ul << m.to)) != 0;
    }

}
