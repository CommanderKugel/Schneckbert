using static Constants;
using static Utils;

using static System.Math;
using System.Runtime.CompilerServices;


public static partial class Search
{

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe int Negamax<NodeType>(pos p, int alpha, int beta, int depth, int ply, SS* ss, bool cutnode)
        where NodeType : NODE
    {

        bool isRoot = typeof(NodeType) == typeof(ROOT_NODE);
        bool nonPV  = typeof(NodeType) == typeof(NON_PV);
        bool isPV   = typeof(NodeType) == typeof(PV_NODE) || isRoot;
        bool allNode = !(isPV || cutnode);

        // pv tracking, but we dont have a pv yet
        if (isPV)
        {
            PV[ply][ply] = move.NullMove;
        }

        // #1 Quiescense Search
        //    If we arrive at a leafe node, drop into QSearch.
        //    It only makes sense to evaluate Quiet positions, because captures 
        //    drastically change the positions evaluation.
        //    To get a valid evaluation, play captures until no usefull capture remains.
        if (depth <= 0)
        {
            return QSearch<NodeType>(p, alpha, beta, ply, ss);
        }

        // #2 Timeout Check
        //    Negamax will negate this big score into the worst mate-score possible
        if (iteration > 1 && !TimeManager.InHardTimeLimit())
        {
            return SCORE_TIMEOUT;
        }

        // #3 Avoid stack-overflows or IndexOutOfBound Exceptions
        if (ply >= MAX_SEARCH_PLY)
        {
            return p.accumulator.Evaluate(ref p);
        }

        // #4 Draw Detection
        //    Excludes Stalemate, this is left to the move loop
        //    Exclude RootNodes, to always return a valid move.
        if (!isRoot && (
            RepetitionTable.IsRepeatedPosition(p) ||
            p.IsFiftyMoveDraw ||
            p.IsInsufficientMaterial))
        {
            return SCORE_DRAW;
        }

        // #5 Mate Distance Pruning
        //    If we already found a mate and the current search-depth guarantees
        //    that we cant find a quicker mate anymore, prune this branch.
        int mdAplha = Max(alpha, -SCORE_MATE + ply);
        int mdBeta  = Min(beta,   SCORE_MATE - ply - 1);
        if (!isRoot && mdAplha >= mdBeta)
        {
            return mdAplha;
        }
        

        // Count the Node as visited now
        TimeManager.NodeCnt++;

        // test if the side-to-move is in check
        ss->checkers = p.get_checkers();
        bool inCheck = ss->checkers != 0;
        bool inSingularity = !ss->ExcludedMove.IsNull;

        // bestScore will contain this nodes score, score is a tmp variable
        int bestScore = -SCORE_MATE;
        int score     = -SCORE_MATE;


        // #6 Transposition Table Probing
        //    A Transposition occurs, if one position is reached multiple times
        //    in the Search Tree, most likely over multiple IIR iterations.
        //    Test if there is a valid Transposition Table Entry for the current position.
        var ttEntry = TranspositionTable.get_entry(p.ZobristKey);
        bool ttHit = p.ZobristKey == ttEntry.key;
        move ttMove = ttHit ? ttEntry.move : move.NullMove;

        // Transposition Table Cutoff
        // If we found a valid TTEntry, we can return the saved score under some circumstances.
        if ( nonPV && ttHit && ttEntry.depth >= depth && 
            !score_is_terminal(ttEntry.score) && !inSingularity && 
            (
                ttEntry.flag == BOUND_UPPER && ttEntry.score <= alpha ||
                ttEntry.flag == BOUND_LOWER && ttEntry.score >= beta  ||
                ttEntry.flag == BOUND_EXACT
            )) 
        {
            return ttEntry.score;
        }


        // #7 Static Evaluation
        //    We will not return this score, because we didn't prove that this position is quiet.
        //    We can use this to make educated guesses about this position and this branch of the game tree.
        if (inCheck)
        {
            ss->StaticEval = ss->RawStaticEval = 0;
        }
        else
        {
            ss->StaticEval = 
                ss->RawStaticEval = p.accumulator.Evaluate(ref p);

            // #8 Static Evaluation Correction History
            //CorrHist.correct_static_eval(ref p, ss);
        }

        // Improving
        // A bool representing if our current static evaluation improved over the last full-move.
        // Not a pruning technique in itself, but used to slightly tweak other heuristics.

        bool improving = !inCheck && ply > 1 &&                 // currently not in Check
                         (ss-2)->StaticEval != 0 &&             // past was not in Check
                         (ss-2)->StaticEval < ss->StaticEval;   // compare valid Evaluations
        int  intImproving = improving ? 1 : 0;
        


        // #9 Reverse Futility Pruning
        //    if the static Evaluation beats beta by a margin, we are probably a piece up
        //    and the opponent needs to recapture somewhere earlier in the search-tree.
        //    Thus, we can safely cut here
        if (nonPV && !inCheck && !isRoot && depth <= 7 && !inSingularity &&
            ss->StaticEval - (cutnode ? 60 : 75) * (depth - intImproving) >= beta)
        {
            return ss->StaticEval;
        }


        // #10 Razoring
        //     If the static evaluation is far below alpha, even by a big margine, we are probably down
        //     a lot of material and probably have to recapture somewhere.
        //     If the Quiescense Score of this position still does not exceed alpha, we lost the piece 
        //     and must have made a tactical mistake to get to this node.
        //     We can prune this node in hopes to get back the material in another branch.
        if ( nonPV &&
            !inCheck &&
            !inSingularity &&
             depth <= 4 &&
            !score_is_terminal(alpha) &&
             ss->StaticEval + depth * 300 <= alpha)
        {
            int rfpScore = QSearch<NON_PV>(p, alpha, beta, ply, ss);

            if (rfpScore <= alpha)
            {
                return rfpScore;
            }
        }


        // #11 Null Move Pruning
        //     the Null-Move-Observation states, that in most positions, it is an advantage 
        //     to be able to move first. So if we can give our opponent two moves in a row, and
        //     still beat beta, this position is too good and we can cut off here.
        //     Zugzwang Positions are the exception and arent accounted for yet, e.g. via p.hasNonPawnMaterial()
        if ( nonPV && 
            !(ss-1)->Move.IsNull &&
            !inCheck && 
             depth>2 && 
            !inSingularity && 
             ss->StaticEval >= beta &&
            (!ttHit || ttEntry.flag != BOUND_UPPER || ttEntry.score >= beta))
        {
            pos posAfterNull = p;
            posAfterNull.force_null_move(ss);

            int nmpDepth = depth - 3 
                         - depth / 6;
            
            int nmpScore = -Negamax<NON_PV>(posAfterNull, -beta, -alpha, nmpDepth, ply+1, ss+1, false);
            RepetitionTable.Pop();

            if (nmpScore >= beta)
            {
                return nmpScore;
            }
        }


        // #12 Prob Cut
        // *COMING SOON*


        // #13 Check Extensions
        //     Positions where we are in Check are highly tactical and only a few moves are legal.
        //     Also, evasions are extremely important to the outcome of the game.
        //     To ensure we find the correct tactic here, extend this node.
        if (inCheck)
        {
            depth++;
        }

        // #14 Internal Iterative Reductions
        //     If we are at sufficient depth and we did not see this node before, search at a lower depth.
        //     Assume that we will encouner this node again in the next ID-iteration and search at the 
        //     intended depth, while also using a ttMove.
        if (!ttHit && depth > 4)
        {
            depth--;
        }

        
        // #15 Move Generating and Ordering
        //     outsourced via the MovePicker class
        //     ToDo: Staged Move Generation
        Span<move> moves = stackalloc move[MAX_MOVE_CNT];
        Span<int> scores = stackalloc int [MAX_MOVE_CNT];
        var picker = new MovePicker(p, false, ttMove, ss, ref moves, ref scores);


        // save the every played move and the number of played moves
        // to update Histries accordingly
        int movesPlayed    = 0;
        int quietsPlayed   = 0;
        int capturesPlayed = 0;
        Span<move> quietsList   = stackalloc move[MAX_MOVE_CNT];
        Span<move> capturesList = stackalloc move[MAX_MOVE_CNT];

        int startAlpha = alpha;
        move m;
        move locBestMove = move.NullMove;
        
        // main move loop here
        while (!(m = picker.next(ref p, ss, ply, ref moves, ref scores, ttMove)).IsNull)
        {

            if (m == ss->ExcludedMove)
            {
                continue;
            }

            bool isCapture           = p.is_capture(m);
            bool nonMatingLineExists = !score_is_terminal(bestScore);
            int  histScore           = isCapture ? 0 : History.get_quiet_hist(p.us, p.piece_on(m.from), m, ref p, ss, ply);
            long startNodes          = TimeManager.NodeCnt;

            // #16 Futility Pruning
            //     If static evaluation falls below alpha, even by a margin,
            //     we dont expect that quiet moves will gain enough to beat alpha again.
            //     Only applicable after proving a non-mate line exists (includes mvsplayed > 0 implicitly)
            // depth + intImproving -> +1.77 @ 8+0.08
            if ( nonMatingLineExists &&
                !isCapture && 
                !m.IsPromo &&
                 nonPV && 
                !inCheck &&
                 depth<5 && 
                (ss->StaticEval + 150 * (depth + intImproving) < alpha))
            {
                continue;
            }

            // #17 Late Move Pruning
            //     Assuming our moveordering is good, later picked moves are 
            //     assumed to be worse than earlier moves.
            //     So skip later, worse scoring and non-tactical moves at low depths.
            if (!isCapture &&
                !m.IsPromo &&
                 nonMatingLineExists &&
                 depth < 5 &&
                 movesPlayed > lmpTable[depth])
            {
                continue;
            }


            // #18 History Pruning
            // *COMING SOON*
            //     If the History Score of a move is really bad and 
            //     there are no tactical reasons to try the move, prune it.
            /*
            if ( nonPV &&
                !isCapture &&
                !m.IsPromo &&
                 nonMatingLineExists &&
                 depth < 5 &&
                 histScore < -512 * depth)
            {
                continue;
            }
            */


            // #19 Static Exchange Evaluation Pruning
            //     If the move hat a bad SEE score in move ordering,
            //     and the move can possibly be pruned, 
            //     run another SEE with a wider margin.
            if (nonMatingLineExists &&
                nonPV &&
                picker.stage_after_killermove(ref scores))
            {
                int margin = isCapture 
                           ? -200 * depth 
                           :  -25 * depth * depth;
                if (!SEE.see_threshold(m, ref p, margin))
                {
                    continue;
                }
            }

            // #20 Copy Make
            //     Copy the position, then make the move on the copied position.
            //     Avoids complex undo_move() method.
            pos nextPos = p;
            if (!nextPos.make_move(m, ss))
            {
                continue;
            }

            // save the played move, maybe its history will be updated later
            movesPlayed++;
            if (isCapture) capturesList[capturesPlayed++] = m;
            else           quietsList[quietsPlayed++]     = m;

            int extensions = 0;

            // #21 Singular Extensions
            //     If the TT suggests that we have a really strong move and we have sufficient depth
            //     left in our search-iteration, we can test if the TTMove is the only strong move.
            //     For that, search this node again, while excluding the candidate singular move.
            //     If no other move fails high, the move is singular and we should extend it,
            //     because it is a lot more important than the other moves.
            if ( movesPlayed == 1 &&
                !inSingularity &&
                !isRoot &&
                 depth >= 8 &&
                 m == ttMove &&
                 ttEntry.depth >= depth-3 &&
                 ttEntry.flag != BOUND_UPPER)
            {
                int singularBeta = Max(-SCORE_MATE+1, ttEntry.score - depth * 2);
                int singularDepth = (depth - 1) / 2;

                ss->ExcludedMove  = m;
                int singularScore = Negamax<NON_PV>(p, singularBeta-1, singularBeta, singularDepth, ply, ss, cutnode);
                ss->ExcludedMove  = move.NullMove;

                // Move is confirmed singular!
                // ToDo: triple extensions
                if (singularScore < singularBeta)
                {
                    extensions = !isPV && singularScore - 15 < singularBeta 
                               ?  2 : 1;
                }

                // #22 Multi Cut
                //     If the candidate-singular move is proven not singular and any other move would
                //     fail high, even in the current window-bounds, this position is probably too good
                //     to be true and our opponent wont allow this branch of the tree to be played out.
                else if ( singularScore >= singularBeta && 
                          singularBeta >= beta && 
                         !score_is_terminal(singularScore) && 
                         !score_is_loss(beta))
                {
                    RepetitionTable.Pop();
                    return singularBeta;
                }

                // #23.1 Negative Extensions
                //     If the Node was neither singular, nor produced a multi-cut snip and therefore is
                //     worse than we hoped for. Some singular candidates behave much different than expected
                //     and maybe arent even worth searching to full depth.

                // this was predicted to produce a multi cut, but didnt cause one
                //else if (ttEntry.score >= beta)
                //{
                //    extensions = -1;
                //}

                // 23.2
                // this was predicted to fail low in the first place
                //else if (ttEntry.score <= alpha) 
                //{
                //    extension = -1;
                //}

                // 23.3
                // CutNodes are predicted to produce fail-highs regularly, but neither the ttMove, 
                // nor the other moves seem to produce one.
                //else if (cutNode)
                //{
                //    extension = -1;
                //}               
            }

            // apply extensions
            int newDepth = depth + extensions - 1;

            
            // #24 Late Move Reductions
            //     Assuming our move-ordering is good, the later a move is picked, the worse it is.
            //     For later moves, we only want to prove that it really is worse, using a shallower search.
            if (movesPlayed > 1 && depth > 2 && !isCapture)
            {
                // TODO
                // nonPV, !improvong, CutNode

                // log-formula = 1 + log(depth) * log(moveCount) / 1.5
                int R = lmrTable[Min(depth, 63)][Min(movesPlayed, 63)];

                // History Reduction, probably needs bigger value for longer TC's
                // ToDo: R += -histScore / HIST_REDUCION_DIV; R = Max(R, 0);
                if (histScore < -256) R++;

                if (isPV) R--;

                R = Max(R, 0);

                score = -Negamax<NON_PV>(nextPos, -alpha-1, -alpha, newDepth+1-R, ply+1, ss+1, true);

                // if the shallower search failse high, we need to prove that the move really beats alpha
                // by re-searching at full depth.
                if (R > 1 && score > alpha)
                {
                    score = -Negamax<NON_PV>(nextPos, -alpha-1, -alpha, newDepth, ply+1, ss+1, !cutnode);
                }
            }

            // if LMR conditions dont apply, do a full-depth Zero-Window Search.
            else if (nonPV || movesPlayed > 1)
            {
                score = -Negamax<NON_PV>(nextPos, -alpha-1, -alpha, newDepth, ply+1, ss+1, !cutnode);
            }

            // if we are at a PVNode and ply either the first move, or a later move has beaten alpha, re-search
            // at full depth with a full window, to optain am exact score.
            if (isPV && (score > alpha || movesPlayed == 1))
            {
                score = -Negamax<PV_NODE>(nextPos, -beta, -alpha, newDepth, ply+1, ss+1, false);
            }


            // here would be the moment to undo the move but its just copy-make
            RepetitionTable.Pop();

            if (isRoot)
            {
                TimeManager.RootNodeCounts[m.FromTo] += TimeManager.NodeCnt - startNodes;
            }

            // #25 Score Update
            if (score > bestScore)
            {
                bestScore = score;
                locBestMove = m;

                // updating root moves mid-iteration works, because we always search the roots best move first
                if (isRoot)
                {
                    rootBestMove = m;
                }

                if (isPV)
                {
                    push_to_pv(m, ply);
                }

                if (score > alpha)
                {
                    alpha = score;

                    // #26 Fail High
                    //     If we beat beta, our opponent has a move that guarantees a position that scores beta,
                    //     so he will never allow us to get to a position with a better score and we can safely prune here.
                    //     This makes up about 95% of all pruning in common Chess-Engine Negamax implementations.
                    if (score >= beta)
                    {   

                        // #27 Update Killer Moves
                        if (!isCapture)
                        {
                            ss->killerMove = m;
                        }

                        // #28 Update History Values
                        int delta = History.calcHistDelta(
                             depth + 
                            (ss->StaticEval <= alpha ? 1 : 0) + // 5 Elo
                            (bestScore      >  beta  ? 1 : 0)   // 6 Elo
                        );
                        
                        if (isCapture) capturesPlayed--;
                        else           quietsPlayed--;

                        History.increaseSingleHistValue(m, delta, ref p, ss, ply, isCapture);
                        History.decreaseCaptureHistValues(ref capturesList, capturesPlayed, delta, ref p);
                        if (!isCapture)
                        {
                            History.decreaseQuietHistValues(ref quietsList, quietsPlayed, delta, ref p, ss, ply);
                        }

                        break;
                    }
                }
            }
        }

        // #29 Check- & Stalemate detection
        //     If no moves in a position are legal, the side to move is either chekmated or stalemated.
        //     Dont save terminal nodes in the TT.
        if (movesPlayed == 0)
        {
            return inCheck ? ply - SCORE_MATE : SCORE_DRAW;
        }

        int flag = bestScore >= beta ? BOUND_LOWER : alpha > startAlpha ? BOUND_EXACT : BOUND_UPPER;
        
        // #30 Save Node to TT
        //     Skip singular confirmation searches, because the best move was excluded there
        if (!inSingularity)
        {
            // For all-nodes, if there is already a ttEntry, dont overwrite the ttMove if alpha was not beaten.
            if (ttHit && !ttMove.IsNull && bestScore < alpha)
            {
                locBestMove = ttMove;
            }

            TranspositionTable.Push(p.ZobristKey, bestScore, depth, flag, locBestMove);
        }

        /*
        if (!inSingularity &&
            !inCheck &&
            (locBestMove.IsNull || !p.is_capture(locBestMove)) &&
            !score_is_terminal(bestScore) &&
            !(bestScore < ss->StaticEval && bestScore < beta ||
              bestScore > ss->StaticEval && bestScore > alpha))
        {
            CorrHist.update_corrhist(ref p, ss, depth, bestScore);
        }
        */

        return bestScore;
    }
}
