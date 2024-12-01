using static Utils;
using static Constants;
using static Attacks;


public static class MoveGen
{

    public static int GenerateMoves(Span<move> moves, pos p, bool OnlyCaptures, ulong checker)
    {
        int moveCnt = 0;
        int us   = p.us;
        int them = 1 - us;
        bool wtm = us == WHITE;

        int ksq = p.get_ksq(us);

        ulong block       = p.get_blocker();
        ulong captureMask = OnlyCaptures ? p.colorBB[them] : ~p.colorBB[us];
        ulong checkMask   = GenerateCheckmask(ksq, checker);
        ulong mask        = captureMask & checkMask;

        GeneratePieceMoves(moves, KNIGHT, mask, KnightAttacks);
        GeneratePieceMoves(moves, BISHOP, mask, BishopAttacks);
        GeneratePieceMoves(moves, ROOK,   mask, RookAttacks);
        GeneratePieceMoves(moves, QUEEN,  mask, QueenAttacks);
        GeneratePieceMoves(moves, KING,   captureMask, KingAttacks);

        GeneratePawnCaptures(moves, checkMask);

        if (!OnlyCaptures)
        {
            GeneratePawnPushes(moves, checkMask);
            GenerateCastlingMoves(moves, ksq, checker);
        }

        return moveCnt;

        ulong GenerateCheckmask(int ksq, ulong checker)
        {
            if (checker == 0) return ulong.MaxValue;
            if (more_than_one(checker)) return 0;
            return checker | Rays[ksq][lsb(checker)];
        }

        void GeneratePieceMoves(Span<move> moves, int pt, ulong relevant, Func<int, ulong, ulong> F)
        {
            ulong pieces = p.get_pieces(pt, us);
            while (pieces != 0)
            {
                int from = popLsb(ref pieces);
                ulong attacks = F(from, block) & relevant;

                while (attacks != 0)
                {
                    int to = popLsb(ref attacks);
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

        void GeneratePawnPushes(Span<move> moves, ulong checkMask)
        {
            ulong pawns = p.get_pieces(PAWN, us);
            ulong empty = ~block;

            int up = wtm ? 8 : -8;
            Func<ulong, ulong> U = wtm ? north : south;

            // simple push
            ulong temp = U(pawns) & empty;
            ExtractPawnMoves(moves, temp & checkMask, up);

            // double push
            ulong thirdRank = wtm ? 0x0000_0000_00FF_0000ul : 0x0000_FF00_0000_0000ul;
            ExtractPawnMoves(moves, U(temp & thirdRank) & empty & checkMask, up + up);
        }

        void GeneratePawnCaptures(Span<move> moves, ulong checkMask)
        {
            ulong pawns = p.get_pieces(PAWN, us);
            ulong enemy = p.colorBB[them];

            int l = wtm ? 7 : -9;
            int r = wtm ? 9 : -7;

            Func<ulong, ulong> R = wtm ? ne : se;
            Func<ulong, ulong> L = wtm ? nw : sw;

            // simple right & left captures
            ExtractPawnMoves(moves, R(pawns) & enemy & checkMask, r);
            ExtractPawnMoves(moves, L(pawns) & enemy & checkMask, l);

            // en passant capture
            if (p.ep != EPSQ_NONE)
            {
                ulong temp = pawns & (west(1ul << p.ep) | east(1ul << p.ep));
                while (temp != 0)
                {
                    int sq = popLsb(ref temp);
                    moves[moveCnt++] = new(sq, wtm ? p.ep + 8 : p.ep - 8, move.EpCapture);
                }
            }
        }

        void GenerateCastlingMoves(Span<move> moves, int ksq, ulong checker)
        {
            // check for in check
            if (checker != 0)
                return;

            // Kingside castlingrights
            if (p.castling_rights[us + us] &&                             // kingside castling rights
               (p.attackers_to(ksq + 1, block) & p.colorBB[them]) == 0 && // dont move through check
               (block & (1ul << ksq + 1 | 1ul << ksq + 2)) == 0)            // no piece in the way
                moves[moveCnt++] = new(ksq, ksq + 2);

            // Queenside castlingrights            
            if (p.castling_rights[us + us + 1] &&                               // queenside castling rights
               (p.attackers_to(ksq - 1, block) & p.colorBB[them]) == 0 &&     // dont move through check
               (block & (1ul << ksq - 1 | 1ul << ksq - 2 | 1ul << ksq - 3)) == 0) // no piece in the way
                moves[moveCnt++] = new(ksq, ksq - 2);
        }
    }
}