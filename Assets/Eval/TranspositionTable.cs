using System;

public enum TTFlag
{
    Exact,
    LowerBound,
    UpperBound
}

public struct TTEntry
{
    public ulong hash;
    public int depth;
    public int score;
    public TTFlag flag;
    public NewBoardController.XQMove bestMove;
    public ushort generation;
    public bool occupied;
}

public class TranspositionTable
{
    private readonly TTEntry[] table;
    private readonly int mask;
    private ushort generation;

    public TranspositionTable(int sizePower = 20)
    {
        int size = 1 << sizePower;
        table = new TTEntry[size];
        mask = size - 1;
        generation = 1;
    }

    public void Clear()
    {
        Array.Clear(table, 0, table.Length);
        generation = 1;
    }

    public void NewSearch()
    {
        generation++;
        if (generation == 0)
            generation = 1;
    }

    public bool TryGetRaw(ulong hash, out TTEntry entry)
    {
        int idx = (int)(hash & (ulong)mask);
        ref TTEntry slot = ref table[idx];

        if (slot.occupied && slot.hash == hash)
        {
            entry = slot;
            return true;
        }

        entry = default;
        return false;
    }

    public bool TryGetBestMove(ulong hash, out NewBoardController.XQMove bestMove)
    {
        if (TryGetRaw(hash, out TTEntry entry))
        {
            bestMove = entry.bestMove;
            return true;
        }

        bestMove = default;
        return false;
    }

    public bool TryProbe(
        ulong hash,
        int depth,
        int ply,
        int alpha,
        int beta,
        out int score,
        out NewBoardController.XQMove bestMove)
    {
        if (TryGetRaw(hash, out TTEntry entry))
        {
            bestMove = entry.bestMove;

            if (entry.depth >= depth)
            {
                int ttScore = UnpackStoredScore(entry.score, ply);

                switch (entry.flag)
                {
                    case TTFlag.Exact:
                        score = ttScore;
                        return true;

                    case TTFlag.LowerBound:
                        if (ttScore >= beta)
                        {
                            score = ttScore;
                            return true;
                        }
                        break;

                    case TTFlag.UpperBound:
                        if (ttScore <= alpha)
                        {
                            score = ttScore;
                            return true;
                        }
                        break;
                }
            }
        }

        score = 0;
        bestMove = default;
        return false;
    }

    public void Store(
        ulong hash,
        int depth,
        int ply,
        int score,
        TTFlag flag,
        NewBoardController.XQMove bestMove)
    {
        int idx = (int)(hash & (ulong)mask);
        ref TTEntry slot = ref table[idx];

        bool replace =
            !slot.occupied ||
            slot.hash == hash ||
            slot.generation != generation ||
            depth >= slot.depth;

        if (!replace) return;

        slot.hash = hash;
        slot.depth = depth;
        slot.score = PackStoredScore(score, ply);
        slot.flag = flag;
        slot.bestMove = bestMove;
        slot.generation = generation;
        slot.occupied = true;
    }

    private static int PackStoredScore(int score, int ply)
    {
        if (score > SearchConstants.MATE_THRESHOLD)
            return score + ply;

        if (score < -SearchConstants.MATE_THRESHOLD)
            return score - ply;

        return score;
    }

    private static int UnpackStoredScore(int score, int ply)
    {
        if (score > SearchConstants.MATE_THRESHOLD)
            return score - ply;

        if (score < -SearchConstants.MATE_THRESHOLD)
            return score + ply;

        return score;
    }
}