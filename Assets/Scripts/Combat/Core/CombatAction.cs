using System.Collections.Generic;
using Chess.Core;

namespace Combat.Core
{
    public enum CombatActionType
    {
        Basic,
        Skill,
        Ultimate
    }

    // What a decision-maker (player input or, later, AI) hands back to
    // CombatState once it's decided what a unit should do this turn.
    public struct CombatAction
    {
        public CombatActionType Type;
        public List<CombatUnit> Targets;

        public static CombatAction BasicAttack(CombatUnit target) => new CombatAction
        {
            Type = CombatActionType.Basic,
            Targets = new List<CombatUnit> { target }
        };
    }
}