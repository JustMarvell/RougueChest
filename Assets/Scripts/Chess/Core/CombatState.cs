using System;
using System.Collections.Generic;
using System.Linq;
using Chess.Core;
using NUnit.Framework;
using UnityEngine.Rendering;

namespace Combat.Core
{
    public enum CombatOutcome
    {
        None,
        AttackerWon,
        DefenderWon
    }

    // Mirrors Chess.Core.GameState: owns the canonical combat state and fires
    // events for the View layer to react to. Basic-attack only for now - no
    // skills, ultimates, elements, or cards. Once this loop is solid, those
    // become content layered on top rather than changes to this structure.
    public class CombatState
    {
        public readonly TurnOrderService TurnOrder = new TurnOrderService();
        public List<CombatUnit> AttackerTeam = new List<CombatUnit>();
        public List<CombatUnit> DefenderTeam = new List<CombatUnit>();
        public CombatOutcome Outcome = CombatOutcome.None;

        public event Action<CombatUnit> OnUnitTurnStart;
        public event Action<CombatUnit, CombatUnit, int> OnDamageDealt; // attacker, target, amount
        public event Action<CombatUnit> OnUnitDefeated;
        public event Action<CombatOutcome> OnCombatEnd;

        public void Setup(List<CombatUnit> attackers, List<CombatUnit> defenders)
        {
            AttackerTeam = attackers;
            DefenderTeam = defenders;
            Outcome = CombatOutcome.None;

            foreach (var u in AttackerTeam) TurnOrder.Register(u);
            foreach (var u in DefenderTeam) TurnOrder.Register(u);
        }

        // Advances combat by exactly one unit's action. The view layer (or a
        // test) calls this in a loop, one call per turn, so animation/pacing
        // can sit between calls without the core needing to know about time.
        public void RunNextTurn()
        {
            if (Outcome != CombatOutcome.None) return;

            CombatUnit actor;
            while (true)
            {
                actor = TurnOrder.PopNextActor();
                if (actor == null) return; // shouldn't happen if CheckForWinner runs after every action

                if (actor.IsFrozen)
                {
                    // Stalemate (Zugzwang + Tempo): consume the freeze and skip
                    // this action entirely. PopNextActor already rescheduled
                    // this unit's next AV interval, so "skipping" costs nothing
                    // extra here - we just loop and pop whoever's next.
                    actor.IsFrozen = false;
                    continue;
                }

                break;
            }

            OnUnitTurnStart?.Invoke(actor);

            var target = PickTarget(actor);
            if (target == null) { CheckForWinner(); return; }

            int damage = actor.Attack; // baseline - no crit/elements/skills yet
            target.TakeDamage(damage);
            OnDamageDealt?.Invoke(actor, target, damage);

            if (target.IsDefeated)
            {
                OnUnitDefeated?.Invoke(target);
                TurnOrder.Remove(target);
            }

            CheckForWinner();
        }

        // Simplest possible targeting: first living enemy. Fine for proving
        // the loop; real targeting (lowest HP, taunt/shield redirects, AoE)
        // comes with skills later.
        CombatUnit PickTarget(CombatUnit actor)
        {
            var enemyTeam = actor.Team == CombatTeam.Attacker ? DefenderTeam : AttackerTeam;
            return enemyTeam.FirstOrDefault(u => !u.IsDefeated);
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