using System.Collections.Generic;
using System.Linq;
using Chess.Core;

namespace Combat.Core
{
    public static class CombatTargeting
    {
        public static List<CombatUnit> GetEnemyTeam(CombatUnit actor, CombatState state) =>
            actor.Team == CombatTeam.Attacker ? state.DefenderTeam : state.AttackerTeam;

        public static List<CombatUnit> GetLivingEnemies(CombatUnit actor, CombatState state) =>
            GetEnemyTeam(actor, state).Where(u => !u.IsDefeated).ToList();

        public static List<CombatUnit> GetAllyTeam(CombatUnit actor, CombatState state) =>
            actor.Team == CombatTeam.Attacker ? state.AttackerTeam : state.DefenderTeam;

        public static List<CombatUnit> GetLivingAllies(CombatUnit actor, CombatState state) =>
            GetAllyTeam(actor, state).Where(u => !u.IsDefeated).ToList();
    }
}