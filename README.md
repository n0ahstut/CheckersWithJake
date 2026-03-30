# CheckersWithJake
Checkers AI with Alpha-Beta Pruning in Unity
A fully playable Checkers game with an AI opponent powered by Minimax search and Alpha-Beta pruning

How to Play

Launch the game
Choose your side — Play as White (AI moves first) or Play as Black (you move first)
Click a piece to select it — legal moves highlight in green
Click a highlighted square to move
The AI will respond automatically
Press Restart to return to the menu at any time

Controls

Left Click — Select a piece / move to a square
Restart Button — Return to the main menu

Rules

Standard 8x8 Checkers on dark squares only
Pieces move diagonally forward; kings move forward and backward
Captures are mandatory — if you can jump, you must
Multi-jump chains are supported
A piece reaching the opponent's back row is promoted to a king
Win by capturing all opponent pieces or blocking all their moves

AI Design
Minimax with Alpha-Beta Pruning
The AI uses recursive Minimax search with Alpha-Beta pruning to find the best move. It alternates between maximizing (AI) and minimizing (opponent) layers, pruning branches where the outcome cannot improve the current best option.
