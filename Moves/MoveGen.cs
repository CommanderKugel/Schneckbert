using static Utils;
using static Constants;
using static Attacks;
using System.Runtime.CompilerServices;


public static class MoveGen
{

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe int GenerateMoves(ref Span<move> moves, ref pos p, bool OnlyCaptures, ulong checker)
    {
        int moveCnt = 0;
        int us   = p.us;
        int them = 1 - us;
        bool wtm = us == WHITE;

        ulong block       = p.get_blocker();
        int   ksq         = p.get_ksq(p.us);
        ulong captureMask = OnlyCaptures ? p.colorBB[them] : ~p.colorBB[us];
        ulong checkMask   = GenerateCheckmask(ksq, checker);
        ulong mask        = captureMask & checkMask;

        GeneratePieceMoves(ref moves, ref moveCnt, p.get_pieces(KNIGHT, us), block, mask, KnightAttacks);
        GeneratePieceMoves(ref moves, ref moveCnt, p.get_pieces(BISHOP, us), block, mask, BishopAttacks);
        GeneratePieceMoves(ref moves, ref moveCnt, p.get_pieces(ROOK,   us), block, mask, RookAttacks);
        GeneratePieceMoves(ref moves, ref moveCnt, p.get_pieces(QUEEN,  us), block, mask, QueenAttacks);
        GeneratePieceMoves(ref moves, ref moveCnt, p.get_pieces(KING,   us), block, captureMask, KingAttacks);

        GeneratePawnCaptures(ref moves, ref moveCnt, ref p, checkMask);

        if (!OnlyCaptures)
        {
            GeneratePawnPushes(ref moves, ref moveCnt, ref p, checkMask);
            GenerateCastlingMoves(ref moves, ref moveCnt, ref p, checker, ksq);
        }

        return moveCnt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GenerateCheckmask(int ksq, ulong checker)
    {
        if (checker == 0) return ulong.MaxValue;
        if (more_than_one(checker)) return 0;
        return checker | Rays[ksq][lsb(checker)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void GeneratePieceMoves(ref Span<move> moves, ref int moveCnt, ulong pieces, ulong block, ulong relevant, Func<int, ulong, ulong> F)
    {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExtractPawnMoves(ref Span<move> moves, ref int moveCnt, ulong pawns, int dir)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GeneratePawnPushes(ref Span<move> moves, ref int moveCnt, ref pos p, ulong checkMask)
    {
        ulong pawns =  p.get_pieces(PAWN, p.us);
        ulong empty = ~p.get_blocker();

        int up = p.us==WHITE ? 8 : -8;
        Func<ulong, ulong> U = p.us==WHITE ? north : south;

        // simple push
        ulong temp = U(pawns) & empty;
        ExtractPawnMoves(ref moves, ref moveCnt, temp & checkMask, up);

        // double push
        ulong thirdRank = p.us==WHITE ? 0x0000_0000_00FF_0000ul : 0x0000_FF00_0000_0000ul;
        ExtractPawnMoves(ref moves, ref moveCnt, U(temp & thirdRank) & empty & checkMask, up + up);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void GeneratePawnCaptures(ref Span<move> moves, ref int moveCnt, ref pos p, ulong checkMask)
    {
        ulong pawns = p.get_pieces(PAWN, p.us);
        ulong enemy = p.colorBB[1-p.us];

        int l = p.us==WHITE ? 7 : -9;
        int r = p.us==WHITE ? 9 : -7;

        Func<ulong, ulong> R = p.us==WHITE ? ne : se;
        Func<ulong, ulong> L = p.us==WHITE ? nw : sw;

        // simple right & left captures
        ExtractPawnMoves(ref moves, ref moveCnt, R(pawns) & enemy & checkMask, r);
        ExtractPawnMoves(ref moves, ref moveCnt, L(pawns) & enemy & checkMask, l);

        // en passant capture
        if (p.ep != SQ_NONE)
        {
            ulong temp = pawns & (west(1ul << p.ep) | east(1ul << p.ep));
            while (temp != 0)
            {
                int sq = popLsb(ref temp);
                moves[moveCnt++] = new(sq, p.us==WHITE ? p.ep + 8 : p.ep - 8, move.EpCapture);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void GenerateCastlingMoves(ref Span<move> moves, ref int moveCnt, ref pos p, ulong checker, int ksq)
    {
        // check for in check
        if (checker != 0)
            return;

        ulong block = p.get_blocker();

        // Kingside castlingrights
        if (p.castlingRights[p.us + p.us] &&                                // kingside castling rights
            (p.attackers_to(ksq + 1, block) & p.colorBB[1-p.us]) == 0 &&    // dont move through check
            (block & (1ul << ksq + 1 | 1ul << ksq + 2)) == 0)               // no piece in the way
            moves[moveCnt++] = new(ksq, ksq + 2, move.Castles);

        // Queenside castlingrights            
        if (p.castlingRights[p.us + p.us + 1] &&                                // queenside castling rights
            (p.attackers_to(ksq - 1, block) & p.colorBB[1-p.us]) == 0 &&        // dont move through check
            (block & (1ul << ksq - 1 | 1ul << ksq - 2 | 1ul << ksq - 3)) == 0)  // no piece in the way
            moves[moveCnt++] = new(ksq, ksq - 2, move.Castles);
    }
}