using System;
public static class MoveEncoding
{
    public const int BoardSquares = 90;
    public const int PolicySize = BoardSquares * BoardSquares; // 8100

    public static int ToBoardIndex(int x, int y)
    {
        return y * 9 + x;
    }

    public static int ToMoveId(int sx, int sy, int dx, int dy)
    {
        int fromIdx = ToBoardIndex(sx, sy);
        int toIdx = ToBoardIndex(dx, dy);
        return fromIdx * BoardSquares + toIdx;
    }

    public static int ToMoveId(NewBoardController.XQMove move)
    {
        return ToMoveId(move.sx, move.sy, move.dx, move.dy);
    }

    public static int ToMoveId(int encodedMove)
    {
        int srcSq = NewBoardController.GetSourceSquare(encodedMove);
        int dstSq = NewBoardController.GetTargetSquare(encodedMove);

        if (!NewBoardController.MailboxToBoardXY(srcSq, out int sx, out int sy))
            throw new Exception($"Invalid source mailbox square: {srcSq}");

        if (!NewBoardController.MailboxToBoardXY(dstSq, out int dx, out int dy))
            throw new Exception($"Invalid target mailbox square: {dstSq}");

        return ToMoveId(sx, sy, dx, dy);
    }

    public static int GetSourceIndex(int moveId)
    {
        return moveId / BoardSquares;
    }

    public static int GetTargetIndex(int moveId)
    {
        return moveId % BoardSquares;
    }

    public static void DecodeMoveId(int moveId, out int sx, out int sy, out int dx, out int dy)
    {
        int fromIdx = GetSourceIndex(moveId);
        int toIdx = GetTargetIndex(moveId);

        sx = fromIdx % 9;
        sy = fromIdx / 9;
        dx = toIdx % 9;
        dy = toIdx / 9;
    }

    public static bool TryGetEncodedMove(NewBoardController board, int moveId, out int encodedMove)
    {
        encodedMove = 0;

        DecodeMoveId(moveId, out int sx, out int sy, out int dx, out int dy);

        int srcSq = NewBoardController.BoardXYToMailbox(sx, sy);
        int dstSq = NewBoardController.BoardXYToMailbox(dx, dy);

        int sourcePiece = board.board[srcSq];
        int targetPiece = board.board[dstSq];

        if (sourcePiece == NewBoardController.Empty || sourcePiece == NewBoardController.Offboard)
            return false;

        int capture = (targetPiece != NewBoardController.Empty && targetPiece != NewBoardController.Offboard) ? 1 : 0;

        encodedMove = NewBoardController.EncodeMove(
            srcSq,
            dstSq,
            sourcePiece,
            targetPiece,
            capture
        );

        return true;
    }
}