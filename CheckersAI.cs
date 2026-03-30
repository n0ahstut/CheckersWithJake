using System;
using System.Collections.Generic;
using UnityEngine;

public class CheckersAI
{
    int maxDepth;
    int aiPlayer;
    int opponent;

    public int nodesSearched;

    public CheckersAI(int player, int depth = 5)
    {
        aiPlayer = player;
        opponent = (player == CheckersBoard.BLACK) ? CheckersBoard.WHITE : CheckersBoard.BLACK;
        maxDepth = depth;
    }

    public Move GetBestMove(CheckersBoard board)
    {
        nodesSearched = 0;
        Move bestMove = null;
        int bestValue = int.MinValue;

        List<Move> moves = board.GetLegalMoves(aiPlayer);
        foreach (Move move in moves)
        {
            CheckersBoard clone = board.Clone();
            clone.ApplyMove(move);
            int value = MinValue(clone, maxDepth - 1, int.MinValue, int.MaxValue);
            if (value > bestValue)
            {
                bestValue = value;
                bestMove = move;
            }
        }
        return bestMove;
    }

    // Maximizing player
    int MaxValue(CheckersBoard board, int depth, int alpha, int beta)
    {
        nodesSearched++;
        if (depth == 0 || board.HasNoPieces(aiPlayer) || board.HasNoPieces(opponent)
            || board.HasNoMoves(aiPlayer))
            return Evaluate(board);

        int v = int.MinValue;
        List<Move> moves = board.GetLegalMoves(aiPlayer);
        if (moves.Count == 0) return Evaluate(board);

        foreach (Move move in moves)
        {
            CheckersBoard clone = board.Clone();
            clone.ApplyMove(move);
            v = Math.Max(v, MinValue(clone, depth - 1, alpha, beta));
            if (v >= beta) return v;
            alpha = Math.Max(alpha, v);
        }
        return v;
    }

    // Minimizing player
    int MinValue(CheckersBoard board, int depth, int alpha, int beta)
    {
        nodesSearched++;
        if (depth == 0 || board.HasNoPieces(aiPlayer) || board.HasNoPieces(opponent)
            || board.HasNoMoves(opponent))
            return Evaluate(board);

        int v = int.MaxValue;
        List<Move> moves = board.GetLegalMoves(opponent);
        if (moves.Count == 0) return Evaluate(board);

        foreach (Move move in moves)
        {
            CheckersBoard clone = board.Clone();
            clone.ApplyMove(move);
            v = Math.Min(v, MaxValue(clone, depth - 1, alpha, beta));
            if (v <= alpha) return v;
            beta = Math.Min(beta, v);
        }
        return v;
    }

    // score = material + position + mobility
    int Evaluate(CheckersBoard board)
    {
        int score = 0;

        int aiMoves = board.GetLegalMoves(aiPlayer).Count;
        int oppMoves = board.GetLegalMoves(opponent).Count;

        if (board.HasNoPieces(opponent) || oppMoves == 0) return 10000;
        if (board.HasNoPieces(aiPlayer) || aiMoves == 0) return -10000;

        for (int r = 0; r < CheckersBoard.SIZE; r++)
        {
            for (int c = 0; c < CheckersBoard.SIZE; c++)
            {
                int piece = board.grid[r, c];
                if (piece == CheckersBoard.EMPTY) continue;

                int value = 0;

                // material
                if (CheckersBoard.IsKing(piece)) value = 30;
                else value = 10;

                // center bonus
                if (c >= 2 && c <= 5 && r >= 2 && r <= 5) value += 2;

                // advancement bonus
                if (!CheckersBoard.IsKing(piece))
                {
                    if (CheckersBoard.IsBlack(piece))
                        value += r;       // higher row = closer to king
                    else
                        value += (7 - r); // lower row = closer to king
                }

                if (CheckersBoard.SameTeam(piece, aiPlayer))
                    score += value;
                else
                    score -= value;
            }
        }

        // mobility bonus
        score += (aiMoves - oppMoves) * 2;

        return score;
    }
}