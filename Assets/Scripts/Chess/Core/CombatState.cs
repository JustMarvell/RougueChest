using System;
using System.Collections.Generic;
using System.Linq;
using Chess.Core;

namespace Combat.Core
{
    public enum CombatOutcome
    {
        None,
        AttackerWon,
        DefenderWon
    }

    public class CombatState
    {
        public readonly TurnOrderService TurnOrder = new TurnOrderService();
        public List<CombatUnit> AttackerTeam = new List<CombatUnit>();
        public List<CombatUnit> DefenderTeam = new List<CombatUnit>();
        public CombatOutcome Outcome = CombatOutcome.None;

        ICombatDecisionProvider attackerProvider;
        ICombatDecisionProvider defenderProvider;

        public event Action<CombatUnit> OnUnitTurnStart;
        public event Action<CombatUnit, CombatUnit, int> OnDamageDealt;
        public event Action<CombatUnit> OnUnitDefeated;
        public event Action<CombatOutcome> OnCombatEnd;

        public void Setup(
            List<CombatUnit> attackers,
            List<CombatUnit> defenders,
            ICombatDecisionProvider attackerProvider,
            ICombatDecisionProvider defenderProvider)
        {
            AttackerTeam = attackers;
            DefenderTeam = defenders;
            this.attackerProvider = attackerProvider;
            this.defenderProvider = defenderProvider;
            Outcome = CombatOutcome.None;

            foreach (var u in AttackerTeam) TurnOrder.Register(u);
            foreach (var u in DefenderTeam) TurnOrder.Register(u);
        }

        // Starts the encounter. From here combat drives itself turn-by-turn
        // through provider callbacks - no external while loop needed (or
        // possible, since a real player's decision can take many frames).
        public void Begin()
        {
            AdvanceTurn();
        }

        void AdvanceTurn()
        {
            if (Outcome != CombatOutcome.None) return;

            CombatUnit actor;
            while (true)
            {
                actor = TurnOrder.PopNextActor();
                if (actor == null) return; // shouldn't happen if CheckForWinner runs after every action

                if (actor.IsFrozen)
                {
                    actor.IsFrozen = false; // Stalemate placeholder: consume freeze, skip this action
                    continue;
                }

                break;
            }

            OnUnitTurnStart?.Invoke(actor);

            var provider = GetProviderFor(actor);
            provider.RequestAction(actor, this, action => ResolveAction(actor, action));
        }

        ICombatDecisionProvider GetProviderFor(CombatUnit actor) =>
            actor.Team == CombatTeam.Attacker ? attackerProvider : defenderProvider;

        // Basic-attack-only resolution for now - Skill/Ultimate branch by
        // action.Type comes once those exist (Section 4 of the design doc).
        void ResolveAction(CombatUnit actor, CombatAction action)
        {
            foreach (var target in action.Targets)
            {
                if (target == null || target.IsDefeated) continue;

                int damage = actor.Attack;
                target.TakeDamage(damage);
                OnDamageDealt?.Invoke(actor, target, damage);

                if (target.IsDefeated)
                {
                    OnUnitDefeated?.Invoke(target);
                    TurnOrder.Remove(target);
                }
            }

            CheckForWinner();
            if (Outcome == CombatOutcome.None)
                AdvanceTurn();
        }

        void CheckForWinner()
        {
            bool attackersAlive = AttackerTeam.Any(u => !u.IsDefeated);
            bool defendersAlive = DefenderTeam.Any(u => !u.IsDefeated);

            if (!attackersAlive)
            {
                Outcome = CombatOutcome.DefenderWon;
                OnCombatEnd?.Invoke(Outcome);
            }
            else if (!defendersAlive)
            {
                Outcome = CombatOutcome.AttackerWon;
                OnCombatEnd?.Invoke(Outcome);
            }
        }
    }
}