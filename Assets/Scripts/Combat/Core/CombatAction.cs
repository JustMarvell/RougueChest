using System.Collections.Generic;
using Chess.Core;

namespace Combat.Core
{
    // What a decision-maker (player input or, later, AI) hands back to
    // CombatState once it's decided what a unit should do this turn.
    // ActionKind/SPCost/effects all now live on the Ability itself (data),
    // not on this struct - CombatState reads Ability.Kind to know whether to
    // spend/generate SP, and iterates Ability.Effects to know what to do.
    public struct CombatAction
    {
        public AbilityDefinition Ability;
        public List<CombatUnit> Targets;
    }
}