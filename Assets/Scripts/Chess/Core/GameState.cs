using System;

namespace Chess.Core
{
    public enum GameStatus { Ongoing, Check, Checkmate, Stalemate, AwaitingCombat }

    public class GameState
    {
        public Board Board;
        public PieceColor SideToMove = PieceColor.White;
        public GameStatus Status = GameStatus.Ongoing;

        public event Action<Square, Square> OnCaptureTriggered;
        public event Action<PieceColor> OnCheckmate;
        public event Action OnStalemate;

        Move pendingCapture;
        bool hasPendingCapture;

        public GameState()
        {
            Board = Board.CreateStandard();
        }

        public bool TryMakeMove(Square from, Square to)
        {
            if (hasPendingCapture) return false;
            var piece = Board.Get(from);
            if (piece == null || piece.Color != SideToMove) return false;

            var legalMoves = MoveGenerator.GenerateLegalMoves(Board, from);
            bool found = false;
            Move chosen = default;
            foreach (var m in legalMoves)
            {
                if (m.To == to) { found = true; chosen = m; break; }
            }
            if (!found) return false;

            if (chosen.IsCapture)
            {
                pendingCapture = chosen;
                hasPendingCapture = true;
                Status = GameStatus.AwaitingCombat;
                OnCaptureTriggered?.Invoke(from, to);
                return true;
            }

            ApplyMove(chosen);
            return true;
        }

        // Called by the combat system once an encounter/battle has resolve
        public void ResolveCapture(bool attackerWon)
        {
            if (!hasPendingCapture) return;
            var move = pendingCapture;
            hasPendingCapture = false;

            if (attackerWon)
                ApplyMove(move);
            else
            {
                Board.Set(move.From, null);
                AdvanceTurn();
            }
        }

        void ApplyMove(Move move)
        {
            Board.MovePiece(move.From, move.To);
            AdvanceTurn();
        }

        void AdvanceTurn()
        {
            SideToMove = SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
            EvaluateStatus();
        }

        void EvaluateStatus()
        {
            var kingSquare = Board.FindKing(SideToMove);
            var enemyColor = SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
            bool inCheck = MoveGenerator.IsSquareAttacked(Board, kingSquare, enemyColor);
            bool hasMoves = MoveGenerator.GenerateAllLegalMoves(Board, SideToMove).Count > 0;

            if (!hasMoves && inCheck)
            {
                Status = GameStatus.Checkmate;
                OnCheckmate?.Invoke(enemyColor);
            }
            else if (!hasMoves)
            {
                Status = GameStatus.Stalemate;
                OnStalemate?.Invoke();
            }
            else
            {
                Status = inCheck ? GameStatus.Check : GameStatus.Ongoing;
            }
        }
    }
}