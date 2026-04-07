using System.Collections.Generic;

public static class BattleHistoryStore
{
    private const int MaxRecords = 10;
    private static readonly List<ReplayRecord> recentReplays = new List<ReplayRecord>();

    public static void AddReplay(ReplayRecord replay)
    {
        if (replay == null) return;

        recentReplays.Insert(0, replay);

        if (recentReplays.Count > MaxRecords)
        {
            recentReplays.RemoveAt(recentReplays.Count - 1);
        }
    }

    public static ReplayRecord GetReplayAt(int index)
    {
        if (index < 0 || index >= recentReplays.Count) return null;
        return recentReplays[index];
    }

    public static List<ReplayRecord> GetAll()
    {
        return recentReplays;
    }

    public static int Count
    {
        get { return recentReplays.Count; }
    }

    public static void Clear()
    {
        recentReplays.Clear();
    }
}