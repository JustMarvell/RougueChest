using System.Collections.Generic;

namespace Chess.Core
{
    public static class MoveGenerator
    {
        static readonly (int, int)[] KnightOffsets = { (1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2) };
        static readonly (int, int)[] KingOffsets = { (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1) };
        static readonly (int, int)[] BishopDirs = { (1, 1), (1, -1), (-1, 1), (-1, -1) };
        static readonly (int, int)[] RookDirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        public static List<Move> GeneratePseudoLegalMoves(Board board, Square from)
        {
            var piece = board.Get(from);
            var moves = new List<Move>();
            if (piece == null) return moves;

            switch (piece.Type)
            {
                case PieceType.Pawn : GeneratePawnMoves(board, from, piece, moves); break;
                case PieceType.Knight : GenerateOffsetMoves(board, from, piece, KnightOffsets, moves); break;
                case PieceType.King : GenerateOffsetMoves(board, from, piece, KingOffsets, moves); break;
                case PieceType.Bishop : GenerateSlidingMoves(board, from, piece, BishopDirs, moves); break;
                case PieceType.Rook : GenerateSlidingMoves(board, from, piece, RookDirs, moves); break;
                case PieceType.Queen :
                    GenerateSlidingMoves(board, from, piece, BishopDirs, moves);
                    GenerateSlidingMoves(board, from, piece, RookDirs, moves);
                    break;
            }

            return moves;
        }

        static void GeneratePawnMoves(Board board, Square from, Piece piece, List<Move> moves)
        {
            int dir = piece.Color == PieceColor.White ? 1 : -1;
            int startRank = piece.Color == PieceColor.White ? 1 : 6;

            var oneStep = new Square(from.File, from.Rank + dir);
            if (oneStep.IsValid && board.IsEmpty(oneStep))
            {
                moves.Add(new Move(from, oneStep, false));
                var twoStep = new Square(from.File, from.Rank + dir * 2);
                if (from.Rank == startRank && board.IsEmpty(twoStep))
                    moves.Add(new Move(from, twoStep, false));
            }

            foreach (int df in new[] { -1, 1 })
            {
                var target = new Square(from.File + df, from.Rank + dir);
                if (!target.IsValid) continue;
                var occupant = board.Get(target);
                if (occupant != null && occupant.Color != piece.Color)
                    moves.Add(new Move(from, target, true));
            }
        }

        static void GenerateOffsetMoves(Board board, Square from, Piece piece, (int, int)[] offsets, List<Move> moves)
        {
            foreach (var (df, dr) in offsets)
            {
                var target = new Square(from.File + df, from.Rank + dr);
                if (!target.IsValid) continue;
                var occupant = board.Get(target);
                if (occupant == null) moves.Add(new Move(from, target, false));
                else if (occupant.Color != piece.Color) moves.Add(new Move(from, target, true));
            }
        }

        static void GenerateSlidingMoves(Board board, Square from, Piece piece, (int, int)[] dirs, List<Move> moves)
        {
            foreach (var (df, dr) in dirs)
            {
                var target = new Square(from.File + df, from.Rank + dr);
                while (target.IsValid)
                {
                    var occupant = board.Get(target);
                    if (occupant == null) 
                        moves.Add(new Move(from, target, false));
                    else 
                    {
                        if (occupant.Color != piece.Color) moves.Add(new Move(from, target, true));
                        break;
                    }

                    target = new Square(target.File + df, target.Rank + dr);
                }
            }
        }

        public static bool IsSquareAttacked(Board board, Square square, PieceColor byColor)
        {
            for (int f = 0; f < 8; f++)
            {
                for (int r = 0; r < 8; r++)
                {
                    var piece = board.Squares[f, r];
                    if (piece == null || piece.Color != byColor) continue;
                    var from = new Square(f, r);

                    if (piece.Type == PieceType.Pawn)
                    {
                        int dir = piece.Color == PieceColor.White ? 1 : -1;
                        if (square == new Square(from.File - 1, from.Rank + dir)) return true;
                        if (square == new Square(from.File + 1, from.Rank + dir)) return true;
                        continue;
                    }

                    foreach (var move in GeneratePseudoLegalMoves(board, from))
                        if (move.To == square) return true;
                }
            }

            return false;
        }

        public static List<Move> GenerateLegalMoves(Board board, Square from)
        {
            var piece = board.Get(from);
            var legal = new List<Move>();
            if (piece == null) return legal;

            foreach (var move in GeneratePseudoLegalMoves(board, from))
            {
                var clone = board.Clone();
                clone.MovePiece(move.From, move.To);
                var kingSquare = clone.FindKing(piece.Color);
                var enemyColor = piece.Color == PieceColor.White ? PieceColor.Black : PieceColor.White;
                if (!IsSquareAttacked(clone, kingSquare, enemyColor))
                    legal.Add(move);
            }
            
            return legal;
        }

        public static List<Move> GenerateAllLegalMoves(Board board, PieceColor color)
        {
            var all = new List<Move>();
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                {
                    var piece = board.Squares[f, r];
                    if (piece != null && piece.Color == color)
                        all.AddRange(GenerateLegalMoves(board, new Square(f, r)));
                }

            return all;
        }
    }
}