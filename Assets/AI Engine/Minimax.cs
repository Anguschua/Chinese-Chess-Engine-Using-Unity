using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class Minimax
{
    private static readonly TranspositionTable tt = new TranspositionTable();

    public static PolicyInference policyInference;
    public static bool usePolicyNetwork = true;
    public static bool useValueNetwork = false;
    public static bool blendPolicyWithClassicOrdering = false;
    public static float policyWeight = 0;

    private const int MAX_PLY = 256;
    private const int ASPIRATION_WINDOW = 60;
    private const int TIME_CHECK_MASK = 32;
    private const int KILLER_SLOTS = 2;

    private const int MEMORY_LIMIT_MB = 1000;

    private const int CHECKMATE_SCORE = SearchConstants.MATE_SCORE;
    private const int REPETITION_DRAW_SCORE = 0;

    private static readonly int?[,] killerMoves = new int?[MAX_PLY, KILLER_SLOTS];
    private static readonly int[,] historyHeuristic =
        new int[NewBoardController.MailboxSize, NewBoardController.MailboxSize];

    private class SearchContext
    {
        public Stopwatch stopwatch;
        public int timeLimitMs;
        public bool aborted;
        public bool abortedByMemory;
        public int nodeCount;

        public long memoryLimitBytes;
        public long memoryBaselineBytes;

        public int currentIterationBestMove;
        public int currentIterationBestScore;
        public bool hasCurrentIterationBest;
    }

    private struct ScoredMove
    {
        public int move;
        public float score;
    }

    public static EngineSearchResult FindBestMove(
        NewBoardController engine,
        Side aiSide,
        int maxDepth,
        int timeLimitMs = 1000)
    {
        Evaluator.useNN = useValueNetwork;
        ClearSearchHeuristics();
        tt.NewSearch();

        var legal = engine.GenerateLegalMoves(false);
        if (legal.Count == 0)
            return new EngineSearchResult(default, 0, 0, 0);

        long memBefore = GC.GetTotalMemory(false);

        var ctx = new SearchContext
        {
            stopwatch = Stopwatch.StartNew(),
            timeLimitMs = timeLimitMs,
            aborted = false,
            abortedByMemory = false,
            nodeCount = 0,
            memoryLimitBytes = MEMORY_LIMIT_MB > 0 ? (long)MEMORY_LIMIT_MB * 1024L * 1024L : long.MaxValue,
            memoryBaselineBytes = memBefore,
            currentIterationBestScore = int.MinValue,
            hasCurrentIterationBest = false
        };

        int stableBestMove = legal[0];
        int stableBestScore = int.MinValue;

        var rootMoves = new List<int>(legal);

        for (int depth = 3; depth <= maxDepth; depth++)
        {
            if (IsTimeUp(ctx) || IsMemoryExceeded(ctx))
                break;

            ctx.aborted = false;
            ctx.abortedByMemory = false;
            ctx.currentIterationBestScore = int.MinValue;
            ctx.currentIterationBestMove = rootMoves[0];
            ctx.hasCurrentIterationBest = false;

            OrderMoves(engine, rootMoves, stableBestMove, ply: 0);

            int alpha, beta;

            if (stableBestScore == int.MinValue)
            {
                alpha = -SearchConstants.INF;
                beta = SearchConstants.INF;
            }
            else
            {
                alpha = stableBestScore - ASPIRATION_WINDOW;
                beta = stableBestScore + ASPIRATION_WINDOW;
            }

            bool research;
            do
            {
                research = false;

                int localAlpha = alpha;
                int localBeta = beta;

                foreach (int move in rootMoves)
                {
                    if (IsTimeUp(ctx) || IsMemoryExceeded(ctx))
                    {
                        ctx.aborted = true;
                        break;
                    }

                    if (!engine.MakeMove(move))
                        continue;

                    int score;

                    if (engine.IsRepetition())
                    {
                        score = REPETITION_DRAW_SCORE;
                    }
                    else
                    {
                        score = -Negamax(
                            engine,
                            depth - 1,
                            -localBeta,
                            -localAlpha,
                            ctx,
                            ply: 1
                        );
                    }

                    engine.TakeBack();

                    if (ctx.aborted)
                        break;

                    if (score > ctx.currentIterationBestScore)
                    {
                        ctx.currentIterationBestScore = score;
                        ctx.currentIterationBestMove = move;
                        ctx.hasCurrentIterationBest = true;
                    }

                    if (score > localAlpha)
                        localAlpha = score;
                }

                if (ctx.aborted)
                    break;

                if (ctx.hasCurrentIterationBest)
                {
                    if (ctx.currentIterationBestScore <= alpha || ctx.currentIterationBestScore >= beta)
                    {
                        alpha = -SearchConstants.INF;
                        beta = SearchConstants.INF;
                        research = true;
                    }
                }
            }
            while (research && !ctx.aborted);

            if (ctx.hasCurrentIterationBest)
            {
                stableBestMove = ctx.currentIterationBestMove;
                stableBestScore = ctx.currentIterationBestScore;
            }

            if (ctx.abortedByMemory)
            {
                UnityEngine.Debug.LogWarning(
                    $"[Minimax] Memory limit exceeded ({MEMORY_LIMIT_MB} MB extra managed memory) at depth {depth}. " +
                    $"Returning best-so-far={engine.MoveToString(stableBestMove)} score={stableBestScore}"
                );
            }
            else
            {
                UnityEngine.Debug.Log(
                    $"[Minimax] Depth {depth} | best={engine.MoveToString(stableBestMove)} | score={stableBestScore} | nodes={ctx.nodeCount} | time={ctx.stopwatch.ElapsedMilliseconds}ms"
                );
            }

            if (ctx.aborted)
                break;
        }

        ctx.stopwatch.Stop();
        long memAfter = GC.GetTotalMemory(false);
        long memDelta = Math.Max(0, memAfter - memBefore);

        if (stableBestScore == int.MinValue)
            stableBestScore = 0;

        NewBoardController.XQMove bestMove = NewBoardController.ToXQMove(stableBestMove);

        if (ctx.abortedByMemory)
        {
            UnityEngine.Debug.LogWarning(
                $"[Minimax] Final best={engine.MoveToString(stableBestMove)} | score={stableBestScore} | " +
                $"time={ctx.stopwatch.ElapsedMilliseconds}ms | mem={memDelta}B | nodes={ctx.nodeCount} | abortedByMemory=true"
            );
        }
        else
        {
            UnityEngine.Debug.Log(
                $"[Minimax Final] best={engine.MoveToString(stableBestMove)} | score={stableBestScore} | time={ctx.stopwatch.ElapsedMilliseconds}ms | mem={memDelta}B | nodes={ctx.nodeCount}"
            );
        }

        return new EngineSearchResult(
            bestMove,
            ctx.stopwatch.ElapsedMilliseconds,
            memDelta,
            ctx.nodeCount
        );
    }

    private static int Negamax(
        NewBoardController engine,
        int depth,
        int alpha,
        int beta,
        SearchContext ctx,
        int ply)
    {
        ctx.nodeCount++;

        if ((ctx.nodeCount & TIME_CHECK_MASK) == 0)
        {
            if (IsTimeUp(ctx) || IsMemoryExceeded(ctx))
            {
                ctx.aborted = true;
                return EvaluateLeaf(engine);
            }
        }

        if (depth <= 0)
            return EvaluateLeaf(engine);

        int originalAlpha = alpha;
        ulong hash = engine.currentHash;

        NewBoardController.XQMove ttMove = default;
        if (tt.TryProbe(hash, depth, ply, alpha, beta, out int ttScore, out ttMove))
            return ttScore;

        var moves = engine.GenerateLegalMoves(false);

        if (moves.Count == 0)
        {
            bool inCheck = engine.IsInCheck(engine.side);
            return inCheck ? (-CHECKMATE_SCORE + ply) : 0;
        }

        int preferredMove = TryEncodeXQMove(engine, ttMove);
        OrderMoves(engine, moves, preferredMove, ply);

        int bestScore = -SearchConstants.INF;
        int bestMove = moves[0];
        bool firstMove = true;

        foreach (int move in moves)
        {
            if ((ctx.nodeCount & TIME_CHECK_MASK) == 0)
            {
                if (IsTimeUp(ctx) || IsMemoryExceeded(ctx))
                {
                    ctx.aborted = true;
                    break;
                }
            }

            if (!engine.MakeMove(move))
                continue;

            int score;

            if (engine.IsRepetition())
            {
                score = REPETITION_DRAW_SCORE;
            }
            else
            {
                if (firstMove)
                {
                    score = -Negamax(engine, depth - 1, -beta, -alpha, ctx, ply + 1);
                }
                else
                {
                    score = -Negamax(engine, depth - 1, -alpha - 1, -alpha, ctx, ply + 1);

                    if (!ctx.aborted && score > alpha && score < beta)
                    {
                        score = -Negamax(engine, depth - 1, -beta, -alpha, ctx, ply + 1);
                    }
                }
            }

            engine.TakeBack();

            if (ctx.aborted)
                break;

            firstMove = false;

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            if (score > alpha)
                alpha = score;

            if (alpha >= beta)
            {
                if (NewBoardController.GetCaptureFlag(move) == 0)
                {
                    StoreKillerMove(ply, move);
                    historyHeuristic[
                        NewBoardController.GetSourceSquare(move),
                        NewBoardController.GetTargetSquare(move)
                    ] += depth * depth;
                }
                break;
            }
        }

        if (bestScore == -SearchConstants.INF)
            return EvaluateLeaf(engine);

        if (ctx.aborted)
            return bestScore;

        TTFlag flag;
        if (bestScore <= originalAlpha)
            flag = TTFlag.UpperBound;
        else if (bestScore >= beta)
            flag = TTFlag.LowerBound;
        else
            flag = TTFlag.Exact;

        tt.Store(hash, depth, ply, bestScore, flag, NewBoardController.ToXQMove(bestMove));
        return bestScore;
    }

    private static int EvaluateLeaf(NewBoardController engine)
    {
        try
        {
            Side stm = (engine.side == NewBoardController.Red) ? Side.Red : Side.Black;
            return Evaluator.Evaluate(engine, stm, stm);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("[Minimax] Evaluator failed, fallback to 0: " + e.Message);
            return 0;
        }
    }

    private static void OrderMoves(
        NewBoardController engine,
        List<int> moves,
        int preferredMove,
        int ply)
    {
        List<ScoredMove> scored = new List<ScoredMove>(moves.Count);

        for (int i = 0; i < moves.Count; i++)
        {
            int move = moves[i];
            float score = MoveScore(move, preferredMove, ply);
            scored.Add(new ScoredMove { move = move, score = score });
        }

        scored.Sort((a, b) => b.score.CompareTo(a.score));

        for (int i = 0; i < moves.Count; i++)
            moves[i] = scored[i].move;
    }

    private static float MoveScore(
        int move,
        int preferredMove,
        int ply)
    {
        float score = 0f;

        if (move == preferredMove)
            score += 1_000_000f;

        int targetPiece = NewBoardController.GetTargetPiece(move);
        if (targetPiece != NewBoardController.Empty)
        {
            var captured = NewBoardController.ConvertPieceFromMailbox(targetPiece);
            score += 50_000f + Evaluator.GetPieceValue(captured.kind);

            int sourcePiece = NewBoardController.GetSourcePiece(move);
            var attacker = NewBoardController.ConvertPieceFromMailbox(sourcePiece);
            score += Mathf.Max(0f,
                Evaluator.GetPieceValue(captured.kind) - 0.25f * Evaluator.GetPieceValue(attacker.kind));
        }

        if (IsKillerMove(ply, move, 0))
            score += 25_000f;
        else if (IsKillerMove(ply, move, 1))
            score += 20_000f;

        score += historyHeuristic[
            NewBoardController.GetSourceSquare(move),
            NewBoardController.GetTargetSquare(move)
        ];

        return score;
    }

    private static int TryEncodeXQMove(NewBoardController engine, NewBoardController.XQMove move)
    {
        if (move.IsDefault())
            return 0;

        try
        {
            int srcSq = NewBoardController.BoardXYToMailbox(move.sx, move.sy);
            int dstSq = NewBoardController.BoardXYToMailbox(move.dx, move.dy);

            int sourcePiece = engine.board[srcSq];
            int targetPiece = engine.board[dstSq];

            if (sourcePiece == NewBoardController.Empty || sourcePiece == NewBoardController.Offboard)
                return 0;

            return NewBoardController.EncodeMove(
                srcSq,
                dstSq,
                sourcePiece,
                targetPiece,
                targetPiece != NewBoardController.Empty ? 1 : 0
            );
        }
        catch
        {
            return 0;
        }
    }

    private static bool IsTimeUp(SearchContext ctx)
    {
        return ctx.stopwatch.ElapsedMilliseconds >= ctx.timeLimitMs;
    }

    private static bool IsMemoryExceeded(SearchContext ctx)
    {
        if (ctx.memoryLimitBytes == long.MaxValue) return false;

        long current = GC.GetTotalMemory(false);
        long usedBySearch = Math.Max(0L, current - ctx.memoryBaselineBytes);

        bool exceeded = usedBySearch >= ctx.memoryLimitBytes;

        if (exceeded)
            ctx.abortedByMemory = true;

        return exceeded;
    }

    private static void ClearSearchHeuristics()
    {
        Array.Clear(killerMoves, 0, killerMoves.Length);
        Array.Clear(historyHeuristic, 0, historyHeuristic.Length);
    }

    private static void StoreKillerMove(int ply, int move)
    {
        if (ply < 0 || ply >= MAX_PLY) return;

        if (killerMoves[ply, 0] == move)
            return;

        killerMoves[ply, 1] = killerMoves[ply, 0];
        killerMoves[ply, 0] = move;
    }

    private static bool IsKillerMove(int ply, int move, int slot)
    {
        if (ply < 0 || ply >= MAX_PLY) return false;
        if (slot < 0 || slot >= KILLER_SLOTS) return false;

        return killerMoves[ply, slot].HasValue && killerMoves[ply, slot].Value == move;
    }
}