using System.Collections.Generic;
using Chess.Core;
using Combat.Core;

namespace Combat.Integration
{
    // Bridges a chess capture into a combat encounter. For now this runs the
    // whole encounter headlessly - no scene, no animation, no manual 3x3
    // team-selection UI (Section 3/5 of the design doc) - so the turn-order
    // and basic-attack loop can be validated against real board pieces before
    // any of that UI exists. Attacker/defender here means "initiated the
    // capture" vs "was captured," not chess color.
    public static class CaptureCombatResolver
    {
        public static bool ResolveCapture(Board board, Square attackerSquare, Square defenderSquare)
        {
            var attackerPiece = board.Get(attackerSquare);
            var defenderPiece = board.Get(defenderSquare);

            var attackerTeam = new List<CombatUnit> { PieceCombatFactory.Create(attackerPiece, CombatTeam.Attacker )};
            var defenderTeam = new List<CombatUnit> { PieceCombatFactory.Create(defenderPiece, CombatTeam.Defender )};

            var combat = new CombatState();
            combat.Setup(attackerTeam, defenderTeam);

            while (combat.Outcome == CombatOutcome.None)
                combat.RunNextTurn();

            return combat.Outcome == CombatOutcome.AttackerWon;
        }
    }
}