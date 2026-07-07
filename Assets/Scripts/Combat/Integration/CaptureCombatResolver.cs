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
        public static void ResolveCapture(
            Board board,
            List<Square> attackerSquares,
            List<Square> defenderSquares,
            ICombatDecisionProvider attackerProvider,
            ICombatDecisionProvider defenderProvider,
            Action<bool> onResolved)
        {
            var attackerTeam = BuildTeam(board, attackerSquares, CombatTeam.Attacker);
            var defenderTeam = BuildTeam(board, defenderSquares, CombatTeam.Defender);

            var combat = new CombatState();
            combat.OnCombatEnd += outcome => onResolved(outcome == CombatOutcome.AttackerWon);
            combat.Setup(attackerTeam, defenderTeam, attackerProvider, defenderProvider);
            combat.Begin();
        }

        public static void ResolveCapture(
            Board board,
            CaptureTeamSelection selection,
            ICombatDecisionProvider attackerProvider,
            ICombatDecisionProvider defenderProvider,
            Action<bool> onResolved)
        {
            var (attackerSquares, defenderSquares) = selection.GetTeams();
            ResolveCapture(board, attackerSquares, defenderSquares, attackerProvider, defenderProvider, onResolved);
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

        public static CombatState PrepareCombat(
            Board board,
            CaptureTeamSelection selection,
            ICombatDecisionProvider attackerProvider,
            ICombatDecisionProvider defenderProvider,
            Action<bool> onResolved)
        {
            var (attackerSquares, defenderSquares) = selection.GetTeams();
            var attackerTeam = BuildTeam(board, attackerSquares, CombatTeam.Attacker);
            var defenderTeam = BuildTeam(board, defenderSquares, CombatTeam.Defender);
            
            var combat = new CombatState();
            combat.OnCombatEnd += outcome => onResolved(outcome == CombatOutcome.AttackerWon);
            combat.Setup(attackerTeam, defenderTeam, attackerProvider, defenderProvider);
            return combat; // caller binds UI, then calls combat.Begin()
        }
    }
}