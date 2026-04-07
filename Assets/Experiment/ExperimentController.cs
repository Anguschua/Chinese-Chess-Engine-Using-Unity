using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExperimentController : MonoBehaviour
{
    public enum EngineType
    {
        Minimax,
        MCTS
    }

    [Header("Core")]
    public NewBoardController newBoardController;

    [Header("UI")]
    public TMP_Text progressText;
    public TMP_Text minimaxStatsText;
    public TMP_Text mctsStatsText;
    public TMP_Text summaryText;

    [Header("Buttons")]
    public Button btnBack;

    
    //public int totalGames = GameSettings.I.getExperimentCount();
    int totalGames = 1; // Default fallback, will be overridden by GameSettings if available

    [Header("Experiment Settings")]
    public int maxPliesPerGame = 100;
    public float delayBetweenGames = 0.1f;
    public bool runOnStart = true;

    [Header("Minimax Settings")]
    public int minimaxDepth = 7;
    public int minimaxTimeLimitMs = 1000;

    [Header("MCTS Settings")]
    public int mctsIterations = 10000;
    public int mctsTimeLimitMs = 1000;

    [Header("Optional")]
    public bool logEachGameToConsole = true;
    public bool logEachMoveToConsole = false;

    private bool isRunning = false;

    private struct EngineAggregate
    {
        public int wins;
        public int losses;
        public int draws;

        public int totalMoves;
        public long totalTimeMs;
        public long totalMemoryBytes;
        public long totalNodes;

        public float AvgTimeMs => totalMoves > 0 ? (float)totalTimeMs / totalMoves : 0f;
        public float AvgMemoryMB => totalMoves > 0 ? (float)totalMemoryBytes / totalMoves / (1024f * 1024f) : 0f;
        public float AvgNodes => totalMoves > 0 ? (float)totalNodes / totalMoves : 0f;

        public int GamesPlayed => wins + losses + draws;
        public float WinRate => GamesPlayed > 0 ? (float)wins / GamesPlayed * 100f : 0f;
    }

    private struct GameOutcome
    {
        public bool finished;
        public Side? winner;
        public string resultText;
        public int plyCount;
    }

    private EngineAggregate minimaxAgg;
    private EngineAggregate mctsAgg;

    private readonly List<ExperimentGameCsvRow> experimentGameRows = new List<ExperimentGameCsvRow>();
    private readonly List<ExperimentMoveCsvRow> experimentMoveRows = new List<ExperimentMoveCsvRow>();

    private void Start()
    {
        LoadExperimentCount();
        if (btnBack != null)
            btnBack.onClick.AddListener(BackToMainMenu);

        if (newBoardController == null)
            newBoardController = new NewBoardController();

        if (runOnStart)
            StartCoroutine(RunExperimentCoroutine());
    }

    private void LoadExperimentCount()
    {
        if (GameSettings.I != null && GameSettings.I.experimentCount > 0)
        {
            totalGames = GameSettings.I.getExperimentCount();
        }
        else
        {
            totalGames = 1;
            Debug.LogWarning("[ExperimentController] Using fallback experimentCount = 1");
        }
    }
    public void BackToMainMenu()
    {
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    [ContextMenu("Run Experiment")]
    public void RunExperiment()
    {
        if (!isRunning)
            StartCoroutine(RunExperimentCoroutine());
    }

    private IEnumerator RunExperimentCoroutine()
    {
        if (isRunning)
            yield break;

        if (newBoardController == null)
        {
            Debug.LogError("[Experiment] NewBoardController is not assigned.");
            yield break;
        }

        isRunning = true;

        minimaxAgg = default;
        mctsAgg = default;
        experimentGameRows.Clear();
        experimentMoveRows.Clear();

        int roundCount = totalGames;

        if (GameSettings.I == null)
        {
            Debug.LogWarning("[Experiment] GameSettings.I is null. Using inspector fallback.");
        }
        else
        {
            Debug.Log($"[Experiment] Received experimentCount = {GameSettings.I.experimentCount}");
            if (GameSettings.I.experimentCount > 0)
                roundCount = GameSettings.I.experimentCount;
        }

        int totalPlayedGames = roundCount * 2;
        int completedGames = 0;

        UpdateAllTexts(completedGames, totalPlayedGames, $"Starting experiment...\nRounds: {roundCount}");

        for (int roundIndex = 0; roundIndex < roundCount; roundIndex++)
        {
            int roundNumber = roundIndex + 1;

            {
                EngineType redEngine = EngineType.Minimax;
                EngineType blackEngine = EngineType.MCTS;

                UpdateAllTexts(
                    completedGames,
                    totalPlayedGames,
                    $"Round {roundNumber}/{roundCount}\nGame 1/2\nRed = {redEngine}, Black = {blackEngine}"
                );

                GameOutcome outcome = RunSingleGame(roundNumber, completedGames + 1, redEngine, blackEngine);
                completedGames++;

                if (logEachGameToConsole)
                {
                    Debug.Log(
                        $"[Experiment] Round {roundNumber}/{roundCount} Game 1/2 | " +
                        $"Red={redEngine} Black={blackEngine} | Result={outcome.resultText} | Plies={outcome.plyCount}"
                    );
                }

                UpdateAllTexts(
                    completedGames,
                    totalPlayedGames,
                    $"Finished Round {roundNumber}/{roundCount} Game 1/2\nResult: {outcome.resultText}\nPlies: {outcome.plyCount}"
                );

                if (delayBetweenGames > 0f) yield return new WaitForSeconds(delayBetweenGames);
                else yield return null;
            }

            {
                EngineType redEngine = EngineType.MCTS;
                EngineType blackEngine = EngineType.Minimax;

                UpdateAllTexts(
                    completedGames,
                    totalPlayedGames,
                    $"Round {roundNumber}/{roundCount}\nGame 2/2\nRed = {redEngine}, Black = {blackEngine}"
                );

                GameOutcome outcome = RunSingleGame(roundNumber, completedGames + 1, redEngine, blackEngine);
                completedGames++;

                if (logEachGameToConsole)
                {
                    Debug.Log(
                        $"[Experiment] Round {roundNumber}/{roundCount} Game 2/2 | " +
                        $"Red={redEngine} Black={blackEngine} | Result={outcome.resultText} | Plies={outcome.plyCount}"
                    );
                }

                UpdateAllTexts(
                    completedGames,
                    totalPlayedGames,
                    $"Finished Round {roundNumber}/{roundCount} Game 2/2\nResult: {outcome.resultText}\nPlies: {outcome.plyCount}"
                );

                if (delayBetweenGames > 0f) yield return new WaitForSeconds(delayBetweenGames);
                else yield return null;
            }
        }

        string folder = ExperimentCsvExporter.Export(experimentGameRows, experimentMoveRows);
        Debug.Log($"[Experiment] CSV exported to: {folder}");

        UpdateAllTexts(completedGames, totalPlayedGames, "Experiment complete.");
        isRunning = false;
    }

    private GameOutcome RunSingleGame(int roundNumber, int gameIndex, EngineType redEngine, EngineType blackEngine)
    {
        ResetBoardToInitialPosition(newBoardController);

        int ply = 0;

        while (ply < maxPliesPerGame)
        {
            Side currentTurn = newBoardController.GetSideAsEnum();
            EngineType engineToMove = (currentTurn == Side.Red) ? redEngine : blackEngine;

            var legalMoves = newBoardController.GenerateLegalMoves(false);
            if (legalMoves.Count == 0)
            {
                bool inCheck = newBoardController.IsInCheckForSide(currentTurn);

                if (inCheck)
                {
                    Side winner = Opp(currentTurn);
                    RegisterWinLoss(winner, redEngine, blackEngine);
                    return FinishGame(
                        roundNumber,
                        gameIndex,
                        redEngine,
                        blackEngine,
                        winner,
                        $"{winner} wins by checkmate/no legal moves",
                        ply
                    );
                }

                RegisterDraw(redEngine, blackEngine);
                return FinishGame(
                    roundNumber,
                    gameIndex,
                    redEngine,
                    blackEngine,
                    null,
                    "Draw by stalemate/no legal moves",
                    ply
                );
            }

            EngineSearchResult result;

            if (engineToMove == EngineType.Minimax)
            {
                result = Minimax.FindBestMove(
                    newBoardController,
                    currentTurn,
                    minimaxDepth,
                    minimaxTimeLimitMs
                );

                minimaxAgg.totalMoves++;
                minimaxAgg.totalTimeMs += result.timeMs;
                minimaxAgg.totalMemoryBytes += result.memoryBytes;
                minimaxAgg.totalNodes += result.nodes;
            }
            else
            {
                result = MCTS.FindBestMove(
                    newBoardController,
                    currentTurn,
                    mctsIterations,
                    mctsTimeLimitMs
                );

                mctsAgg.totalMoves++;
                mctsAgg.totalTimeMs += result.timeMs;
                mctsAgg.totalMemoryBytes += result.memoryBytes;
                mctsAgg.totalNodes += result.nodes;
            }

            var best = result.bestMove;

            experimentMoveRows.Add(new ExperimentMoveCsvRow
            {
                round = roundNumber,
                gameIndex = gameIndex,
                ply = ply + 1,
                engine = engineToMove.ToString(),
                side = currentTurn.ToString(),
                move = best.ToString(),
                timeMs = result.timeMs,
                memoryBytes = result.memoryBytes,
                nodes = result.nodes
            });

            if (best.IsDefault())
            {
                bool inCheck = newBoardController.IsInCheckForSide(currentTurn);
                if (inCheck)
                {
                    Side winner = Opp(currentTurn);
                    RegisterWinLoss(winner, redEngine, blackEngine);
                    return FinishGame(
                        roundNumber,
                        gameIndex,
                        redEngine,
                        blackEngine,
                        winner,
                        $"{winner} wins (engine returned default move while checked)",
                        ply
                    );
                }

                RegisterDraw(redEngine, blackEngine);
                return FinishGame(
                    roundNumber,
                    gameIndex,
                    redEngine,
                    blackEngine,
                    null,
                    "Draw (engine returned default move)",
                    ply
                );
            }

            int encodedMove = newBoardController.EncodeBoardMoveFromCoords(best.sx, best.sy, best.dx, best.dy);
            bool moveOk = newBoardController.MakeMove(encodedMove);

            if (!moveOk)
            {
                Side winner = Opp(currentTurn);
                RegisterWinLoss(winner, redEngine, blackEngine);
                return FinishGame(
                    roundNumber,
                    gameIndex,
                    redEngine,
                    blackEngine,
                    winner,
                    $"{winner} wins (illegal engine move rejected)",
                    ply
                );
            }

            if (logEachMoveToConsole)
            {
                Debug.Log(
                    $"[Experiment] Game {gameIndex} Ply {ply + 1} | {engineToMove} ({currentTurn}) -> {best} | " +
                    $"time={result.timeMs}ms mem={result.memoryBytes}B nodes={result.nodes}"
                );
            }

            // Simple repetition rule using engine hash history
            if (newBoardController.IsRepetition())
            {
                RegisterDraw(redEngine, blackEngine);
                return FinishGame(
                    roundNumber,
                    gameIndex,
                    redEngine,
                    blackEngine,
                    null,
                    "Draw by repetition",
                    ply + 1
                );
            }

            Side nextTurn = newBoardController.GetSideAsEnum();

            if (!newBoardController.HasAnyLegalMoveForSide(nextTurn))
            {
                bool nextInCheck = newBoardController.IsInCheckForSide(nextTurn);

                if (nextInCheck)
                {
                    Side winner = Opp(nextTurn);
                    RegisterWinLoss(winner, redEngine, blackEngine);
                    return FinishGame(
                        roundNumber,
                        gameIndex,
                        redEngine,
                        blackEngine,
                        winner,
                        $"{winner} wins by checkmate",
                        ply + 1
                    );
                }

                RegisterDraw(redEngine, blackEngine);
                return FinishGame(
                    roundNumber,
                    gameIndex,
                    redEngine,
                    blackEngine,
                    null,
                    "Draw by stalemate",
                    ply + 1
                );
            }

            ply++;
        }

        RegisterDraw(redEngine, blackEngine);
        return FinishGame(
            roundNumber,
            gameIndex,
            redEngine,
            blackEngine,
            null,
            $"Draw by max plies ({maxPliesPerGame})",
            ply
        );
    }

    private GameOutcome FinishGame(
        int roundNumber,
        int gameIndex,
        EngineType redEngine,
        EngineType blackEngine,
        Side? winner,
        string resultText,
        int plyCount)
    {
        experimentGameRows.Add(new ExperimentGameCsvRow
        {
            round = roundNumber,
            gameIndex = gameIndex,
            redEngine = redEngine.ToString(),
            blackEngine = blackEngine.ToString(),
            result = resultText,
            winner = winner.HasValue ? winner.Value.ToString() : "Draw",
            plies = plyCount
        });

        return new GameOutcome
        {
            finished = true,
            winner = winner,
            resultText = resultText,
            plyCount = plyCount
        };
    }

    private void RegisterWinLoss(Side winner, EngineType redEngine, EngineType blackEngine)
    {
        EngineType winningEngine = (winner == Side.Red) ? redEngine : blackEngine;
        EngineType losingEngine = (winner == Side.Red) ? blackEngine : redEngine;

        if (winningEngine == EngineType.Minimax) minimaxAgg.wins++;
        else mctsAgg.wins++;

        if (losingEngine == EngineType.Minimax) minimaxAgg.losses++;
        else mctsAgg.losses++;
    }

    private void RegisterDraw(EngineType redEngine, EngineType blackEngine)
    {
        if (redEngine == EngineType.Minimax) minimaxAgg.draws++;
        else mctsAgg.draws++;

        if (blackEngine == EngineType.Minimax) minimaxAgg.draws++;
        else mctsAgg.draws++;
    }

    private void UpdateAllTexts(int completedGames, int totalPlayedGames, string progressMessage)
    {
        if (progressText != null)
        {
            progressText.text =
                $"Progress: {completedGames}/{totalPlayedGames}\n" +
                $"{progressMessage}";
        }

        if (minimaxStatsText != null)
        {
            minimaxStatsText.text =
                "<b>Minimax</b>\n" +
                $"Win rate: {minimaxAgg.WinRate:F1}%\n" +
                $"W / L / D: {minimaxAgg.wins} / {minimaxAgg.losses} / {minimaxAgg.draws}\n" +
                $"Avg time: {minimaxAgg.AvgTimeMs:F2} ms\n" +
                $"Avg memory: {minimaxAgg.AvgMemoryMB:F4} MB\n" +
                $"Avg nodes: {minimaxAgg.AvgNodes:F1}";
        }

        if (mctsStatsText != null)
        {
            mctsStatsText.text =
                "<b>MCTS</b>\n" +
                $"Win rate: {mctsAgg.WinRate:F1}%\n" +
                $"W / L / D: {mctsAgg.wins} / {mctsAgg.losses} / {mctsAgg.draws}\n" +
                $"Avg time: {mctsAgg.AvgTimeMs:F2} ms\n" +
                $"Avg memory: {mctsAgg.AvgMemoryMB:F4} MB\n" +
                $"Avg nodes/iters: {mctsAgg.AvgNodes:F1}";
        }

        if (summaryText != null)
        {
            int totalDrawGames = (minimaxAgg.draws + mctsAgg.draws) / 2;

            summaryText.text =
                $"Total games: {completedGames}/{totalPlayedGames}\n" +
                $"Minimax wins: {minimaxAgg.wins}\n" +
                $"MCTS wins: {mctsAgg.wins}\n" +
                $"Draw games: {totalDrawGames}";
        }
    }

    private void ResetBoardToInitialPosition(NewBoardController bc)
    {
        bc.SetStartPosition();
    }

    private static Side Opp(Side side)
    {
        return (side == Side.Red) ? Side.Black : Side.Red;
    }
}