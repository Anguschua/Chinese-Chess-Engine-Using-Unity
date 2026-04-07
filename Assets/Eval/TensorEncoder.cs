using System;
public static class TensorEncoder
{
    public const int Channels = 15;
    public const int Rows = 10;
    public const int Cols = 9;
    public const int InputSize = Channels * Rows * Cols;

    private const int SideToMovePlane = 14;

    private static int Idx(int c, int y, int x)
    {
        return c * Rows * Cols + y * Cols + x;
    }

    
    public static float[] EncodeBoard(NewBoardController board, Side sideToMove)
    {
        float[] data = new float[InputSize];

        for (int sq = 0; sq < board.board.Length; sq++)
        {
            int piece = board.board[sq];
            if (piece == NewBoardController.Empty || piece == NewBoardController.Offboard)
                continue;

            if (!NewBoardController.MailboxToBoardXY(sq, out int x, out int y))
                continue;

            int channel = GetChannelFromMailboxPiece(piece);
            data[Idx(channel, y, x)] = 1f;
        }

        float stm = sideToMove == Side.Red ? 1f : 0f;
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++)
            {
                data[Idx(SideToMovePlane, y, x)] = stm;
            }
        }

        return data;
    }

    public static float[] EncodeBoard(NewBoardController.XQPiece?[,] board, Side sideToMove)
    {
        float[] data = new float[InputSize];

        for (int x = 0; x < Cols; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                var pieceNullable = board[x, y];
                if (!pieceNullable.HasValue) continue;

                var piece = pieceNullable.Value;
                int channel = GetChannel(piece.side, piece.kind);
                data[Idx(channel, y, x)] = 1f;
            }
        }

        float stm = sideToMove == Side.Red ? 1f : 0f;
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++)
            {
                data[Idx(SideToMovePlane, y, x)] = stm;
            }
        }

        return data;
    }

    private static int GetChannelFromMailboxPiece(int mailboxPiece)
    {
        switch (mailboxPiece)
        {
            case NewBoardController.RedKing:     return 0;
            case NewBoardController.RedRook:     return 1;
            case NewBoardController.RedHorse:    return 2;
            case NewBoardController.RedElephant: return 3;
            case NewBoardController.RedAdvisor:  return 4;
            case NewBoardController.RedCannon:   return 5;
            case NewBoardController.RedPawn:     return 6;

            case NewBoardController.BlackKing:     return 7;
            case NewBoardController.BlackRook:     return 8;
            case NewBoardController.BlackHorse:    return 9;
            case NewBoardController.BlackElephant: return 10;
            case NewBoardController.BlackAdvisor:  return 11;
            case NewBoardController.BlackCannon:   return 12;
            case NewBoardController.BlackPawn:     return 13;

            default:
                throw new Exception($"Unknown mailbox piece: {mailboxPiece}");
        }
    }

    private static int GetChannel(Side side, PieceKind kind)
    {
        bool red = side == Side.Red;

        switch (kind)
        {
            case PieceKind.King:     return red ? 0 : 7;
            case PieceKind.Rook:     return red ? 1 : 8;
            case PieceKind.Horse:    return red ? 2 : 9;
            case PieceKind.Elephant: return red ? 3 : 10;
            case PieceKind.Advisor:  return red ? 4 : 11;
            case PieceKind.Cannon:   return red ? 5 : 12;
            case PieceKind.Pawn:     return red ? 6 : 13;
            default: throw new Exception("Unknown PieceKind");
        }
    }
}
