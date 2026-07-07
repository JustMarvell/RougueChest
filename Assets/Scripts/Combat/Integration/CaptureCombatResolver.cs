using System;
using System.Collections.Generic;
using Chess.Core;
using Combat.Core;
using Combat.Integration;
using Combat.Selection;

namespace Chess.Integration
{
    public static class CaptureCombatResolver
    {
        public static CombatState PrepareCombat(
            GameState gameState,
            CaptureTeamSelection selection,
            ICombatDecisionProvider attackerProvider,
            ICombatDecisionProvider defenderProvider,
            Action<bool> onResolved)
        {
            var (attackerSquares, defenderSquares) = selection.GetTeams();
            var attackerTeam = BuildTeam(gameState.Board, attackerSquares, CombatTeam.Attacker);
            var defenderTeam = BuildTeam(gameState.Board, defenderSquares, CombatTeam.Defender);

            var attackerSP = gameState.GetSPPool(selection.AttackerColor);
            var defenderSP = gameState.GetSPPool(selection.DefenderColor);

            var combat = new CombatState();
            combat.OnCombatEnd += outcome => onResolved(outcome == CombatOutcome.AttackerWon);
            combat.Setup(attackerTeam, defenderTeam, attackerSP, defenderSP, attackerProvider, defenderProvider);
            return combat; // caller binds UI, then calls combat.Begin()
        }

        static List<CombatUnit> BuildTeam(Board board, List<Square> squares, CombatTeam team)
        {
            var units = new List<CombatUnit>();
            foreach (var sq in squares)
            {
                var piece = board.Get(sq);
                if (piece == null) continue;
                units.Add(PieceCombatFactory.Create(piece, team));
            }

            return units;
        }
    }
}