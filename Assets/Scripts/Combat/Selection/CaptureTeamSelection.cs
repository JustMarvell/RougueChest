using System.Collections.Generic;
using Chess.Core;

namespace Combat.Selection
{
    public enum SelectionPhase
    {
        AttackerPicking,
        DefenderPicking,
        Ready
    }

    // Drives the "pause board, highlight 3x3, manually pick up to 4 more"
    // flow from Section 3/5 of the design doc. Pure logic - the View layer
    // reads CurrentEligible/CurrentPicked to know what to highlight and calls
    // TogglePick/ConfirmCurrentPhase in response to clicks. No knowledge of
    // combat stats or rendering here, same separation as Chess.Core/View.
    public class CaptureTeamSelection
    {
        public const int MaxTeamSize = 5;

        public readonly Square AttackerOrigin;
        public readonly Square DefenderOrigin;
        public readonly PieceColor AttackerColor;
        public readonly PieceColor DefenderColor;

        public List<Square> AttackerEligible { get; }
        public List<Square> DefenderEligible { get; }
        public List<Square> AttackerPicked { get; } = new List<Square>();
        public List<Square> DefenderPicked { get; } = new List<Square>();

        public SelectionPhase Phase { get; private set; } = SelectionPhase.AttackerPicking;

        public List<Square> CurrentEligible => Phase == SelectionPhase.AttackerPicking ? AttackerEligible : DefenderEligible;
        public List<Square> CurrentPicked => Phase == SelectionPhase.AttackerPicking ? AttackerPicked : DefenderPicked;
        public Square CurrentOrigin => Phase == SelectionPhase.AttackerPicking ? AttackerOrigin : DefenderOrigin;

        public CaptureTeamSelection(Board board, Square attackerSquare, Square defenderSquare)
        {
            AttackerOrigin = attackerSquare;
            DefenderOrigin = defenderSquare;
            AttackerColor = board.Get(attackerSquare).Color;
            DefenderColor = board.Get(defenderSquare).Color;

            AttackerEligible = ComputeEligible(board, attackerSquare, AttackerColor);
            DefenderEligible = ComputeEligible(board, defenderSquare, DefenderColor);

            // The piece performing/receiving the capture is always on the team.
            AttackerPicked.Add(attackerSquare);
            DefenderPicked.Add(defenderSquare);
        }

        // 3x3 centered on the origin square, clipped to the board, allied
        // pieces only, excluding the origin itself (it's mandatory, not a pick).
        static List<Square> ComputeEligible(Board board, Square center, PieceColor color)
        {
            var result = new List<Square>();
            for (int df = -1; df <= 1; df++)
            {
                for (int dr = -1; dr <= 1; dr++)
                {
                    if (df == 0 && dr == 0) continue;
                    var sq = new Square(center.File + df, center.Rank + dr);
                    if (!sq.IsValid) continue;

                    var piece = board.Get(sq);
                    if (piece != null && piece.Color == color)
                        result.Add(sq);
                }
            }

            return result;
        }

        // Returns true if the pick state actually changed (view can use this
        // to know whether to re-render, ignore no-op clicks, etc).
        public bool TogglePick(Square square)
        {
            if (Phase == SelectionPhase.Ready) return false;
            if (square == CurrentOrigin) return false; // mandatory piece, not toggleable
            if (!CurrentEligible.Contains(square)) return false;

            var picked = CurrentPicked;
            if (picked.Contains(square))
            {
                picked.Remove(square);
                return true;
            }

            if (picked.Count >= MaxTeamSize) return false; // team is full
            picked.Add(square);
            return true;
        }

        public bool ConfirmCurrentPhase()
        {
            switch (Phase)
            {
                case SelectionPhase.AttackerPicking:
                    Phase = SelectionPhase.DefenderPicking;
                    return true;
                case SelectionPhase.DefenderPicking:
                    Phase = SelectionPhase.Ready;
                    return true;
                default:
                    return false;
            }
        }

        public bool IsReady => Phase == SelectionPhase.Ready;

        // Only meaningfull once IsReady is true
        public (List<Square> attackers, List<Square> defenders) GetTeams()
        {
            return (AttackerPicked, DefenderPicked);
        }
    }
}