using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainController : Controller
{
    [Header("Core refs")]
    public BoardGrid boardGrid;
    public PieceSpawner pieceSpawner;
    public Camera mainCamera;

    [Header("New Engine")]
    public NewBoardController newBoardController = new NewBoardController();

    [Header("Optional UI")]
    public Text statusText;

    [Header("AI")]
    public bool aiEnabled = true;
    public Side aiSide = Side.Black;
    public int aiDepth = 10000;
    public const int TimeLimit = 2000;

    private Chess[,] boardState;
    private Chess selectedPiece;
    private Side currentTurn = Side.Red;
    private bool gameOver = false;

    private GameUIController ui;

    [Header("Replay Recording")]
    private readonly List<ReplayMoveData> currentReplayMoves = new List<ReplayMoveData>();
    private bool replaySavedForThisMatch = false;

    private readonly Stack<MoveRecord> moveHistory = new Stack<MoveRecord>();

    private struct MoveRecord
    {
        public Chess movedPiece;
        public int srcX;
        public int srcY;
        public int dstX;
        public int dstY;

        public Chess capturedPiece;
        public Side previousTurn;
        public Side movedSide;
        public PieceKind movedKind;
    }

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Start()
    {
        ui = FindObjectOfType<GameUIController>();

        if (boardGrid == null)
        {
            Debug.LogError("[MainController] boardGrid is not assigned.");
            return;
        }

        if (pieceSpawner == null)
        {
            Debug.LogError("[MainController] pieceSpawner is not assigned.");
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError("[MainController] mainCamera is not assigned.");
            return;
        }

        if (pieceSpawner.boardGrid == null)
            pieceSpawner.boardGrid = boardGrid;

        SetupModeFromSettings();
        ApplyBoardPerspective();

        pieceSpawner.SpawnInitialPosition();
        BuildBoardStateFromScene();
        SyncEngineFromMain();

        replaySavedForThisMatch = false;
        currentReplayMoves.Clear();
        moveHistory.Clear();

        RefreshUI();
        UpdateStatusText();

        if (aiEnabled && currentTurn == aiSide && !gameOver)
            StartCoroutine(AIMoveCoroutine());
    }

    void Update()
    {
        if (gameOver) return;
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleLeftClick();

        if (selectedPiece != null && Mouse.current.rightButton.wasPressedThisFrame)
            TryMoveSelectedPieceToMouse();
    }

    private void SetupModeFromSettings()
    {
        currentTurn = Side.Red;
        aiSide = Side.Black;

        if (GameSettings.I != null)
        {
            aiSide = (GameSettings.I.playerSide == Side.Black) ? Side.Red : Side.Black;

            if (ui != null)
            {
                if (GameSettings.I.mode == GameMode.PlayAI1)
                {
                    ui.SetTitleValue("Play vs Minimax AI");
                    ui.SetModeValue("AI1 (Minimax)");
                }
                else if (GameSettings.I.mode == GameMode.PlayAI2)
                {
                    ui.SetTitleValue("Play vs MCTS AI");
                    ui.SetModeValue("AI2 (MCTS)");
                }
                else if (GameSettings.I.mode == GameMode.Experiment)
                {
                    ui.SetTitleValue("Experiment");
                    ui.SetModeValue($"Experiment N = {GameSettings.I.experimentCount}");
                }
            }
        }
    }

    private void ApplyBoardPerspective()
    {
        Side perspectiveSide = Side.Red;

        if (GameSettings.I != null)
        {
            // In normal play, show the board from the player's perspective.
            // In experiment mode, keep the standard red perspective unless you want otherwise.
            if (GameSettings.I.mode == GameMode.PlayAI1 || GameSettings.I.mode == GameMode.PlayAI2)
                perspectiveSide = GameSettings.I.playerSide;
        }

        boardGrid.SetPerspective(perspectiveSide);
        boardGrid.SetCoords();

        Debug.Log($"[MainController] Board perspective = {perspectiveSide}");
    }

    private void BuildBoardStateFromScene()
    {
        boardState = new Chess[boardGrid.cols, boardGrid.rows];

        if (pieceSpawner == null || pieceSpawner.piecesParent == null)
        {
            Debug.LogError("[MainController] Missing piecesParent.");
            return;
        }

        Chess[] pieces = pieceSpawner.piecesParent.GetComponentsInChildren<Chess>(true);

        foreach (Chess p in pieces)
        {
            if (p == null) continue;
            if (!p.gameObject.activeInHierarchy) continue;

            if (p.xBoard < 0 || p.xBoard >= boardGrid.cols ||
                p.yBoard < 0 || p.yBoard >= boardGrid.rows)
                continue;

            boardState[p.xBoard, p.yBoard] = p;
        }
    }

    private void SyncEngineFromMain()
    {
        if (newBoardController == null || boardState == null) return;
        newBoardController.LoadFromSceneBoard(boardState, currentTurn);
    }

    private bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < boardGrid.cols && y >= 0 && y < boardGrid.rows;
    }

    private void HandleLeftClick()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        Vector2 point2D = new Vector2(mouseWorld.x, mouseWorld.y);

        Collider2D hit = Physics2D.OverlapPoint(point2D);
        if (hit == null)
        {
            selectedPiece = null;
            RefreshUI();
            return;
        }

        Chess piece = hit.GetComponent<Chess>();
        if (piece == null) return;

        if (piece.side != currentTurn)
        {
            Debug.Log($"Not {piece.side} turn. Current turn: {currentTurn}");
            return;
        }

        selectedPiece = piece;
        RefreshUI();
    }

    private void TryMoveSelectedPieceToMouse()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        mouseWorld.z = 0f;

        if (!boardGrid.WorldToBoard(mouseWorld, out int bx, out int by))
            return;

        TryMakeMove(selectedPiece, bx, by);
    }

    private void TryMakeMove(Chess piece, int dstX, int dstY)
    {
        if (piece == null) return;
        if (!IsInsideBoard(dstX, dstY)) return;
        if (piece.side != currentTurn) return;

        int srcX = piece.xBoard;
        int srcY = piece.yBoard;
        if (srcX == dstX && srcY == dstY) return;

        Chess target = boardState[dstX, dstY];

        if (target != null && target.side == piece.side)
        {
            selectedPiece = target;
            RefreshUI();
            return;
        }

        if (target != null && target.kind == PieceKind.King)
        {
            Debug.Log("Illegal: direct king capture is not allowed.");
            return;
        }

        if (!newBoardController.IsMoveLegalBoardCoords(srcX, srcY, dstX, dstY))
        {
            Debug.Log($"Illegal move for {piece.kind} from ({srcX},{srcY}) to ({dstX},{dstY})");
            return;
        }

        ReplayMoveData replayMove = new ReplayMoveData
        {
            srcX = srcX,
            srcY = srcY,
            dstX = dstX,
            dstY = dstY,
            movedSide = piece.side,
            movedKind = piece.kind,
            wasCapture = target != null
        };

        if (target != null)
        {
            replayMove.capturedSide = target.side;
            replayMove.capturedKind = target.kind;
        }

        ApplySceneMove(piece, target, srcX, srcY, dstX, dstY);

        moveHistory.Push(new MoveRecord
        {
            movedPiece = piece,
            srcX = srcX,
            srcY = srcY,
            dstX = dstX,
            dstY = dstY,
            capturedPiece = target,
            previousTurn = currentTurn,
            movedSide = piece.side,
            movedKind = piece.kind
        });

        currentReplayMoves.Add(replayMove);

        int encodedMove;
        if (!newBoardController.TryMakeBoardMove(srcX, srcY, dstX, dstY, out encodedMove))
        {
            Debug.LogError("[MainController] NewBoardController legality mismatch after scene move.");
            SyncEngineFromMain();
            return;
        }

        SwitchTurn();

        if (newBoardController.IsRepetition())
        {
            HandleSimpleRepetitionDraw();
            RefreshUI();
            return;
        }

        if (gameOver)
        {
            RefreshUI();
            return;
        }

        CheckForCheckmate(currentTurn);
        RefreshUI();

        if (aiEnabled && currentTurn == aiSide && !gameOver)
            StartCoroutine(AIMoveCoroutine());
    }

    private void ApplySceneMove(Chess piece, Chess target, int srcX, int srcY, int dstX, int dstY)
    {
        if (target != null)
        {
            boardState[dstX, dstY] = null;
            target.gameObject.SetActive(false);
        }

        boardState[srcX, srcY] = null;
        boardState[dstX, dstY] = piece;
        piece.MoveTo(dstX, dstY);
    }

    private void HandleSimpleRepetitionDraw()
    {
        SaveReplayIfNeeded("Draw by repetition");

        gameOver = true;
        selectedPiece = null;

        string msg = "Draw by repetition.";
        Debug.Log(msg);

        if (statusText != null)
            statusText.text = msg;

        StartCoroutine(RestartAfterDelay(3f));
    }

    private void CheckForCheckmate(Side sideToMoveNow)
    {
        if (!newBoardController.IsInCheckForSide(sideToMoveNow))
            return;

        if (!newBoardController.HasAnyLegalMoveForSide(sideToMoveNow))
            OnCheckmate(sideToMoveNow);
    }

    private void OnCheckmate(Side loser)
    {
        SaveReplayIfNeeded("Checkmate");

        gameOver = true;
        selectedPiece = null;

        Side winner = (loser == Side.Red) ? Side.Black : Side.Red;
        string msg = $"{winner} wins by checkmate!";

        Debug.Log(msg);

        if (statusText != null)
            statusText.text = msg + "\nRestarting in 3 seconds...";

        StartCoroutine(RestartAfterDelay(3f));
    }

    private void SwitchTurn()
    {
        currentTurn = (currentTurn == Side.Red) ? Side.Black : Side.Red;
        selectedPiece = null;
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText == null || gameOver) return;
        statusText.text = "Turn: " + currentTurn;
    }

    private void RefreshUI()
    {
        if (ui == null) return;

        ui.SetTurn(currentTurn);

        if (selectedPiece == null)
            ui.SetSelectedValue("(none)");
        else
            ui.SetSelectedValue($"{selectedPiece.id} @ ({selectedPiece.xBoard},{selectedPiece.yBoard})");

        ui.SetButtons(moveHistory.Count > 0);
    }

    private Chess FindLivePieceAt(int x, int y)
    {
        if (boardState == null) return null;
        if (!IsInsideBoard(x, y)) return null;

        Chess piece = boardState[x, y];
        if (piece != null &&
            piece.gameObject.activeInHierarchy &&
            piece.xBoard == x &&
            piece.yBoard == y)
        {
            return piece;
        }

        BuildBoardStateFromScene();
        return boardState[x, y];
    }

    public void UI_Undo()
    {
        StopAllCoroutines();

        if (moveHistory.Count == 0)
        {
            Debug.Log("[UI] No move to undo.");
            return;
        }

        gameOver = false;
        selectedPiece = null;

        if (aiEnabled)
        {
            UndoLastMove();
            if (moveHistory.Count > 0)
                UndoLastMove();
        }
        else
        {
            UndoLastMove();
        }

        RefreshUI();
        UpdateStatusText();
    }

    private void UndoLastMove()
    {
        if (moveHistory.Count == 0) return;

        MoveRecord record = moveHistory.Pop();

        Chess movedPiece = record.movedPiece;
        if (movedPiece == null) return;

        boardState[record.dstX, record.dstY] = null;
        boardState[record.srcX, record.srcY] = movedPiece;

        movedPiece.gameObject.SetActive(true);
        movedPiece.MoveTo(record.srcX, record.srcY);

        if (record.capturedPiece != null)
        {
            record.capturedPiece.gameObject.SetActive(true);
            record.capturedPiece.MoveTo(record.dstX, record.dstY);
            boardState[record.dstX, record.dstY] = record.capturedPiece;
        }

        if (currentReplayMoves.Count > 0)
            currentReplayMoves.RemoveAt(currentReplayMoves.Count - 1);

        currentTurn = record.previousTurn;

        SyncEngineFromMain();

        gameOver = false;
        selectedPiece = null;
    }

    public void UI_Restart()
    {
        StartCoroutine(NewGameRoutine());
    }

    public void UI_BackToMenu()
    {
        SceneManager.LoadScene(SceneNames.MainMenu, LoadSceneMode.Single);
    }

    public void NewGame()
    {
        StartCoroutine(NewGameRoutine());
    }

    private IEnumerator NewGameRoutine()
    {
        if (pieceSpawner == null || boardGrid == null) yield break;

        gameOver = false;
        selectedPiece = null;
        moveHistory.Clear();
        currentReplayMoves.Clear();
        replaySavedForThisMatch = false;

        SetupModeFromSettings();
        ApplyBoardPerspective();

        ClearAllPieces();
        yield return null;

        pieceSpawner.SpawnInitialPosition();
        yield return null;

        BuildBoardStateFromScene();
        SyncEngineFromMain();

        if (ui != null)
        {
            ui.SetTimeValue("0 ms");
            ui.SetMemoryValue("0.00 MB");
        }

        RefreshUI();
        UpdateStatusText();

        if (aiEnabled && currentTurn == aiSide && !gameOver)
            StartCoroutine(AIMoveCoroutine());
    }

    private void ClearAllPieces()
    {
        if (pieceSpawner == null || pieceSpawner.piecesParent == null) return;

        Transform parent = pieceSpawner.piecesParent;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    private void SaveReplayIfNeeded(string resultLabel)
    {
        if (replaySavedForThisMatch) return;
        if (currentReplayMoves.Count == 0) return;

        ReplayRecord replay = new ReplayRecord
        {
            replayId = Guid.NewGuid().ToString(),
            dateTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            displayName = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {resultLabel}",
            moves = new List<ReplayMoveData>(currentReplayMoves)
        };

        BattleHistoryStore.AddReplay(replay);
        replaySavedForThisMatch = true;

        Debug.Log($"[Replay] Saved replay with {replay.moves.Count} moves.");
    }

    private IEnumerator RestartAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        StartCoroutine(NewGameRoutine());
    }

    private IEnumerator AIMoveCoroutine()
    {
        yield return new WaitForSeconds(0.2f);

        if (gameOver)
            yield break;

        EngineSearchResult result;

        if (GameSettings.I != null && GameSettings.I.mode == GameMode.PlayAI2)
        {
            result = MCTS.FindBestMove(newBoardController, aiSide, iterations: 100000000, TimeLimit);
        }
        else
        {
            result = Minimax.FindBestMove(newBoardController, aiSide, aiDepth, TimeLimit);
        }

        if (ui != null)
            ui.SetAIMetrics(result.timeMs, result.memoryBytes, result.nodes);

        var best = result.bestMove;

        if (best.sx == best.dx && best.sy == best.dy)
        {
            Debug.LogWarning("[AI] Engine returned a null/default move.");
            yield break;
        }

        Chess piece = FindLivePieceAt(best.sx, best.sy);
        if (piece == null)
        {
            Debug.LogError($"[AI] No Chess object at ({best.sx},{best.sy}).");
            yield break;
        }

        if (piece.side != currentTurn)
        {
            Debug.LogError($"[AI] Piece side mismatch. piece={piece.side}, currentTurn={currentTurn}");
            yield break;
        }

        TryMakeMove(piece, best.dx, best.dy);
    }
}