using System.Collections.Generic;
using Chess.Core;
using Combat.Core;
using Combat.Selection;

namespace Combat.Integration
{
    // Bridges a chess capture into a combat encounter. For now this runs the
    // whole encounter headlessly - no scene, no animation - so the turn-order
    // and combat loop can be validated before the combat scene itself exists.
    // Attacker/defender here means "initiated the capture" vs "was captured,"
    // not chess color.
    public static class CaptureCombatResolver
    {
        public static bool ResolveCapture(Board board, List<Square> attackerSquares, List<Square> defenderSquares)
        {
            var attackerTeam = BuildTeam(board, attackerSquares, CombatTeam.Attacker);
            var defenderTeam = BuildTeam(board, defenderSquares, CombatTeam.Defender);

            var combat = new CombatState();
            combat.Setup(attackerTeam, defenderTeam);

            while (combat.Outcome == CombatOutcome.None)
            {
                combat.RunNextTurn();
            }

            return combat.Outcome == CombatOutcome.AttackerWon;
        }

        // Convenience overload for once a CaptureTeamSelection has finished
        // both picking phases - avoids the caller having to unpack GetTeams()
        // itself. This is the entry point step (2) will call once selection
        // wiring exists.
        public static bool ResolveCapture(Board board, CaptureTeamSelection selection)
        {
            var (attackerSquares, defenderSquares) = selection.GetTeams();
            return ResolveCapture(board, attackerSquares, defenderSquares);
        }

        static List<CombatUnit> BuildTeam(Board board, List<Square> squares, CombatTeam team)
        {
            var units = new List<CombatUnit>();
            foreach (var sq in squares)
            {
                var piece = board.Get(sq);
                if (piece == null) continue; //defensive - board shouldn't change mid-selection, but don't crash if it does
                units.Add(PieceCombatFactory.Create(piece, team));
            }

            return units;
        }
    }
}