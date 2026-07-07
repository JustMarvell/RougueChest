using System;
using Chess.Core;

namespace Combat.Core
{
    // Supplies a decision for a unit's turn. CombatState calls RequestAction
    // and waits - synchronously or across many frames - until onDecided is
    // invoked. A team's provider is assigned at Setup() time, so PvP, PvE,
    // and (later) a networked/server-validated version are all just
    // different implementations of this one interface.
    public interface ICombatDecisionProvider
    {
        void RequestAction(CombatUnit actor, CombatState state, Action<CombatAction> onDecided);
    }
}