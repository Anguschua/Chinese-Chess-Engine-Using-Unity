using UnityEngine;

[ExecuteAlways]
public class BoardGrid : MonoBehaviour
{
    [Header("Board size")]
    public int cols = 9;
    public int rows = 10;

    [Header("Grid alignment")]
    [Tooltip("World position of the bottom-left intersection in the normal Red perspective.")]
    public Vector3 origin = new Vector3(0f, 0f, 0f);

    [Tooltip("Horizontal distance between intersections.")]
    public float cellSizeX = 1f;

    [Tooltip("Vertical distance between intersections.")]
    public float cellSizeY = 1f;

    [Header("Piece offset")]
    [Tooltip("Small Z offset so pieces render above the board.")]
    public float pieceZOffset = -0.01f;

    [Header("Perspective")]
    [Tooltip("False = normal Red perspective. True = Black perspective (board visually flipped 180 degrees).")]
    public bool flippedForBlack = false;

    [Header("Debug")]
    public bool drawGizmos = true;
    public float gizmoRadius = 0.06f;
    public Color gizmoColor = Color.green;

    private Vector3[,] coords;

    void Awake()
    {
        SetCoords();
    }

    void OnEnable()
    {
        SetCoords();
    }

    void OnValidate()
    {
        SetCoords();
    }

    [ContextMenu("Refresh Grid")]
    public void RefreshGrid()
    {
        SetCoords();
    }

    public void SetPerspective(Side side)
    {
        flippedForBlack = (side == Side.Black);
        SetCoords();
    }

    public void SetCoords()
    {
        if (cols <= 0 || rows <= 0) return;

        coords = new Vector3[cols, rows];
        
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                coords[x, y] = new Vector3(
                    origin.x + x * cellSizeX,
                    origin.y + y * cellSizeY,
                    origin.z
                );
            }
        }
    }

    private void EnsureCoords()
    {
        if (coords == null || coords.GetLength(0) != cols || coords.GetLength(1) != rows)
        {
            SetCoords();
        }
    }

    private int ToViewX(int boardX)
    {
        return flippedForBlack ? (cols - 1 - boardX) : boardX;
    }

    private int ToViewY(int boardY)
    {
        return flippedForBlack ? (rows - 1 - boardY) : boardY;
    }

    private int ToBoardX(int viewX)
    {
        return flippedForBlack ? (cols - 1 - viewX) : viewX;
    }

    private int ToBoardY(int viewY)
    {
        return flippedForBlack ? (rows - 1 - viewY) : viewY;
    }

    public Vector3 GetCoord(int x, int y, bool forPiece = false)
    {
        EnsureCoords();

        if (x < 0 || x >= cols || y < 0 || y >= rows)
        {
            Debug.LogError($"[BoardGrid] GetCoord out of range: ({x}, {y})");
            return origin;
        }

        int viewX = ToViewX(x);
        int viewY = ToViewY(y);

        Vector3 pos = coords[viewX, viewY];
        if (forPiece)
            pos.z += pieceZOffset;

        return pos;
    }

    public Vector3 BoardToWorld(int x, int y, bool forPiece = false)
    {
        return GetCoord(x, y, forPiece);
    }

    public bool WorldToBoard(Vector3 worldPos, out int x, out int y)
    {
        float localX = (worldPos.x - origin.x) / cellSizeX;
        float localY = (worldPos.y - origin.y) / cellSizeY;

        int viewX = Mathf.RoundToInt(localX);
        int viewY = Mathf.RoundToInt(localY);

        if (viewX < 0 || viewX >= cols || viewY < 0 || viewY >= rows)
        {
            x = -1;
            y = -1;
            return false;
        }

        x = ToBoardX(viewX);
        y = ToBoardY(viewY);
        return true;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        if (cols <= 0 || rows <= 0) return;

        EnsureCoords();

        Gizmos.color = gizmoColor;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Gizmos.DrawSphere(coords[x, y], gizmoRadius);
            }
        }
    }
}