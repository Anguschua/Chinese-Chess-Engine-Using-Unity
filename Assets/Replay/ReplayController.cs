using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ReplayController : MonoBehaviour
{
    [Header("Core refs")]
    public BoardGrid boardGrid;
    public PieceSpawner pieceSpawner;
    public Camera mainCamera;

    [Header("UI")]
    public Button applyMoveButton;
    public Button undoMoveButton;
    public Button btnBack;
    public TMP_Text titleText;
    public TMP_Text moveIndexText;
    public TMP_Text statusText;

    private Chess[,] boardState;
    private ReplayRecord replay;
    private int currentMoveIndex = 0;

    private struct AppliedReplayMove
    {
        public Chess movedPiece;
        public int srcX;
        public int srcY;
        public int dstX;
        public int dstY;
        public Chess capturedPiece;
    }

    private Stack<AppliedReplayMove> appliedMoves = new Stack<AppliedReplayMove>();

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        replay = ReplaySelectionStore.SelectedReplay;

        if (replay == null)
        {
            Debug.LogWarning("[ReplayController] No replay selected.");
            SceneManager.LoadScene(SceneNames.ReplayList, LoadSceneMode.Single);
            return;
        }

        if (titleText != null)
            titleText.text = replay.displayName;

        StartCoroutine(SetupReplayRoutine());

        if (applyMoveButton != null)
        {
            applyMoveButton.onClick.RemoveAllListeners();
            applyMoveButton.onClick.AddListener(ApplyNextMove);
        }

        if (undoMoveButton != null)
        {
            undoMoveButton.onClick.RemoveAllListeners();
            undoMoveButton.onClick.AddListener(UndoLastReplayMove);
        }
        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(BackToMainMenu);
        }
            
    }

    IEnumerator SetupReplayRoutine()
    {
        if (boardGrid == null || pieceSpawner == null)
        {
            Debug.LogError("[ReplayController] Missing boardGrid or pieceSpawner.");
            yield break;
        }

        boardGrid.SetCoords();

        ClearAllPieces();
        yield return null;

        pieceSpawner.SpawnInitialPosition();
        yield return null;

        BuildBoardStateFromScene();

        currentMoveIndex = 0;
        appliedMoves.Clear();

        UpdateUI();
    }

    void ClearAllPieces()
    {
        if (pieceSpawner == null || pieceSpawner.piecesParent == null) return;

        Transform parent = pieceSpawner.piecesParent;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    void BuildBoardStateFromScene()
    {
        if (boardGrid == null || pieceSpawner == null || pieceSpawner.piecesParent == null)
        {
            Debug.LogError("[ReplayController] Cannot build board state.");
            return;
        }

        boardState = new Chess[boardGrid.cols, boardGrid.rows];

        Chess[] pieces = pieceSpawner.piecesParent.GetComponentsInChildren<Chess>(true);

        foreach (var p in pieces)
        {
            if (p == null) continue;
            if (!p.gameObject.activeInHierarchy) continue;

            if (p.xBoard < 0 || p.xBoard >= boardGrid.cols ||
                p.yBoard < 0 || p.yBoard >= boardGrid.rows)
                continue;

            boardState[p.xBoard, p.yBoard] = p;
        }
    }

    Chess FindPieceAt(int x, int y)
    {
        if (boardState == null) return null;
        if (x < 0 || x >= boardGrid.cols || y < 0 || y >= boardGrid.rows) return null;

        Chess piece = boardState[x, y];
        if (piece != null && piece.gameObject.activeInHierarchy)
            return piece;

        return null;
    }

    public void ApplyNextMove()
    {
        if (replay == null) return;
        if (currentMoveIndex >= replay.moves.Count) return;

        ReplayMoveData move = replay.moves[currentMoveIndex];

        Chess movedPiece = FindPieceAt(move.srcX, move.srcY);
        if (movedPiece == null)
        {
            Debug.LogError($"[Replay] No piece found at source ({move.srcX},{move.srcY})");
            return;
        }

        Chess capturedPiece = FindPieceAt(move.dstX, move.dstY);

        if (capturedPiece != null && capturedPiece.side == movedPiece.side)
        {
            Debug.LogError("[Replay] Invalid replay state: destination occupied by same side.");
            return;
        }

        if (capturedPiece != null)
        {
            boardState[move.dstX, move.dstY] = null;
            capturedPiece.gameObject.SetActive(false);
        }

        boardState[move.srcX, move.srcY] = null;
        boardState[move.dstX, move.dstY] = movedPiece;
        movedPiece.MoveTo(move.dstX, move.dstY);

        AppliedReplayMove applied = new AppliedReplayMove
        {
            movedPiece = movedPiece,
            srcX = move.srcX,
            srcY = move.srcY,
            dstX = move.dstX,
            dstY = move.dstY,
            capturedPiece = capturedPiece
        };

        appliedMoves.Push(applied);
        currentMoveIndex++;

        UpdateUI();
    }

    public void UndoLastReplayMove()
    {
        if (appliedMoves.Count == 0) return;

        AppliedReplayMove applied = appliedMoves.Pop();

        boardState[applied.dstX, applied.dstY] = null;

        applied.movedPiece.gameObject.SetActive(true);
        applied.movedPiece.MoveTo(applied.srcX, applied.srcY);
        boardState[applied.srcX, applied.srcY] = applied.movedPiece;

        if (applied.capturedPiece != null)
        {
            applied.capturedPiece.gameObject.SetActive(true);
            applied.capturedPiece.MoveTo(applied.dstX, applied.dstY);
            boardState[applied.dstX, applied.dstY] = applied.capturedPiece;
        }

        currentMoveIndex--;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (moveIndexText != null)
        {
            int total = (replay != null) ? replay.moves.Count : 0;
            moveIndexText.text = $"Move: {currentMoveIndex}/{total}";
        }

        if (applyMoveButton != null)
            applyMoveButton.interactable = replay != null && currentMoveIndex < replay.moves.Count;

        if (undoMoveButton != null)
            undoMoveButton.interactable = currentMoveIndex > 0;

        if (statusText != null)
            statusText.text = "Replay Mode";
    }

    public void BackToReplayList()
    {
        SceneManager.LoadScene(SceneNames.ReplayList, LoadSceneMode.Single);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(SceneNames.MainMenu, LoadSceneMode.Single);
    }
}