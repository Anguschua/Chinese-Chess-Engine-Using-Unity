using UnityEngine;

public static class Evaluator
{
    public static NNInference nnInference;

    public static bool useNN = false;
    public static bool blendNNWithHandcrafted = false;
    public static float nnWeight = 1f;

    // PST Table Set Up:
    // For simplicity, we use the same PST for both sides, and mirror the square index

    /*
    material weights: by Yen et al. 2004, "Computer Chinese Chess" ICGA Journal
    PST weights: by Yen et al. 2004, "Computer Chinese Chess" ICGA Journal, adjusted with Mailbox board orientation
    */
    private static readonly bool[] EvaluateTypes =
    {
        true,   // Pawn
        false,  // Advisor
        false,  // Elephant
        true,   // Horse
        true,   // Cannon
        true,   // Rook
        false   // King
    };

    private static readonly int[] MaterialWeights =
    {
         0,
         30,   // RedPawn
         120,  // RedAdvisor
         120,  // RedElephant
         270,  // RedHorse
         285,  // RedCannon
         600,  // RedRook
         6000, // RedKing

        -30,   // BlackPawn
        -120,  // BlackAdvisor
        -120,  // BlackElephant
        -270,  // BlackHorse
        -285,  // BlackCannon
        -600,  // BlackRook
        -6000  // BlackKing
    };

    // PST index:
    // 0 pawn
    // 1 advisor (unused)
    // 2 elephant (unused)
    // 3 horse
    // 4 cannon
    // 5 rook
    // 6 king (unused)
    private static readonly int[][] PST =
    {
        // Pawn
        new int[]
        {
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  3,  6,  9, 12,  9,  6,  3,  0,  0,
            0, 18, 36, 56, 80,120, 80, 56, 36, 18,  0,
            0, 14, 26, 42, 60, 80, 60, 42, 26, 14,  0,
            0, 10, 20, 30, 34, 40, 34, 30, 20, 10,  0,
            0,  6, 12, 18, 18, 20, 18, 18, 12,  6,  0,
            0,  2,  0,  8,  0,  8,  0,  8,  0,  2,  0,
            0,  0,  0, -2,  0,  4,  0, -2,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
        },

        // Advisor (unused)
        new int[154],

        // Elephant (unused)
        new int[154],

        // Horse
        new int[]
        {
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  4,  8, 16, 12,  4, 12, 16,  8,  4,  0,
            0,  4, 10, 28, 16,  8, 16, 28, 10,  4,  0,
            0, 12, 14, 16, 20, 18, 20, 16, 14, 12,  0,
            0,  8, 24, 18, 24, 20, 24, 18, 24,  8,  0,
            0,  6, 16, 14, 18, 16, 18, 14, 16,  6,  0,
            0,  4, 12, 16, 14, 12, 14, 16, 12,  4,  0,
            0,  2,  6,  8,  6, 10,  6,  8,  6,  2,  0,
            0,  4,  2,  8,  8,  4,  8,  8,  2,  4,  0,
            0,  0,  2,  4,  4, -2,  4,  4,  2,  0,  0,
            0,  0, -4,  0,  0,  0,  0,  0, -4,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0
        },

        // Cannon
        new int[]
        {
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  6,  4,  0,-10,-12,-10,  0,  4,  6,  0,
            0,  2,  2,  0, -4,-14, -4,  0,  2,  2,  0,
            0,  2,  2,  0,-10, -8,-10,  0,  2,  2,  0,
            0,  0,  0, -2,  4, 10,  4, -2,  0,  0,  0,
            0,  0,  0,  0,  2,  8,  2,  0,  0,  0,  0,
            0, -2,  0,  4,  2,  6,  2,  4,  0, -2,  0,
            0,  0,  0,  0,  2,  4,  2,  0,  0,  0,  0,
            0,  4,  0,  8,  6, 10,  6,  8,  0,  4,  0,
            0,  0,  2,  4,  6,  6,  6,  4,  2,  0,  0,
            0,  0,  0,  2,  6,  6,  6,  2,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0
        },

        // Rook
        new int[]
        {
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0, 14, 14, 12, 18, 16, 18, 12, 14, 14,  0,
            0, 16, 20, 18, 24, 26, 24, 18, 20, 16,  0,
            0, 12, 12, 12, 18, 18, 18, 12, 12, 12,  0,
            0, 12, 18, 16, 22, 22, 22, 16, 18, 12,  0,
            0, 12, 14, 12, 18, 18, 18, 12, 14, 12,  0,
            0, 12, 16, 14, 20, 20, 20, 14, 16, 12,  0,
            0,  6, 10,  8, 14, 14, 14,  8, 10,  6,  0,
            0,  4,  8,  6, 14, 12, 14,  6,  8,  4,  0,
            0,  8,  4,  8, 16,  8, 16,  8,  4,  8,  0,
            0, -2, 10,  6, 14, 12, 14,  6, 10, -2,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0
        },

        // King (unused)
        new int[154]
    };

    private static readonly int[] MirrorSquare =
    {
        0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
        0,122,123,124,125,126,127,128,129,130,  0,
        0,111,112,113,114,115,116,117,118,119,  0,
        0,100,101,102,103,104,105,106,107,108,  0,
        0, 89, 90, 91, 92, 93, 94, 95, 96, 97,  0,
        0, 78, 79, 80, 81, 82, 83, 84, 85, 86,  0,
        0, 67, 68, 69, 70, 71, 72, 73, 74, 75,  0,
        0, 56, 57, 58, 59, 60, 61, 62, 63, 64,  0,
        0, 45, 46, 47, 48, 49, 50, 51, 52, 53,  0,
        0, 34, 35, 36, 37, 38, 39, 40, 41, 42,  0,
        0, 23, 24, 25, 26, 27, 28, 29, 30, 31,  0,
        0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0
    };

    public static int Evaluate(NewBoardController board, Side sideToMove, Side sideToScore)
    {
        int handcrafted = EvaluateHandcrafted(board, sideToMove, sideToScore);

        if (!useNN || nnInference == null)
            return handcrafted;

        int nnScore = EvaluateWithNN(board, sideToMove, sideToScore);

        if (!blendNNWithHandcrafted)
            return nnScore;

        float blended = (1f - nnWeight) * handcrafted + nnWeight * nnScore;
        return Mathf.RoundToInt(blended);
    }

    public static int EvaluateHandcrafted(NewBoardController board, Side sideToMove, Side sideToScore)
    {
        int score = 0;

        for (int square = 0; square < board.board.Length; square++)
        {
            int piece = board.board[square];
            if (piece == NewBoardController.Empty || piece == NewBoardController.Offboard)
                continue;

            score += MaterialWeights[piece];

            int pstIndex = GetPstIndexFromMailboxPiece(piece);
            if (pstIndex < 0 || !EvaluateTypes[pstIndex])
                continue;

            int pieceColor = GetColorFromMailboxPiece(piece);

            if (pieceColor == NewBoardController.Red)
                score += PST[pstIndex][square];
            else
                score -= PST[pstIndex][MirrorSquare[square]];
        }
        
        int sideScore = (board.side == NewBoardController.Red) ? score : -score;

        Side stm = (board.side == NewBoardController.Red) ? Side.Red : Side.Black;
        
        if (sideToScore == stm)
            return sideScore;
        else
            return -sideScore;
    }

    // Not Used
    public static int EvaluateWithNN(NewBoardController board, Side sideToMove, Side sideToScore)
    {
        if (nnInference == null)
        {
            Debug.LogWarning("[Evaluator] nnInference is null. Falling back to handcrafted eval.");
            return EvaluateHandcrafted(board, sideToMove, sideToScore);
        }

        Side actualSTM = (board.side == NewBoardController.Red) ? Side.Red : Side.Black;

        float[] input = TensorEncoder.EncodeBoard(board, actualSTM);
        float raw = nnInference.EvaluateBoard(input);

        if (float.IsNaN(raw) || float.IsInfinity(raw))
        {
            Debug.LogWarning($"[Evaluator] NN returned invalid value: {raw}. Fallback to handcrafted.");
            return EvaluateHandcrafted(board, sideToMove, sideToScore);
        }

        // Convert Red-perspective network output to engine score
        int redPerspectiveScore = (int)raw;

        return (sideToScore == Side.Red) ? redPerspectiveScore : -redPerspectiveScore;
    }
    private static int GetColorFromMailboxPiece(int mailboxPiece)
    {
        return mailboxPiece <= NewBoardController.RedKing
            ? NewBoardController.Red
            : NewBoardController.Black;
    }

    private static int GetPstIndexFromMailboxPiece(int mailboxPiece)
    {
        switch (mailboxPiece)
        {
            case NewBoardController.RedPawn:
            case NewBoardController.BlackPawn:
                return 0;

            case NewBoardController.RedAdvisor:
            case NewBoardController.BlackAdvisor:
                return 1;

            case NewBoardController.RedElephant:
            case NewBoardController.BlackElephant:
                return 2;

            case NewBoardController.RedHorse:
            case NewBoardController.BlackHorse:
                return 3;

            case NewBoardController.RedCannon:
            case NewBoardController.BlackCannon:
                return 4;

            case NewBoardController.RedRook:
            case NewBoardController.BlackRook:
                return 5;

            case NewBoardController.RedKing:
            case NewBoardController.BlackKing:
                return 6;

            default:
                return -1;
        }
    }

    public static int GetPieceValue(PieceKind kind)
    {
        switch (kind)
        {
            case PieceKind.King:     return 6000;
            case PieceKind.Rook:     return 600;
            case PieceKind.Cannon:   return 285;
            case PieceKind.Horse:    return 270;
            case PieceKind.Elephant: return 120;
            case PieceKind.Advisor:  return 120;
            case PieceKind.Pawn:     return 30;
            default:                 return 0;
        }
    }
}