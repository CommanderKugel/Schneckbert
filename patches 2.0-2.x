2.1 QS SEE pruning Fix
    simply prune all bad captures
    futility value braks as soon as alpha is a terminal score
--------------------------------------------------
Results of dev vs main (8+0.08, NULL, 16MB, UHO_2024_8mvs_big_+105_+124.pgn):
Elo: 11.85 +/- 6.54, nElo: 18.88 +/- 10.41
LOS: 99.98 %, DrawRatio: 43.20 %, PairsRatio: 1.24
Games: 4282, Wins: 1295, Losses: 1149, Draws: 1838, Points: 2214.0 (51.70 %)
Ptnml(0-2): [73, 469, 925, 587, 87], WL/DD Ratio: 1.37
LLR: 2.89 (-2.25, 2.89) [0.00, 5.00]
--------------------------------------------------
Results of dev vs main (20+0.2, NULL, 16MB, UHO_2024_8mvs_big_+105_+124.pgn):
Elo: 5.88 +/- 4.17, nElo: 9.79 +/- 6.94
LOS: 99.71 %, DrawRatio: 45.81 %, PairsRatio: 1.12
Games: 9632, Wins: 2708, Losses: 2545, Draws: 4379, Points: 4897.5 (50.85 %)
Ptnml(0-2): [139, 1091, 2206, 1228, 152], WL/DD Ratio: 1.14
LLR: 2.90 (-2.25, 2.89) [0.00, 5.00]
--------------------------------------------------

2.2 TT indications to try NMP
&& (!ttHit || ttScore >= beta || ttFlag != BoundUpper)
longest fucking shit so far
-------------------------------------------------
Results of dev vs main (8+0.08, NULL, 16MB, UHO_2024_8mvs_big_+105_+124.pgn):
Elo: 5.50 +/- 4.01, nElo: 8.83 +/- 6.44
LOS: 99.64 %, DrawRatio: 44.12 %, PairsRatio: 1.11
Games: 11192, Wins: 3191, Losses: 3014, Draws: 4987, Points: 5684.5 (50.79 %)
Ptnml(0-2): [193, 1291, 2469, 1432, 211], WL/DD Ratio: 1.18
LLR: 2.93 (-2.25, 2.89) [0.00, 5.00]
--------------------------------------------------
Results of dev vs main (20+0.2, NULL, 16MB, UHO_2024_8mvs_big_+105_+124.pgn):
Elo: 3.12 +/- 2.54, nElo: 5.27 +/- 4.29
LOS: 99.20 %, DrawRatio: 46.34 %, PairsRatio: 1.06
Games: 25182, Wins: 6930, Losses: 6704, Draws: 11548, Points: 12704.0 (50.45 %)
Ptnml(0-2): [322, 2963, 5835, 3109, 362], WL/DD Ratio: 1.13
LLR: 2.89 (-2.25, 2.89) [0.00, 5.00]
--------------------------------------------------

2.3 Bigger History Bonus if bestScore > beta 
i dont understand this at all, this patch is a total brain fart
--------------------------------------------------
Results of dev vs main (8+0.08, NULL, 16MB, UHO_2024_8mvs_big_+105_+124.pgn):
Elo: 5.58 +/- 4.08, nElo: 8.87 +/- 6.48
LOS: 99.63 %, DrawRatio: 44.60 %, PairsRatio: 1.09
Games: 11028, Wins: 3198, Losses: 3021, Draws: 4809, Points: 5602.5 (50.80 %)
Ptnml(0-2): [197, 1263, 2459, 1356, 239], WL/DD Ratio: 1.25
LLR: 2.91 (-2.25, 2.89) [0.00, 5.00]
--------------------------------------------------
Results of dev vs main (20+0.2, NULL, 16MB, UHO_2024_8mvs_big_+105_+124.pgn):
Elo: 5.32 +/- 3.91, nElo: 8.84 +/- 6.48
LOS: 99.62 %, DrawRatio: 45.68 %, PairsRatio: 1.10
Games: 11028, Wins: 3087, Losses: 2918, Draws: 5023, Points: 5598.5 (50.77 %)
Ptnml(0-2): [154, 1274, 2519, 1383, 184], WL/DD Ratio: 1.13
LLR: 2.89 (-2.25, 2.89) [0.00, 5.00]
--------------------------------------------------

2.4 bigger History Bonus if eval <= alpha
--------------------------------------------------
Results of dev vs main (8+0.08, NULL, 16MB, UHO_2024_8mvs_big_+105_+124.pgn):
Elo: 10.15 +/- 5.92, nElo: 16.47 +/- 9.60
LOS: 99.96 %, DrawRatio: 44.83 %, PairsRatio: 1.22
Games: 5032, Wins: 1480, Losses: 1333, Draws: 2219, Points: 2589.5 (51.46 %)
Ptnml(0-2): [83, 543, 1128, 668, 94], WL/DD Ratio: 1.24
LLR: 2.90 (-2.25, 2.89) [0.00, 5.00]
--------------------------------------------------
Results of dev vs main (20+0.2, NULL, 16MB, UHO_2024_8mvs_big_+105_+124.pgn):
Elo: 6.38 +/- 4.41, nElo: 10.62 +/- 7.34
LOS: 99.77 %, DrawRatio: 45.05 %, PairsRatio: 1.11
Games: 8600, Wins: 2440, Losses: 2282, Draws: 3878, Points: 4379.0 (50.92 %)
Ptnml(0-2): [108, 1011, 1937, 1103, 141], WL/DD Ratio: 1.20
LLR: 2.89 (-2.25, 2.89) [0.00, 5.00]
--------------------------------------------------