using System.Collections.Generic;

public class CheckersBoard
{
    public const int SIZE = 8;
    public const int EMPTY = 0;
    public const int BLACK = 1;
    public const int WHITE = 2;
    public const int BLACK_KING = 3;
    public const int WHITE_KING = 4;

    public int[,] grid;

    public CheckersBoard()
    {
        grid = new int[SIZE, SIZE];
        InitBoard();
    }

    public CheckersBoard(int[,] copyGrid)
    {
        grid = new int[SIZE, SIZE];
        for (int r = 0; r < SIZE; r++)
            for (int c = 0; c < SIZE; c++)
                grid[r, c] = copyGrid[r, c];
    }

    public CheckersBoard Clone()
    {
        return new CheckersBoard(grid);
    }

    void InitBoard()
    {
        for (int r = 0; r < SIZE; r++)
        {
            for (int c = 0; c < SIZE; c++)
            {
                grid[r, c] = EMPTY;
                if ((r + c) % 2 == 1)
                {
                    if (r < 3) grid[r, c] = BLACK;
                    else if (r > 4) grid[r, c] = WHITE;
                }
            }
        }
    }

    public static bool IsBlack(int piece) { return piece == BLACK || piece == BLACK_KING; }
    public static bool IsWhite(int piece) { return piece == WHITE || piece == WHITE_KING; }
    public static bool IsKing(int piece) { return piece == BLACK_KING || piece == WHITE_KING; }

    public static bool SameTeam(int piece, int player)
    {
        if (player == BLACK || player == BLACK_KING) return IsBlack(piece);
        if (player == WHITE || player == WHITE_KING) return IsWhite(piece);
        return false;
    }

    public static bool IsEnemy(int piece, int player)
    {
        if (piece == EMPTY) return false;
        if (player == BLACK || player == BLACK_KING) return IsWhite(piece);
        if (player == WHITE || player == WHITE_KING) return IsBlack(piece);
        return false;
    }

    // captures are mandatory; returns only captures if any exist
    public List<Move> GetLegalMoves(int player)
    {
        List<Move> captures = new List<Move>();
        List<Move> simpleMoves = new List<Move>();

        for (int r = 0; r < SIZE; r++)
        {
            for (int c = 0; c < SIZE; c++)
            {
                if (!SameTeam(grid[r, c], player)) continue;

                List<Move> pieceCaps = GetCaptures(r, c, player);
                captures.AddRange(pieceCaps);

                List<Move> pieceSimple = GetSimpleMoves(r, c, player);
                simpleMoves.AddRange(pieceSimple);
            }
        }

        if (captures.Count > 0) return captures;
        return simpleMoves;
    }

    List<Move> GetSimpleMoves(int r, int c, int player)
    {
        List<Move> moves = new List<Move>();
        int piece = grid[r, c];
        int[] rowDirs = GetRowDirs(piece);

        foreach (int dr in rowDirs)
        {
            foreach (int dc in new int[] { -1, 1 })
            {
                int nr = r + dr;
                int nc = c + dc;
                if (InBounds(nr, nc) && grid[nr, nc] == EMPTY)
                {
                    Move m = new Move();
                    m.steps.Add(new int[] { r, c });
                    m.steps.Add(new int[] { nr, nc });
                    moves.Add(m);
                }
            }
        }
        return moves;
    }

    List<Move> GetCaptures(int r, int c, int player)
    {
        List<Move> results = new List<Move>();
        List<int[]> jumped = new List<int[]>();
        Move current = new Move();
        current.steps.Add(new int[] { r, c });
        FindCapturesRecursive(r, c, player, grid[r, c], jumped, current, results);
        return results;
    }

    void FindCapturesRecursive(int r, int c, int player, int piece, List<int[]> jumped, Move current, List<Move> results)
    {
        bool found = false;
        int[] rowDirs = GetRowDirs(piece);

        foreach (int dr in rowDirs)
        {
            foreach (int dc in new int[] { -1, 1 })
            {
                int mr = r + dr;
                int mc = c + dc;
                int lr = r + dr * 2;
                int lc = c + dc * 2;

                if (!InBounds(lr, lc)) continue;
                if (!IsEnemy(grid[mr, mc], player)) continue;
                if (grid[lr, lc] != EMPTY) continue;
                if (AlreadyJumped(jumped, mr, mc)) continue;

                found = true;
                jumped.Add(new int[] { mr, mc });
                current.steps.Add(new int[] { lr, lc });
                current.captured.Add(new int[] { mr, mc });

                // promote if landing on back rank mid-chain
                int newPiece = piece;
                if (!IsKing(piece))
                {
                    if ((IsBlack(piece) && lr == SIZE - 1) || (IsWhite(piece) && lr == 0))
                        newPiece = IsBlack(piece) ? BLACK_KING : WHITE_KING;
                }

                FindCapturesRecursive(lr, lc, player, newPiece, jumped, current, results);

                jumped.RemoveAt(jumped.Count - 1);
                current.steps.RemoveAt(current.steps.Count - 1);
                current.captured.RemoveAt(current.captured.Count - 1);
            }
        }

        if (!found && current.steps.Count > 1)
        {
            results.Add(current.Copy());
        }
    }

    bool AlreadyJumped(List<int[]> jumped, int r, int c)
    {
        foreach (int[] j in jumped)
            if (j[0] == r && j[1] == c) return true;
        return false;
    }

    public void ApplyMove(Move move)
    {
        int[] start = move.steps[0];
        int[] end = move.steps[move.steps.Count - 1];
        int piece = grid[start[0], start[1]];

        foreach (int[] cap in move.captured)
            grid[cap[0], cap[1]] = EMPTY;

        grid[start[0], start[1]] = EMPTY;
        grid[end[0], end[1]] = piece;

        // king promotion
        if (piece == BLACK && end[0] == SIZE - 1) grid[end[0], end[1]] = BLACK_KING;
        if (piece == WHITE && end[0] == 0) grid[end[0], end[1]] = WHITE_KING;
    }

    public bool HasNoPieces(int player)
    {
        for (int r = 0; r < SIZE; r++)
            for (int c = 0; c < SIZE; c++)
                if (SameTeam(grid[r, c], player)) return false;
        return true;
    }

    public bool HasNoMoves(int player)
    {
        return GetLegalMoves(player).Count == 0;
    }

    int[] GetRowDirs(int piece)
    {
        if (IsKing(piece)) return new int[] { -1, 1 };
        if (IsBlack(piece)) return new int[] { 1 };  // black moves down
        return new int[] { -1 };                      // white moves up
    }

    bool InBounds(int r, int c)
    {
        return r >= 0 && r < SIZE && c >= 0 && c < SIZE;
    }
}

// sequence of board positions visited and squares captured
public class Move
{
    public List<int[]> steps;
    public List<int[]> captured;

    public Move()
    {
        steps = new List<int[]>();
        captured = new List<int[]>();
    }

    public Move Copy()
    {
        Move m = new Move();
        foreach (int[] s in steps) m.steps.Add(new int[] { s[0], s[1] });
        foreach (int[] c in captured) m.captured.Add(new int[] { c[0], c[1] });
        return m;
    }
}