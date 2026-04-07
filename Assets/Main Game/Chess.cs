using UnityEngine;

public enum Side
{
    Red,
    Black
}

public enum PieceKind
{
    Rook,
    Horse,
    Elephant,
    Advisor,
    King,
    Cannon,
    Pawn
}

[RequireComponent(typeof(SpriteRenderer))]
public class Chess : MonoBehaviour
{
    public int xBoard;
    public int yBoard;
    public string id;
    public Sprite pieceSprite;

    public Side side;
    public PieceKind kind;

    private SpriteRenderer sr;
    private BoardGrid grid;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(int x, int y, string name, Sprite sprite, BoardGrid boardGrid)
    {
        xBoard      = x;
        yBoard      = y;
        id          = name;
        pieceSprite = sprite;
        grid        = boardGrid;

        sr.sprite   = pieceSprite;
        SnapToGrid();
    }

    public void SnapToGrid()
    {
        if (grid == null) return;
        Vector3 worldPos = grid.GetCoord(xBoard, yBoard, forPiece: true);
        transform.position = worldPos;
    }

    public void MoveTo(int x, int y)
    {
        xBoard = x;
        yBoard = y;
        SnapToGrid();
    }
}
