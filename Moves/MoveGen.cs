using static Utils;
using static Constants;
using static Attacks;


public static class MoveGen
{

    public static byte GenerateMoves(Span<move> moves, pos p, bool OnlyCaptures)
    {
        int us, them;
        byte moveCnt;
        ulong block, mask;
        bool wtm;

        moveCnt = 0;
        us = p.us;
        them = 1 - us;
        wtm = us == WHITE;
        block = p.get_blocker();

        mask = OnlyCaptures ? p.colorBB[them] : ~p.colorBB[us];

        GeneratePieceMoves(moves, p.get_pieces(KNIGHT, us), KnightAttacks);
        GeneratePieceMoves(moves, p.get_pieces(BISHOP, QUEEN, us), BishopAttacks);
        GeneratePieceMoves(moves, p.get_pieces(ROOK, QUEEN, us), RookAttacks);
        GeneratePieceMoves(moves, p.get_pieces(KING, us), KingAttacks);

        GeneratePawnMoves(moves);

        if (!OnlyCaptures)
            GenerateCastlingMoves(moves);

        return moveCnt;


        void GeneratePieceMoves(Span<move> moves, ulong pieces, Func<int, ulong, ulong> F)
        {
            int from, to;
            ulong attacks;
            while (pieces != 0)
            {
                from = popLsb(ref pieces);
                attacks = F(from, block) & mask;
                while (attacks != 0)
                {
                    to = popLsb(ref attacks);
                    moves[moveCnt++] = new(from, to);
                }
            }
        }

        void ExtractPawnMoves(Span<move> moves, ulong pawns, int dir)
        {
            int to; ulong temp;
            temp = pawns & 0x00FF_FFFF_FFFF_FF00ul;
            // normal moves
            while (temp != 0)
            {
                to = popLsb(ref temp);
                moves[moveCnt++] = new move(to - dir, to);
            }
            // promotions
            temp = pawns & 0xFF00_0000_0000_00FFul;
            while (temp != 0)
            {
                to = popLsb(ref temp);
                moves[moveCnt++] = new move(to - dir, to, move.KnightPromo);
                moves[moveCnt++] = new move(to - dir, to, move.BishopPromo);
                moves[moveCnt++] = new move(to - dir, to, move.RookPromo);
                moves[moveCnt++] = new move(to - dir, to, move.QueenPromo);
            }
        }

        void GeneratePawnMoves(Span<move> moves)
        {
            int l, r, up;
            ulong pawns, enemy, empty, temp, thirdRank;
            Func<ulong, ulong> R, L, U;

            pawns = p.get_pieces(PAWN, us);
            enemy = p.colorBB[them];

            up = wtm ? 8 : -8;
            l = wtm ? 7 : -9;
            r = wtm ? 9 : -7;
            U = wtm ? north : south;
            R = wtm ? ne : se;
            L = wtm ? nw : sw;

            // Quiet Moves
            if (!OnlyCaptures)
            {
                empty = ~block;
                // simple push
                temp = U(pawns) & empty;
                ExtractPawnMoves(moves, temp, up);
                // double push
                thirdRank = wtm ? 0x0000_0000_00FF_0000ul : 0x0000_FF00_0000_0000ul;
                ExtractPawnMoves(moves, U(temp & thirdRank) & empty, up + up);
            }

            // simple right & left captures
            ExtractPawnMoves(moves, R(pawns) & enemy, r);
            ExtractPawnMoves(moves, L(pawns) & enemy, l);

            // en passant capture
            if (p.ep != EPSQ_NONE)
            {
                temp = pawns & (west(1ul << p.ep) | east(1ul << p.ep));
                while (temp != 0)
                {
                    up = popLsb(ref temp);
                    moves[moveCnt++] = new(up, wtm ? p.ep + 8 : p.ep - 8, move.EpCapture);
                }
            }
        }

        void GenerateCastlingMoves(Span<move> moves)
        {

            int ksq = p.get_ksq(us);

            // check for in check
            if ((p.attackers_to(ksq, block) & p.colorBB[them]) != 0)
                return;

            // Kingside castlingrights
            if (p.castling_rights[us + us] &&                             // singside castling rights
               (p.attackers_to(ksq + 1, block) & p.colorBB[them]) == 0 && // dont move through check
               (block & (1ul << ksq + 1 | 1ul << ksq + 2)) == 0)            // no piece in the way
                moves[moveCnt++] = new(ksq, ksq + 2);

            // Queenside castlingrights            
            if (p.castling_rights[us + us + 1] &&                               // singside castling rights
               (p.attackers_to(ksq - 1, block) & p.colorBB[them]) == 0 &&     // dont move through check
               (block & (1ul << ksq - 1 | 1ul << ksq - 2 | 1ul << ksq - 3)) == 0) // no piece in the way
                moves[moveCnt++] = new(ksq, ksq - 2);
        }


    }
}