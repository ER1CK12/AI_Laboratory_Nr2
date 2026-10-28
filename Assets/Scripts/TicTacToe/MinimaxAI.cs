using System;
using System.Diagnostics;

namespace TicTacToe5
{
    public sealed class MinimaxAI
    {
        public const float WinScore  = 10f;
        public const float LossScore = -10f;
        public const float DrawScore = 0f;
        public const float PlyPenalty = 0.1f;
        public const float DecisiveThreshold = 7f;
        readonly int me;
        readonly int opp;
        Board board;
        long nodes;
        long deadlineMs;
        bool aborted;
        bool canAbort;
        readonly Stopwatch clock = new Stopwatch();
        int[][] moveBuffer;
        float[][] prioBuffer;

        public MinimaxAI(int aiPlayer)
        {
            me = aiPlayer;
            opp = Player.Opponent(aiPlayer);
        }

        public SearchResult FindBestMove(Board position, int maxDepth, int timeLimitMs)
        {
            board = position.Clone();
            int n = board.CellCount;
            moveBuffer = new int[n + 2][];
            prioBuffer = new float[n + 2][];
            for (int i = 0; i < n + 2; i++) { moveBuffer[i] = new int[n]; prioBuffer[i] = new float[n]; }

            nodes = 0;
            aborted = false;
            deadlineMs = timeLimitMs;
            clock.Restart();

            var result = new SearchResult();
            if (board.EmptyCount == 0) return result;

            int rootCount = OrderMoves(0, me);
            var rootMoves = new int[rootCount];
            Array.Copy(moveBuffer[0], rootMoves, rootCount);
            int limit = Math.Min(maxDepth, board.EmptyCount);
            for (int depth = 1; depth <= limit; depth++)
            {
                var scores = new float[rootCount];
                var upper = new bool[rootCount];
                canAbort = depth > 1;
                float alpha = float.NegativeInfinity;
                float bestScore = float.NegativeInfinity;
                int bestMove = -1;

                for (int i = 0; i < rootCount; i++)
                {
                    int m = rootMoves[i];
                    float s = ScoreMove(m, me, depth, 1, alpha, float.PositiveInfinity);
                    if (aborted) break;

                    scores[i] = s;
                    upper[i] = i > 0 && s <= alpha;
                    if (s > bestScore) { bestScore = s; bestMove = m; }
                    if (s > alpha) alpha = s;
                }

                if (aborted && depth > 1) break;

                result.BestMove = bestMove;
                result.BestScore = bestScore;
                result.DepthReached = depth;
                result.SolvedExactly = depth >= board.EmptyCount;
                result.RootScores = new float[n];
                result.RootIsUpperBound = new bool[n];
                for (int i = 0; i < n; i++) result.RootScores[i] = float.NaN;
                for (int i = 0; i < rootCount; i++)
                {
                    result.RootScores[rootMoves[i]] = scores[i];
                    result.RootIsUpperBound[rootMoves[i]] = upper[i];
                }

                var keys = new float[rootCount];
                for (int i = 0; i < rootCount; i++) keys[i] = -scores[i];
                Array.Sort(keys, rootMoves);

                if (Math.Abs(bestScore) >= DecisiveThreshold) break;
            }

            result.Nodes = nodes;
            result.Milliseconds = clock.Elapsed.TotalMilliseconds;
            return result;
        }

        float ScoreMove(int m, int player, int depth, int ply, float alpha, float beta)
        {
            bool win = board.Place(m, player);
            float s;
            if (win)
                s = player == me ? WinScore - PlyPenalty * ply : LossScore + PlyPenalty * ply;
            else if (board.EmptyCount == 0)
                s = DrawScore;
            else if (player == me)
                s = MinValue(depth - 1, ply + 1, alpha, beta);
            else
                s = MaxValue(depth - 1, ply + 1, alpha, beta);
            board.Undo(m);
            return s;
        }

        float MaxValue(int depth, int ply, float alpha, float beta)
        {
            if (TimeUp()) return 0f;
            if (depth == 0) return board.Evaluate(me);

            float best = float.NegativeInfinity;
            int count = OrderMoves(ply, me);
            int[] moves = moveBuffer[ply];
            for (int i = 0; i < count; i++)
            {
                float s = ScoreMove(moves[i], me, depth, ply, alpha, beta);
                if (s > best) best = s;
                if (best > alpha) alpha = best;
                if (alpha >= beta) break;
            }
            return best;
        }

        float MinValue(int depth, int ply, float alpha, float beta)
        {
            if (TimeUp()) return 0f;
            if (depth == 0) return board.Evaluate(me);

            float best = float.PositiveInfinity;
            int count = OrderMoves(ply, opp);
            int[] moves = moveBuffer[ply];
            for (int i = 0; i < count; i++)
            {
                float s = ScoreMove(moves[i], opp, depth, ply, alpha, beta);
                if (s < best) best = s;
                if (best < beta) beta = best;
                if (alpha >= beta) break;
            }
            return best;
        }

        bool TimeUp()
        {
            nodes++;
            if (aborted) return true;
            if (canAbort && (nodes & 1023) == 0 && clock.ElapsedMilliseconds > deadlineMs) aborted = true;
            return aborted;
        }

        int OrderMoves(int ply, int mover)
        {
            int other = Player.Opponent(mover);
            int k = board.WinLength;
            int[] moves = moveBuffer[ply];
            float[] prio = prioBuffer[ply];
            int count = 0;

            for (int cell = 0; cell < board.CellCount; cell++)
            {
                if (board.Cells[cell] != Player.None) continue;
                float p = 0f;
                foreach (int w in board.CellWindows[cell])
                {
                    int mine = board.Count(w, mover), theirs = board.Count(w, other);
                    if (theirs == 0) p += 1f + board.LineWeight[mine] * 2f + (mine == k - 1 ? 1e6f : 0f);
                    if (mine == 0)   p += board.LineWeight[theirs] + (theirs == k - 1 ? 1e5f : 0f);
                }

                int j = count++;
                while (j > 0 && prio[j - 1] < p) { prio[j] = prio[j - 1]; moves[j] = moves[j - 1]; j--; }
                prio[j] = p; moves[j] = cell;
            }
            return count;
        }
    }
}
