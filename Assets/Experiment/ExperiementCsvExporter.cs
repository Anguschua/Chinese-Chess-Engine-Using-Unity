using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public class ExperimentMoveCsvRow
{
    public int round;
    public int gameIndex;
    public int ply;
    public string engine;
    public string side;
    public string move;
    public long timeMs;
    public long memoryBytes;
    public int nodes;
}

[Serializable]
public class ExperimentGameCsvRow
{
    public int round;
    public int gameIndex;
    public string redEngine;
    public string blackEngine;
    public string result;
    public string winner;
    public int plies;
}

public static class ExperimentCsvExporter
{
    public static string Export(
        List<ExperimentGameCsvRow> gameRows,
        List<ExperimentMoveCsvRow> moveRows,
        string exportFolderPath = "C:\\Users\\sko51\\Desktop\\result",
        string prefix = "xiangqi_experiment")
    {
        string folder = ResolveFolder(exportFolderPath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string gamesPath = Path.Combine(folder, $"{prefix}_games_{timestamp}.csv");
        string movesPath = Path.Combine(folder, $"{prefix}_moves_{timestamp}.csv");

        WriteGamesCsv(gamesPath, gameRows);
        WriteMovesCsv(movesPath, moveRows);

        Debug.Log($"[ExperimentCSV] Saved files:\n{gamesPath}\n{movesPath}");
        return folder;
    }

    private static string ResolveFolder(string exportFolderPath)
    {
        string folder = exportFolderPath;

        if (string.IsNullOrWhiteSpace(folder))
            folder = Path.Combine(Application.persistentDataPath, "ExperimentCSVs");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        return folder;
    }

    private static void WriteGamesCsv(string path, List<ExperimentGameCsvRow> rows)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Round,GameIndex,RedEngine,BlackEngine,Result,Winner,Plies");

        if (rows != null)
        {
            foreach (var r in rows)
            {
                sb.AppendLine(
                    $"{r.round}," +
                    $"{r.gameIndex}," +
                    $"{Esc(r.redEngine)}," +
                    $"{Esc(r.blackEngine)}," +
                    $"{Esc(r.result)}," +
                    $"{Esc(r.winner)}," +
                    $"{r.plies}"
                );
            }
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void WriteMovesCsv(string path, List<ExperimentMoveCsvRow> rows)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Round,GameIndex,Ply,Engine,Side,Move,TimeMs,MemoryBytes,Nodes");

        if (rows != null)
        {
            foreach (var r in rows)
            {
                sb.AppendLine(
                    $"{r.round}," +
                    $"{r.gameIndex}," +
                    $"{r.ply}," +
                    $"{Esc(r.engine)}," +
                    $"{Esc(r.side)}," +
                    $"{Esc(r.move)}," +
                    $"{r.timeMs}," +
                    $"{r.memoryBytes}," +
                    $"{r.nodes}"
                );
            }
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string Esc(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "\"\"";

        s = s.Replace("\"", "\"\"");
        return $"\"{s}\"";
    }
}