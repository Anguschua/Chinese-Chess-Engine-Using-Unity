using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class MCTS
{
    public static PolicyInference policyInference;
    public static bool usePolicyNetwork = true;
    public static bool useValueNetwork = false;

    private const double CPUCT = 1.25;
    private const double PW_C = 1.8;
    private const double PW_ALPHA = 0.5;

    private const int MEMORY_LIMIT_MB = 1000;
    private const int MEMORY_CHECK_MASK = 31;

    private class Node
    {
        public Node parent;
        public readonly List<Node> children = new List<Node>();

        public int move;
        public int sideToMove;
        public float prior;

        public List<int> legalMoves;
        public List<float> priors;
        public int[] expansionOrder;
        public int nextExpandIndex;
        public bool initialized;

        public int visits;
        public double valueSum;

        public Node(Node parent, int move, int sideToMove, float prior = 1f)
        {
            this.parent = parent;
            this.move = move;
            this.sideToMove = sideToMove;
            this.prior = prior;

            legalMoves = null;
            priors = null;
            expansionOrder = null;
            nextExpandIndex = 0;
            initialized = false;

            visits = 0;
            valueSum = 0.0;
        }

        public double MeanValue => visits == 0 ? 0.5 : valueSum / visits;
    }

    public static EngineSearchResult FindBestMove(
        NewBoardController engine,
        Side aiSide,
        int iterations = 5000,
        int timeLimitMs = 1800)
    {
        Evaluator.useNN = useValueNetwork;

        long memBefore = GC.GetTotalMemory(false);
        long memoryLimitBytes = MEMORY_LIMIT_MB > 0 ? (long)MEMORY_LIMIT_MB * 1024L * 1024L : long.MaxValue;
        var sw = Stopwatch.StartNew();

        int aiColor = aiSide == Side.Red ? NewBoardController.Red : NewBoardController.Black;

        var traversalBoard = new NewBoardController();
        CopyEngine(engine, traversalBoard);

        var root = new Node(parent: null, move: 0, sideToMove: traversalBoard.side, prior: 1f);
        EnsureInitialized(root, traversalBoard);

        int completedIterations = 0;
        bool abortedByMemory = false;

        if (root.legalMoves.Count == 0)
        {
            sw.Stop();
            long memAfter0 = GC.GetTotalMemory(false);
            return new EngineSearchResult(default, sw.ElapsedMilliseconds, Math.Max(0, memAfter0 - memBefore), 0);
        }

        var appliedMoves = new List<int>(128);

        while (completedIterations < iterations && sw.ElapsedMilliseconds < timeLimitMs)
        {
            if ((completedIterations & MEMORY_CHECK_MASK) == 0 &&
                IsMemoryExceeded(memoryLimitBytes, memBefore))
            {
                abortedByMemory = true;
                break;
            }

            appliedMoves.Clear();
            Node node = root;

            try
            {
                while (true)
                {
                    EnsureInitialized(node, traversalBoard);

                    if (CanExpandMore(node))
                        break;

                    if (node.children.Count == 0)
                        break;

                    Node child = SelectChild(node, aiColor);
                    if (!traversalBoard.MakeMove(child.move))
                        break;

                    appliedMoves.Add(child.move);
                    node = child;
                }

                EnsureInitialized(node, traversalBoard);
                if (CanExpandMore(node))
                {
                    node = Expand(node, traversalBoard, appliedMoves);
                }

                double value = EvaluateLeaf(traversalBoard, node, aiSide, aiColor);
                Backpropagate(node, value);

                completedIterations++;
            }
            finally
            {
                for (int i = appliedMoves.Count - 1; i >= 0; i--)
                    traversalBoard.TakeBack();
            }
        }

        Node bestChild = null;
        int bestVisits = -1;

        foreach (var child in root.children)
        {
            if (child.visits > bestVisits)
            {
                bestVisits = child.visits;
                bestChild = child;
            }
        }

        sw.Stop();
        long memAfter = GC.GetTotalMemory(false);
        long memDelta = Math.Max(0, memAfter - memBefore);

        if (bestChild == null)
        {
            if (abortedByMemory)
            {
                UnityEngine.Debug.LogWarning(
                    $"[MCTS] Memory limit exceeded ({MEMORY_LIMIT_MB} MB extra managed memory). Returning default move."
                );
            }

            return new EngineSearchResult(default, sw.ElapsedMilliseconds, memDelta, completedIterations);
        }

        var bestMove = NewBoardController.ToXQMove(bestChild.move);

        if (abortedByMemory)
        {
            UnityEngine.Debug.LogWarning(
                $"[MCTS] Memory limit exceeded ({MEMORY_LIMIT_MB} MB extra managed memory). Returning best-so-far: " +
                $"{engine.MoveToString(bestChild.move)} | visits={bestVisits} | time={sw.ElapsedMilliseconds}ms | mem={memDelta}B"
            );
        }
        else
        {
            UnityEngine.Debug.Log(
                $"[MCTS] best={engine.MoveToString(bestChild.move)}, visits={bestVisits}, q={bestChild.MeanValue:F3}, children={root.children.Count}, time={sw.ElapsedMilliseconds}ms, mem={memDelta}B, iters={completedIterations}"
            );
        }

        return new EngineSearchResult(bestMove, sw.ElapsedMilliseconds, memDelta, completedIterations);
    }

    private static bool IsMemoryExceeded(long memoryLimitBytes, long memoryBaselineBytes)
    {
        if (memoryLimitBytes == long.MaxValue) return false;

        long current = GC.GetTotalMemory(false);
        long usedBySearch = Math.Max(0L, current - memoryBaselineBytes);

        return usedBySearch >= memoryLimitBytes;
    }

    private static void CopyEngine(NewBoardController src, NewBoardController dst)
    {
        dst.ResetBoard();

        for (int i = 0; i < src.board.Length; i++)
            dst.board[i] = src.board[i];

        dst.side = src.side;
        dst.kingSquare[NewBoardController.Red] = src.kingSquare[NewBoardController.Red];
        dst.kingSquare[NewBoardController.Black] = src.kingSquare[NewBoardController.Black];
        dst.currentHash = src.currentHash;
        dst.ply = src.ply;
    }

    private static void EnsureInitialized(Node node, NewBoardController board)
    {
        if (node.initialized) return;

        node.legalMoves = board.GenerateLegalMoves(false);
        node.priors = BuildPriors(board, node.legalMoves);
        node.expansionOrder = BuildExpansionOrder(node.priors);
        node.nextExpandIndex = 0;
        node.initialized = true;
    }

    private static int MaxChildrenAllowed(Node node)
    {
        int allowed = Mathf.Max(
            1,
            Mathf.FloorToInt((float)(PW_C * Math.Pow(Math.Max(1, node.visits), PW_ALPHA)))
        );

        return Mathf.Min(allowed, node.legalMoves.Count);
    }

    private static bool CanExpandMore(Node node)
    {
        return node.children.Count < MaxChildrenAllowed(node) &&
               node.nextExpandIndex < node.expansionOrder.Length;
    }

    private static Node SelectChild(Node node, int aiColor)
    {
        Node best = null;
        double bestScore = double.MinValue;

        double sqrtParentVisits = Math.Sqrt(Math.Max(1, node.visits));

        foreach (var child in node.children)
        {
            double q = (node.sideToMove == aiColor) ? child.MeanValue : (1.0 - child.MeanValue);
            double u = CPUCT * child.prior * sqrtParentVisits / (1.0 + child.visits);
            double score = q + u;

            if (score > bestScore)
            {
                bestScore = score;
                best = child;
            }
        }

        return best;
    }

    private static Node Expand(Node node, NewBoardController board, List<int> appliedMoves)
    {
        int moveIndex = node.expansionOrder[node.nextExpandIndex++];
        int move = node.legalMoves[moveIndex];
        float prior = node.priors[moveIndex];

        if (!board.MakeMove(move))
            return node;

        appliedMoves.Add(move);

        var child = new Node(
            parent: node,
            move: move,
            sideToMove: board.side,
            prior: prior
        );

        node.children.Add(child);
        return child;
    }

    private static double EvaluateLeaf(NewBoardController board, Node node, Side aiSide, int aiColor)
    {
        EnsureInitialized(node, board);

        if (node.legalMoves.Count == 0)
        {
            if (board.IsInCheck(board.side))
            {
                int winner = board.side ^ 1;
                return winner == aiColor ? 1.0 : 0.0;
            }

            return 0.5;
        }

        if (board.IsRepetition())
            return 0.5;

        try
        {
            Side stm = (board.side == NewBoardController.Red) ? Side.Red : Side.Black;
            int eval = Evaluator.Evaluate(board, stm, aiSide);

            double x = Math.Max(-4.0, Math.Min(4.0, eval / 700.0));
            return 1.0 / (1.0 + Math.Exp(-x));
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("[MCTS] Evaluator failed, fallback to 0.5: " + e.Message);
            return 0.5;
        }
    }

    private static void Backpropagate(Node node, double valueForRootSide)
    {
        while (node != null)
        {
            node.visits++;
            node.valueSum += valueForRootSide;
            node = node.parent;
        }
    }

    private static List<float> BuildPriors(NewBoardController board, List<int> legalMoves)
    {
        if (legalMoves == null || legalMoves.Count == 0)
            return new List<float>();

        var priors = new List<float>(legalMoves.Count);
        float sum = 0f;

        for (int i = 0; i < legalMoves.Count; i++)
        {
            float p = Mathf.Max(0.001f, HandcraftedPrior(board, legalMoves[i]));
            priors.Add(p);
            sum += p;
        }

        if (sum <= 0f)
            return BuildUniformPriors(legalMoves.Count);

        for (int i = 0; i < priors.Count; i++)
            priors[i] /= sum;

        return priors;
    }

    private static float HandcraftedPrior(NewBoardController board, int move)
    {
        float score = 1f;

        int srcSq = NewBoardController.GetSourceSquare(move);
        int dstSq = NewBoardController.GetTargetSquare(move);
        int srcPiece = NewBoardController.GetSourcePiece(move);
        int targetPiece = NewBoardController.GetTargetPiece(move);

        PieceKind movingKind = NewBoardController.ConvertPieceFromMailbox(srcPiece).kind;

        if (targetPiece != NewBoardController.Empty)
        {
            PieceKind capturedKind = NewBoardController.ConvertPieceFromMailbox(targetPiece).kind;
            score += 6f + 0.02f * Evaluator.GetPieceValue(capturedKind);

            score += Mathf.Max(0f,
                (Evaluator.GetPieceValue(capturedKind) - 0.25f * Evaluator.GetPieceValue(movingKind)));
        }

        if (NewBoardController.MailboxToBoardXY(srcSq, out int sx, out int sy) &&
            NewBoardController.MailboxToBoardXY(dstSq, out int dx, out int dy))
        {
            if (movingKind == PieceKind.Pawn)
            {
                if (board.side == NewBoardController.Red && dy > sy) score += 5f;
                if (board.side == NewBoardController.Black && dy < sy) score += 5f;
            }

            if (movingKind == PieceKind.Horse || movingKind == PieceKind.Cannon || movingKind == PieceKind.Rook)
            {
                float before = Mathf.Abs(sx - 4) + Mathf.Abs(sy - 4.5f);
                float after = Mathf.Abs(dx - 4) + Mathf.Abs(dy - 4.5f);
                if (after < before) score += 2f;
            }
        }

        return score;
    }

    private static List<float> BuildUniformPriors(int count)
    {
        var priors = new List<float>(count);
        if (count <= 0) return priors;

        float p = 1f / count;
        for (int i = 0; i < count; i++)
            priors.Add(p);

        return priors;
    }

    private static int[] BuildExpansionOrder(List<float> priors)
    {
        int n = priors.Count;
        int[] order = new int[n];
        for (int i = 0; i < n; i++)
            order[i] = i;

        Array.Sort(order, (a, b) => priors[b].CompareTo(priors[a]));
        return order;
    }
}