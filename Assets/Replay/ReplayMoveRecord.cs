using System;

[Serializable]
public class ReplayMoveData
{
    public int srcX;
    public int srcY;
    public int dstX;
    public int dstY;

    public Side movedSide;
    public PieceKind movedKind;

    public bool wasCapture;
    public Side capturedSide;
    public PieceKind capturedKind;
}