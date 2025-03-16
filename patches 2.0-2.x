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