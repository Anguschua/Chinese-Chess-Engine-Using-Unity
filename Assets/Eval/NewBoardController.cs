using System;
using System.Collections.Generic;
using UnityEngine;

// The 11*14 mailbox board representation is inspired by Javascript WUKONG Chinese Chess Engine by Code Monkey:
public class NewBoardController
{
    // Constants / mailbox board
    public const int COLS = 9;
    public const int ROWS = 10;

    public const int BoardWidth = 11;
    public const int BoardHeight = 14;
    public const int MailboxSize = BoardWidth * BoardHeight;

    public const int Empty = 0;
    public const int Offboard = 15;

    public const int Red = 0;
    public const int Black = 1;
    public const int NoColor = 2;

    public const int RedPawn = 1;
    public const int RedAdvisor = 2;
    public const int RedElephant = 3;
    public const int RedHorse = 4;
    public const int RedCannon = 5;
    public const int RedRook = 6;
    public const int RedKing = 7;

    public const int BlackPawn = 8;
    public const int BlackAdvisor = 9;
    public const int BlackElephant = 10;
    public const int BlackHorse = 11;
    public const int BlackCannon = 12;
    public const int BlackRook = 13;
    public const int BlackKing = 14;

    public const int TypePawn = 16;
    public const int TypeAdvisor = 17;
    public const int TypeElephant = 18;
    public const int TypeHorse = 19;
    public const int TypeCannon = 20;
    public const int TypeRook = 21;
    public const int TypeKing = 22;

    private static readonly int[] PieceType =
    {
        0,
        TypePawn, TypeAdvisor, TypeElephant, TypeHorse, TypeCannon, TypeRook, TypeKing,
        TypePawn, TypeAdvisor, TypeElephant, TypeHorse, TypeCannon, TypeRook, TypeKing
    };

    private static readonly int[] PieceColor =
    {
        NoColor,
        Red, Red, Red, Red, Red, Red, Red,
        Black, Black, Black, Black, Black, Black, Black
    };

    // Move and piece structs
    [Serializable]
    public struct XQPiece
    {
        public Side side;
        public PieceKind kind;

        public XQPiece(Side side, PieceKind kind)
        {
            this.side = side;
            this.kind = kind;
        }

        public override string ToString()
        {
            return $"{side} {kind}";
        }
    }

    [Serializable]
    public struct XQMove
    {
        public int sx, sy, dx, dy;
        public XQPiece? captured;

        public XQMove(int sx, int sy, int dx, int dy, XQPiece? captured = null)
        {
            this.sx = sx;
            this.sy = sy;
            this.dx = dx;
            this.dy = dy;
            this.captured = captured;
        }

        public bool IsDefault()
        {
            return sx == dx && sy == dy && !captured.HasValue;
        }

        public override string ToString()
        {
            return $"({sx},{sy})->({dx},{dy}) cap={(captured.HasValue ? captured.Value.kind.ToString() : "none")}";
        }
    }

    // Zobrist hashing (mailbox board)
    private static readonly ulong[,] zobristPieceKeys = new ulong[16, MailboxSize];
    private static ulong zobristSideKey;
    private static bool zobristInitialized = false;

    // Mailbox coordinates for playable board squares
    // Top row = Black home rank, bottom row = Red home rank
    public const int A9 = 23, B9 = 24, C9 = 25, D9 = 26, E9 = 27, F9 = 28, G9 = 29, H9 = 30, I9 = 31;
    public const int A8 = 34, B8 = 35, C8 = 36, D8 = 37, E8 = 38, F8 = 39, G8 = 40, H8 = 41, I8 = 42;
    public const int A7 = 45, B7 = 46, C7 = 47, D7 = 48, E7 = 49, F7 = 50, G7 = 51, H7 = 52, I7 = 53;
    public const int A6 = 56, B6 = 57, C6 = 58, D6 = 59, E6 = 60, F6 = 61, G6 = 62, H6 = 63, I6 = 64;
    public const int A5 = 67, B5 = 68, C5 = 69, D5 = 70, E5 = 71, F5 = 72, G5 = 73, H5 = 74, I5 = 75;
    public const int A4 = 78, B4 = 79, C4 = 80, D4 = 81, E4 = 82, F4 = 83, G4 = 84, H4 = 85, I4 = 86;
    public const int A3 = 89, B3 = 90, C3 = 91, D3 = 92, E3 = 93, F3 = 94, G3 = 95, H3 = 96, I3 = 97;
    public const int A2 = 100, B2 = 101, C2 = 102, D2 = 103, E2 = 104, F2 = 105, G2 = 106, H2 = 107, I2 = 108;
    public const int A1 = 111, B1 = 112, C1 = 113, D1 = 114, E1 = 115, F1 = 116, G1 = 117, H1 = 118, I1 = 119;
    public const int A0 = 122, B0 = 123, C0 = 124, D0 = 125, E0 = 126, F0 = 127, G0 = 128, H0 = 129, I0 = 130;

    private static readonly string[] Coordinates =
    {
        "xx","xx","xx","xx","xx","xx","xx","xx","xx","xx","xx",
        "xx","xx","xx","xx","xx","xx","xx","xx","xx","xx","xx",
        "xx","a9","b9","c9","d9","e9","f9","g9","h9","i9","xx",
        "xx","a8","b8","c8","d8","e8","f8","g8","h8","i8","xx",
        "xx","a7","b7","c7","d7","e7","f7","g7","h7","i7","xx",
        "xx","a6","b6","c6","d6","e6","f6","g6","h6","i6","xx",
        "xx","a5","b5","c5","d5","e5","f5","g5","h5","i5","xx",
        "xx","a4","b4","c4","d4","e4","f4","g4","h4","i4","xx",
        "xx","a3","b3","c3","d3","e3","f3","g3","h3","i3","xx",
        "xx","a2","b2","c2","d2","e2","f2","g2","h2","i2","xx",
        "xx","a1","b1","c1","d1","e1","f1","g1","h1","i1","xx",
        "xx","a0","b0","c0","d0","e0","f0","g0","h0","i0","xx",
        "xx","xx","xx","xx","xx","xx","xx","xx","xx","xx","xx",
        "xx","xx","xx","xx","xx","xx","xx","xx","xx","xx","xx"
    };

    private static readonly Dictionary<string, int> CoordToSquare = BuildCoordMap();

    // Directions
    private const int Up = -11;
    private const int Down = 11;
    private const int Left = -1;
    private const int Right = 1;

    private static readonly int[] Orthogonals = { Left, Right, Up, Down };
    private static readonly int[] Diagonals = { Up + Left, Up + Right, Down + Left, Down + Right };

    private static readonly int[][] PawnAttackOffsets =
    {
        new[] { Down, Left, Right }, // red attacks from perspective of attacked square
        new[] { Up, Left, Right }    // black
    };

    private static readonly int[][] PawnMoveOffsets =
    {
        new[] { Up, Left, Right },   // red moves upward on mailbox
        new[] { Down, Left, Right }  // black moves downward
    };

    private static readonly int[][] KnightAttackOffsets =
    {
        new[] { Up + Up + Left, Left + Left + Up },
        new[] { Up + Up + Right, Right + Right + Up },
        new[] { Down + Down + Left, Left + Left + Down },
        new[] { Right + Right + Down, Down + Down + Right }
    };

    private static readonly int[][] KnightMoveOffsets =
    {
        new[] { Left + Left + Up, Left + Left + Down },
        new[] { Right + Right + Up, Right + Right + Down },
        new[] { Up + Up + Left, Up + Up + Right },
        new[] { Down + Down + Left, Down + Down + Right }
    };

    private static readonly int[] ElephantMoveOffsets =
    {
        (Up + Left) * 2,
        (Up + Right) * 2,
        (Down + Left) * 2,
        (Down + Right) * 2
    };

    // Board zones
    // 0 = invalid for side, 1 = side's own half, 2 = palace
    private static readonly int[][] BoardZones =
    {
        new[]
        {
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,1,1,1,1,1,1,1,1,1,0,
            0,1,1,1,1,1,1,1,1,1,0,
            0,1,1,1,2,2,2,1,1,1,0,
            0,1,1,1,2,2,2,1,1,1,0,
            0,1,1,1,2,2,2,1,1,1,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0
        },
        new[]
        {
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,1,1,1,2,2,2,1,1,1,0,
            0,1,1,1,2,2,2,1,1,1,0,
            0,1,1,1,2,2,2,1,1,1,0,
            0,1,1,1,1,1,1,1,1,1,0,
            0,1,1,1,1,1,1,1,1,1,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0
        }
    };

    // Move encoding
    // bits:
    // source square  0..7
    // target square  8..15
    // source piece  16..19
    // target piece  20..23
    // capture flag  24
    public static int EncodeMove(int sourceSquare, int targetSquare, int sourcePiece, int targetPiece, int captureFlag)
    {
        return sourceSquare |
               (targetSquare << 8) |
               (sourcePiece << 16) |
               (targetPiece << 20) |
               (captureFlag << 24);
    }

    public static int GetSourceSquare(int move) => move & 0xFF;
    public static int GetTargetSquare(int move) => (move >> 8) & 0xFF;
    public static int GetSourcePiece(int move) => (move >> 16) & 0xF;
    public static int GetTargetPiece(int move) => (move >> 20) & 0xF;
    public static int GetCaptureFlag(int move) => (move >> 24) & 0x1;


    public readonly int[] board = new int[MailboxSize];
    public int side = Red;
    public int[] kingSquare = new int[2];
    public int ply;
    public ulong currentHash;

    public bool useHash = false;

    private readonly List<MoveState> moveStack = new List<MoveState>(256);

    private struct MoveState
    {
        public int move;
        public int side;
        public int redKingSquare;
        public int blackKingSquare;
        public ulong hash;
    }

    public NewBoardController()
    {
        ResetBoard();
    }

    public void ResetBoard()
    {
        EnsureZobrist();

        for (int rank = 0; rank < BoardHeight; rank++)
        {
            for (int file = 0; file < BoardWidth; file++)
            {
                int sq = rank * BoardWidth + file;
                board[sq] = Coordinates[sq] == "xx" ? Offboard : Empty;
            }
        }

        side = Red;
        kingSquare[Red] = 0;
        kingSquare[Black] = 0;
        ply = 0;
        currentHash = 0UL;
        moveStack.Clear();
    }

    public void SetStartPosition()
    {
        LoadFromFen("rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w");
    }

    public void LoadFromFen(string fen)
    {
        ResetBoard();

        string[] parts = fen.Trim().Split(' ');
        string placement = parts[0];
        string stm = parts.Length > 1 ? parts[1] : "w";

        int index = 0;

        for (int rank = 0; rank < BoardHeight; rank++)
        {
            for (int file = 0; file < BoardWidth; file++)
            {
                int sq = rank * BoardWidth + file;
                if (board[sq] == Offboard) continue;

                char c = placement[index];

                if (char.IsLetter(c))
                {
                    int piece = CharToPiece(c);
                    board[sq] = piece;

                    if (piece == RedKing) kingSquare[Red] = sq;
                    else if (piece == BlackKing) kingSquare[Black] = sq;

                    index++;
                }
                else if (char.IsDigit(c))
                {
                    int offset = c - '0';
                    if (board[sq] == Empty) file--;
                    file += offset;
                    index++;
                }

                if (index < placement.Length && placement[index] == '/')
                    index++;
            }
        }

        side = (stm == "b") ? Black : Red;
        currentHash = ComputeFullHash();
    }

    public void LoadFromPieceArray(XQPiece?[,] sourceBoard, Side sideToMove)
    {
        ResetBoard();
        side = sideToMove == Side.Red ? Red : Black;

        for (int x = 0; x < COLS; x++)
        {
            for (int y = 0; y < ROWS; y++)
            {
                var p = sourceBoard[x, y];
                if (!p.HasValue) continue;

                int sq = BoardXYToMailbox(x, y);
                int piece = ConvertPieceToMailbox(p.Value.side, p.Value.kind);
                board[sq] = piece;

                if (piece == RedKing) kingSquare[Red] = sq;
                else if (piece == BlackKing) kingSquare[Black] = sq;
            }
        }

        currentHash = ComputeFullHash();
    }

    public XQPiece?[,] ExportToPieceArray()
    {
        var result = new XQPiece?[COLS, ROWS];

        for (int sq = 0; sq < board.Length; sq++)
        {
            int piece = board[sq];
            if (piece == Empty || piece == Offboard) continue;

            if (!MailboxToBoardXY(sq, out int x, out int y))
                continue;

            result[x, y] = ConvertPieceFromMailbox(piece);
        }

        return result;
    }

    public void LoadFromSceneBoard(Chess[,] boardState, Side stm)
    {
        ResetBoard();
        side = (stm == Side.Red) ? Red : Black;

        for (int x = 0; x < COLS; x++)
        {
            for (int y = 0; y < ROWS; y++)
            {
                Chess c = boardState[x, y];
                if (c == null) continue;
                if (!c.gameObject.activeInHierarchy) continue;

                int sq = BoardXYToMailbox(x, y);
                int piece = ConvertPieceToMailbox(c.side, c.kind);
                board[sq] = piece;

                if (piece == RedKing) kingSquare[Red] = sq;
                else if (piece == BlackKing) kingSquare[Black] = sq;
            }
        }

        currentHash = ComputeFullHash();
    }

    public bool HasAnyLegalMoveForSide(Side sideEnum)
    {
        int originalSide = side;
        side = (sideEnum == Side.Red) ? Red : Black;

        bool result = HasAnyLegalMove();

        side = originalSide;
        return result;
    }

    public bool IsInCheckForSide(Side sideEnum)
    {
        int sideColor = (sideEnum == Side.Red) ? Red : Black;
        return IsInCheck(sideColor);
    }

    public List<int> GeneratePseudoMoves(bool capturesOnly = false)
    {
        var moves = new List<int>(128);

        for (int sq = 0; sq < board.Length; sq++)
        {
            int piece = board[sq];
            if (piece == Empty || piece == Offboard) continue;
            if (PieceColor[piece] != side) continue;

            int pieceType = PieceType[piece];

            // Pawn
            if (pieceType == TypePawn)
            {
                for (int dir = 0; dir < PawnMoveOffsets[side].Length; dir++)
                {
                    int target = sq + PawnMoveOffsets[side][dir];
                    int targetPiece = board[target];

                    if (targetPiece == Offboard) continue;
                    PushMove(moves, sq, target, piece, targetPiece, capturesOnly);
                    if (BoardZones[side][sq] == 1) break;
                }
            }

            // King / Advisor
            if (pieceType == TypeKing || pieceType == TypeAdvisor)
            {
                int[] offsets = pieceType == TypeKing ? Orthogonals : Diagonals;
                for (int dir = 0; dir < offsets.Length; dir++)
                {
                    int target = sq + offsets[dir];
                    int targetPiece = board[target];

                    if (BoardZones[side][target] == 2)
                        PushMove(moves, sq, target, piece, targetPiece, capturesOnly);
                }
            }

            // Elephant
            if (pieceType == TypeElephant)
            {
                for (int dir = 0; dir < ElephantMoveOffsets.Length; dir++)
                {
                    int target = sq + ElephantMoveOffsets[dir];
                    int eye = sq + Diagonals[dir];
                    int targetPiece = board[target];

                    if (BoardZones[side][target] == 0) continue;
                    if (board[eye] != Empty) continue;

                    PushMove(moves, sq, target, piece, targetPiece, capturesOnly);
                }
            }

            // Horse
            if (pieceType == TypeHorse)
            {
                for (int dir = 0; dir < Orthogonals.Length; dir++)
                {
                    int leg = sq + Orthogonals[dir];
                    if (board[leg] != Empty) continue;

                    for (int k = 0; k < 2; k++)
                    {
                        int target = sq + KnightMoveOffsets[dir][k];
                        int targetPiece = board[target];
                        if (targetPiece == Offboard) continue;

                        PushMove(moves, sq, target, piece, targetPiece, capturesOnly);
                    }
                }
            }

            // Rook / Cannon
            if (pieceType == TypeRook || pieceType == TypeCannon)
            {
                for (int dir = 0; dir < Orthogonals.Length; dir++)
                {
                    int target = sq + Orthogonals[dir];
                    int jumpOver = 0;

                    while (board[target] != Offboard)
                    {
                        int targetPiece = board[target];

                        if (jumpOver == 0)
                        {
                            if (pieceType == TypeRook)
                            {
                                PushMove(moves, sq, target, piece, targetPiece, capturesOnly);
                                if (targetPiece != Empty) break;
                            }
                            else
                            {
                                if (targetPiece == Empty)
                                {
                                    PushMove(moves, sq, target, piece, targetPiece, capturesOnly);
                                }
                                else
                                {
                                    jumpOver = 1;
                                }
                            }
                        }
                        else
                        {
                            if (targetPiece != Empty)
                            {
                                if (PieceColor[targetPiece] == (side ^ 1))
                                    PushMove(moves, sq, target, piece, targetPiece, capturesOnly);
                                break;
                            }
                        }

                        if (pieceType == TypeRook && targetPiece != Empty)
                            break;

                        target += Orthogonals[dir];
                    }
                }
            }
        }

        return moves;
    }

    public List<int> GenerateLegalMoves(bool capturesOnly = false)
    {
        var legal = new List<int>(128);
        var pseudo = GeneratePseudoMoves(capturesOnly);

        for (int i = 0; i < pseudo.Count; i++)
        {
            int move = pseudo[i];
            if (MakeMove(move))
            {
                legal.Add(move);
                TakeBack();
            }
        }

        return legal;
    }

    public bool HasAnyLegalMove()
    {
        var pseudo = GeneratePseudoMoves(false);

        for (int i = 0; i < pseudo.Count; i++)
        {
            if (MakeMove(pseudo[i]))
            {
                TakeBack();
                return true;
            }
        }

        return false;
    }

    private void PushMove(List<int> moveList, int sourceSquare, int targetSquare, int sourcePiece, int targetPiece, bool capturesOnly)
    {
        if (targetPiece != Empty && PieceColor[targetPiece] == side)
            return;

        if (capturesOnly && targetPiece == Empty)
            return;

        int captureFlag = targetPiece != Empty ? 1 : 0;
        int move = EncodeMove(sourceSquare, targetSquare, sourcePiece, targetPiece, captureFlag);
        moveList.Add(move);
    }

    public bool MakeMove(int move)
    {
        EnsureZobrist();

        int sourceSquare = GetSourceSquare(move);
        int targetSquare = GetTargetSquare(move);
        int sourcePiece = GetSourcePiece(move);
        int targetPiece = GetTargetPiece(move);
        bool wasCapture = GetCaptureFlag(move) != 0;

        if (board[sourceSquare] != sourcePiece)
            return false;

        moveStack.Add(new MoveState
        {
            move = move,
            side = side,
            redKingSquare = kingSquare[Red],
            blackKingSquare = kingSquare[Black],
            hash = currentHash
        });

        // Incremental Hash Update
        currentHash ^= zobristPieceKeys[sourcePiece, sourceSquare];
        if (wasCapture && targetPiece != Empty)
            currentHash ^= zobristPieceKeys[targetPiece, targetSquare];
        currentHash ^= zobristPieceKeys[sourcePiece, targetSquare];
        currentHash ^= zobristSideKey;

        board[targetSquare] = sourcePiece;
        board[sourceSquare] = Empty;

        if (sourcePiece == RedKing) kingSquare[Red] = targetSquare;
        else if (sourcePiece == BlackKing) kingSquare[Black] = targetSquare;

        side ^= 1;
        ply++;

        if (IsSquareAttacked(kingSquare[side ^ 1], side))
        {
            TakeBack();
            return false;
        }

        return true;
    }

    public void TakeBack()
    {
        if (moveStack.Count == 0)
            return;

        MoveState state = moveStack[moveStack.Count - 1];
        moveStack.RemoveAt(moveStack.Count - 1);

        int move = state.move;
        int sourceSquare = GetSourceSquare(move);
        int targetSquare = GetTargetSquare(move);
        int sourcePiece = GetSourcePiece(move);
        int targetPiece = GetTargetPiece(move);
        bool wasCapture = GetCaptureFlag(move) != 0;

        board[sourceSquare] = sourcePiece;
        board[targetSquare] = wasCapture ? targetPiece : Empty;

        side = state.side;
        kingSquare[Red] = state.redKingSquare;
        kingSquare[Black] = state.blackKingSquare;
        currentHash = state.hash;
        ply--;
    }

    public bool IsSquareAttacked(int square, int byColor)
    {
        // Horse attacks
        for (int dir = 0; dir < Diagonals.Length; dir++)
        {
            int leg = square + Diagonals[dir];
            if (board[leg] != Empty) continue;

            for (int k = 0; k < 2; k++)
            {
                int from = square + KnightAttackOffsets[dir][k];
                int piece = board[from];
                if (piece == ((byColor == Red) ? RedHorse : BlackHorse))
                    return true;
            }
        }

        // King / rook / cannon line attacks
        for (int dir = 0; dir < Orthogonals.Length; dir++)
        {
            int target = square + Orthogonals[dir];
            int jumpOver = 0;

            while (board[target] != Offboard)
            {
                int piece = board[target];

                if (jumpOver == 0)
                {
                    if (piece == ((byColor == Red) ? RedRook : BlackRook) ||
                        piece == ((byColor == Red) ? RedKing : BlackKing))
                        return true;
                }

                if (piece != Empty) jumpOver++;

                if (jumpOver == 2 && piece == ((byColor == Red) ? RedCannon : BlackCannon))
                    return true;

                target += Orthogonals[dir];
            }
        }

        // Pawn attacks
        for (int dir = 0; dir < PawnAttackOffsets[byColor].Length; dir++)
        {
            int from = square + PawnAttackOffsets[byColor][dir];
            if (board[from] == ((byColor == Red) ? RedPawn : BlackPawn))
                return true;
        }

        return false;
    }

    public bool IsInCheck(int color)
    {
        return IsSquareAttacked(kingSquare[color], color ^ 1);
    }

    
    public static int BoardXYToMailbox(int x, int y)
    {
        // Mailbox uses top rank = 9, bottom rank = 0.
        int mailboxRankFromTop = 2 + (9 - y);
        int mailboxFile = 1 + x;
        return mailboxRankFromTop * BoardWidth + mailboxFile;
    }

    public static bool MailboxToBoardXY(int sq, out int x, out int y)
    {
        x = 0;
        y = 0;

        if (sq < 0 || sq >= Coordinates.Length || Coordinates[sq] == "xx")
            return false;

        int rankFromTop = (sq / BoardWidth) - 2;
        int file = (sq % BoardWidth) - 1;

        if (file < 0 || file > 8 || rankFromTop < 0 || rankFromTop > 9)
            return false;

        x = file;
        y = 9 - rankFromTop;
        return true;
    }

    public static int ConvertPieceToMailbox(Side side, PieceKind kind)
    {
        bool isRed = side == Side.Red;
        switch (kind)
        {
            case PieceKind.Pawn: return isRed ? RedPawn : BlackPawn;
            case PieceKind.Advisor: return isRed ? RedAdvisor : BlackAdvisor;
            case PieceKind.Elephant: return isRed ? RedElephant : BlackElephant;
            case PieceKind.Horse: return isRed ? RedHorse : BlackHorse;
            case PieceKind.Cannon: return isRed ? RedCannon : BlackCannon;
            case PieceKind.Rook: return isRed ? RedRook : BlackRook;
            case PieceKind.King: return isRed ? RedKing : BlackKing;
            default: return Empty;
        }
    }

    public static XQPiece ConvertPieceFromMailbox(int piece)
    {
        switch (piece)
        {
            case RedPawn: return new XQPiece(Side.Red, PieceKind.Pawn);
            case RedAdvisor: return new XQPiece(Side.Red, PieceKind.Advisor);
            case RedElephant: return new XQPiece(Side.Red, PieceKind.Elephant);
            case RedHorse: return new XQPiece(Side.Red, PieceKind.Horse);
            case RedCannon: return new XQPiece(Side.Red, PieceKind.Cannon);
            case RedRook: return new XQPiece(Side.Red, PieceKind.Rook);
            case RedKing: return new XQPiece(Side.Red, PieceKind.King);

            case BlackPawn: return new XQPiece(Side.Black, PieceKind.Pawn);
            case BlackAdvisor: return new XQPiece(Side.Black, PieceKind.Advisor);
            case BlackElephant: return new XQPiece(Side.Black, PieceKind.Elephant);
            case BlackHorse: return new XQPiece(Side.Black, PieceKind.Horse);
            case BlackCannon: return new XQPiece(Side.Black, PieceKind.Cannon);
            case BlackRook: return new XQPiece(Side.Black, PieceKind.Rook);
            case BlackKing: return new XQPiece(Side.Black, PieceKind.King);

            default:
                throw new ArgumentOutOfRangeException(nameof(piece), $"Unsupported mailbox piece: {piece}");
        }
    }

    public static XQMove ToXQMove(int encodedMove)
    {
        int src = GetSourceSquare(encodedMove);
        int dst = GetTargetSquare(encodedMove);

        if (!MailboxToBoardXY(src, out int sx, out int sy))
            throw new Exception("Invalid source mailbox square.");
        if (!MailboxToBoardXY(dst, out int dx, out int dy))
            throw new Exception("Invalid target mailbox square.");

        int targetPiece = GetTargetPiece(encodedMove);
        XQPiece? captured = null;
        if (targetPiece != Empty)
            captured = ConvertPieceFromMailbox(targetPiece);

        return new XQMove(sx, sy, dx, dy, captured);
    }

    public static int FromXQMove(XQMove move, int sourcePieceMailbox, int targetPieceMailbox)
    {
        int src = BoardXYToMailbox(move.sx, move.sy);
        int dst = BoardXYToMailbox(move.dx, move.dy);
        int capture = targetPieceMailbox != Empty ? 1 : 0;
        return EncodeMove(src, dst, sourcePieceMailbox, targetPieceMailbox, capture);
    }

    public string MoveToString(int move)
    {
        return Coordinates[GetSourceSquare(move)] + Coordinates[GetTargetSquare(move)];
    }

    public void PrintBoard()
    {
        string s = "";

        for (int rank = 0; rank < BoardHeight; rank++)
        {
            for (int file = 0; file < BoardWidth; file++)
            {
                int sq = rank * BoardWidth + file;
                if (board[sq] == Offboard) continue;

                if (file == 1)
                    s += (11 - rank) + " ";

                s += PieceToChar(board[sq]) + " ";
            }

            s += "\n";
        }

        s += "  a b c d e f g h i\n";
        s += $"side: {(side == Red ? "red" : "black")}\n";
        Debug.Log(s);
    }

    private static char PieceToChar(int piece)
    {
        switch (piece)
        {
            case Empty: return '.';
            case RedPawn: return 'P';
            case RedAdvisor: return 'A';
            case RedElephant: return 'B';
            case RedHorse: return 'N';
            case RedCannon: return 'C';
            case RedRook: return 'R';
            case RedKing: return 'K';
            case BlackPawn: return 'p';
            case BlackAdvisor: return 'a';
            case BlackElephant: return 'b';
            case BlackHorse: return 'n';
            case BlackCannon: return 'c';
            case BlackRook: return 'r';
            case BlackKing: return 'k';
            default: return '?';
        }
    }

    private static int CharToPiece(char c)
    {
        switch (c)
        {
            case 'P': return RedPawn;
            case 'A': return RedAdvisor;
            case 'B':
            case 'E': return RedElephant;
            case 'N':
            case 'H': return RedHorse;
            case 'C': return RedCannon;
            case 'R': return RedRook;
            case 'K': return RedKing;

            case 'p': return BlackPawn;
            case 'a': return BlackAdvisor;
            case 'b':
            case 'e': return BlackElephant;
            case 'n':
            case 'h': return BlackHorse;
            case 'c': return BlackCannon;
            case 'r': return BlackRook;
            case 'k': return BlackKing;

            default:
                throw new ArgumentException($"Unsupported FEN piece: {c}");
        }
    }

    private static Dictionary<string, int> BuildCoordMap()
    {
        var map = new Dictionary<string, int>(154);
        for (int i = 0; i < Coordinates.Length; i++)
        {
            if (Coordinates[i] != "xx")
                map[Coordinates[i]] = i;
        }

        return map;
    }

    public bool IsMoveLegalBoardCoords(int sx, int sy, int dx, int dy)
    {
        int srcSq = BoardXYToMailbox(sx, sy);
        int dstSq = BoardXYToMailbox(dx, dy);

        int sourcePiece = board[srcSq];
        int targetPiece = board[dstSq];

        if (sourcePiece == Empty || sourcePiece == Offboard)
            return false;

        if (PieceColor[sourcePiece] != side)
            return false;

        if (targetPiece != Empty && targetPiece != Offboard && PieceColor[targetPiece] == side)
            return false;

        var legalMoves = GenerateLegalMoves(false);
        for (int i = 0; i < legalMoves.Count; i++)
        {
            int move = legalMoves[i];
            if (GetSourceSquare(move) == srcSq && GetTargetSquare(move) == dstSq)
                return true;
        }

        return false;
    }

    public int EncodeBoardMoveFromCoords(int sx, int sy, int dx, int dy)
    {
        int srcSq = BoardXYToMailbox(sx, sy);
        int dstSq = BoardXYToMailbox(dx, dy);

        int sourcePiece = board[srcSq];
        int targetPiece = board[dstSq];

        int capture = (targetPiece != Empty && targetPiece != Offboard) ? 1 : 0;
        return EncodeMove(srcSq, dstSq, sourcePiece, targetPiece, capture);
    }

    public bool TryMakeBoardMove(int sx, int sy, int dx, int dy, out int encodedMove)
    {
        encodedMove = 0;

        int move = EncodeBoardMoveFromCoords(sx, sy, dx, dy);
        int sourcePiece = GetSourcePiece(move);

        if (sourcePiece == Empty || sourcePiece == Offboard)
            return false;

        if (!MakeMove(move))
            return false;

        encodedMove = move;
        return true;
    }

    private static void EnsureZobrist()
    {
        if (zobristInitialized) return;

        ulong state = 0x9E3779B97F4A7C15UL;

        for (int piece = 0; piece < 16; piece++)
        {
            for (int sq = 0; sq < MailboxSize; sq++)
            {
                zobristPieceKeys[piece, sq] = NextU64(ref state);
            }
        }

        zobristSideKey = NextU64(ref state);
        zobristInitialized = true;
    }

    private static ulong NextU64(ref ulong state)
    {
        state += 0x9E3779B97F4A7C15UL;
        ulong z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    private ulong ComputeFullHash()
    {
        EnsureZobrist();

        ulong hash = 0UL;

        for (int sq = 0; sq < board.Length; sq++)
        {
            int piece = board[sq];
            if (piece == Empty || piece == Offboard) continue;
            hash ^= zobristPieceKeys[piece, sq];
        }

        if (side == Black)
            hash ^= zobristSideKey;

        return hash;
    }

    public bool IsRepetition()
    {
        for (int i = 0; i < moveStack.Count; i++)
        {
            if (moveStack[i].hash == currentHash)
                return true;
        }

        return false;
    }

    public Side GetSideAsEnum()
    {
        return side == Red ? Side.Red : Side.Black;
    }
}