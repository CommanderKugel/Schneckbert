using static Constants;
using static Utils;

public unsafe partial struct pos
{
    /// <summary>
    /// parses a fen an returns the expected position
    /// </summary>
    public pos (string fen) 
    {
        for (int pt=PAWN;  pt<=KING;  pt++) pieceBB[pt] = 0;
        for (int cl=BLACK; cl<=WHITE; cl++) colorBB[cl] = 0;
        for (int cr=0;     cr<4;      cr++) castlingRights[cr] = false;
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
        for (int pt=PAWN; pt<=KING; pt++)
        {
            PieceKeys[pt] = Zobrist.calc_piece_key(ref this, pt);
        }
        
        FiftyMoveCnt = 0;

        AccStack.stack[0] = new Accumulator(this);
    }

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
