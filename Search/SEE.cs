using static Constants;
using static Utils;
using static Attacks;
using System.Runtime.CompilerServices;

public static class SEE
{
    public static int[] SEE_values = {
        100, 450, 450, 650, 1250, 0, 0
    };

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe bool see_threshold(move m, ref pos p, int threshold)
    {
        int from     = m.from;
        int to       = m.to;
        int attacker = p.piece_on(from);
        int victim   = p.get_captured_pt(m);

        // Best-case value: Value of the captured piece, as it simply hangs.
        // Ff we can't beat the threshhold in best-case, dont even try
        int balance = SEE_values[victim] - threshold;
        if (balance < 0)
        {
            return false;
        }

        // worst-case szenario: attacker gets recaptured & we cant recapture
        // if we still beat the threshhold, we always win this exchange
        balance -= SEE_values[attacker];
        if (balance >= 0)
        {
            return true;
        }

        // if we get here, we have to compute the correct SEE value
        ulong bishops = p.pieceBB[BISHOP] | p.pieceBB[QUEEN];
        ulong rooks   = p.pieceBB[ROOK] | p.pieceBB[QUEEN];

        // pseudo make move
        ulong block = p.get_blocker();
        block = (block ^ (1ul << from)) | (1ul << to);
        if (m.IsEp)
        {
            block ^= 1ul << p.ep;
        }
        
        ulong allAttacker = p.attackers_to(to, block) & block;

        // start with opponents turn to recapture
        int stm = 1 - p.us;
        int pt;
        ulong myAttacker;

        while (true)
        {           
            myAttacker = allAttacker & p.colorBB[stm];
            if (myAttacker == 0)
            {
                break;
            }

            // get next least valuable attacker
            for (pt=PAWN; pt<=KING; pt++)
            {
                if ((myAttacker & p.pieceBB[pt]) != 0)
                {
                    break;
                }
            }

            // pseudo make capture on blocking Pieces
            // need different Attacker and blocker bitboards for next step
            block ^= 1ul << lsb(myAttacker & p.pieceBB[pt]);

            // new slider attackers might be uncovered
            if (pt == PAWN || pt == BISHOP || pt == QUEEN)
            {
                allAttacker |= BishopAttacks(to, block) & bishops;
            }
            if (pt == ROOK || pt == QUEEN)
            {
                allAttacker |= RookAttacks(to, block) & rooks;
            }

            // remove just moved Piece from attackers
            allAttacker &= block;

            stm = 1-stm;

            // if the recapture wont let us beat the threshhold again, we lost this exchange
            balance = -balance - 1 - SEE_values[pt];
            if (balance >= 0)
            {
                // if the king is the last piece to take but the opponent still protects
                // the piece, the kings capture is illegal
                if (pt == KING && (allAttacker & p.colorBB[stm]) != 0)
                {
                    stm = 1-stm;
                }

                break;
            }
        }

        // loop breaks when a side lost the exchange
        return stm != p.us;
    }

}
