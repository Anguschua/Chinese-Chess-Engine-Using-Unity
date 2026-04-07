using UnityEngine;

public class PieceSpawner : MonoBehaviour
{
    [Header("Refs")]
    public GameObject piecePrefab;
    public Transform piecesParent;
    public BoardGrid boardGrid;

    [Header("Sprites")]
    public Sprite redPawn, redRook, redCannon, redAdvisor, redElephant, redHorse, redKing;
    public Sprite blackPawn, blackRook, blackCannon, blackAdvisor, blackElephant, blackHorse, blackKing;

    public void SpawnInitialPosition()
    {

        // --- Red (logical bottom) ---
        Spawn("r-rook1",   redRook,     0, 0, Side.Red,  PieceKind.Rook);
        Spawn("r-horse1",  redHorse,    1, 0, Side.Red,  PieceKind.Horse);
        Spawn("r-elep1",   redElephant, 2, 0, Side.Red,  PieceKind.Elephant);
        Spawn("r-advisor1",redAdvisor,  3, 0, Side.Red,  PieceKind.Advisor);
        Spawn("r-king",    redKing,     4, 0, Side.Red,  PieceKind.King);
        Spawn("r-advisor2",redAdvisor,  5, 0, Side.Red,  PieceKind.Advisor);
        Spawn("r-elep2",   redElephant, 6, 0, Side.Red,  PieceKind.Elephant);
        Spawn("r-horse2",  redHorse,    7, 0, Side.Red,  PieceKind.Horse);
        Spawn("r-rook2",   redRook,     8, 0, Side.Red,  PieceKind.Rook);

        Spawn("r-cannon1", redCannon,   1, 2, Side.Red,  PieceKind.Cannon);
        Spawn("r-cannon2", redCannon,   7, 2, Side.Red,  PieceKind.Cannon);

        Spawn("r-pawn1",   redPawn,     0, 3, Side.Red,  PieceKind.Pawn);
        Spawn("r-pawn2",   redPawn,     2, 3, Side.Red,  PieceKind.Pawn);
        Spawn("r-pawn3",   redPawn,     4, 3, Side.Red,  PieceKind.Pawn);
        Spawn("r-pawn4",   redPawn,     6, 3, Side.Red,  PieceKind.Pawn);
        Spawn("r-pawn5",   redPawn,     8, 3, Side.Red,  PieceKind.Pawn);

        // --- Black (logical top) ---
        Spawn("b-rook1",   blackRook,     0, 9, Side.Black, PieceKind.Rook);
        Spawn("b-horse1",  blackHorse,    1, 9, Side.Black, PieceKind.Horse);
        Spawn("b-elep1",   blackElephant, 2, 9, Side.Black, PieceKind.Elephant);
        Spawn("b-advisor1",blackAdvisor,  3, 9, Side.Black, PieceKind.Advisor);
        Spawn("b-king",    blackKing,     4, 9, Side.Black, PieceKind.King);
        Spawn("b-advisor2",blackAdvisor,  5, 9, Side.Black, PieceKind.Advisor);
        Spawn("b-elep2",   blackElephant, 6, 9, Side.Black, PieceKind.Elephant);
        Spawn("b-horse2",  blackHorse,    7, 9, Side.Black, PieceKind.Horse);
        Spawn("b-rook2",   blackRook,     8, 9, Side.Black, PieceKind.Rook);

        Spawn("b-cannon1", blackCannon,   1, 7, Side.Black, PieceKind.Cannon);
        Spawn("b-cannon2", blackCannon,   7, 7, Side.Black, PieceKind.Cannon);

        Spawn("b-pawn1",   blackPawn,     0, 6, Side.Black, PieceKind.Pawn);
        Spawn("b-pawn2",   blackPawn,     2, 6, Side.Black, PieceKind.Pawn);
        Spawn("b-pawn3",   blackPawn,     4, 6, Side.Black, PieceKind.Pawn);
        Spawn("b-pawn4",   blackPawn,     6, 6, Side.Black, PieceKind.Pawn);
        Spawn("b-pawn5",   blackPawn,     8, 6, Side.Black, PieceKind.Pawn);
    }

    void Spawn(string name, Sprite sprite, int x, int y, Side side, PieceKind kind)
    {
        GameObject go = Instantiate(piecePrefab, piecesParent);
        go.name = name;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = sprite;
            sr.sortingLayerName = "Pieces";
            sr.sortingOrder = 10;
            sr.color = Color.white;
        }

        var chess = go.GetComponent<Chess>();
        chess.side = side;
        chess.kind = kind;
        chess.Init(x, y, name, sprite, boardGrid);
    }
}