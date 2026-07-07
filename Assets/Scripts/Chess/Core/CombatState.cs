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

        // References, NOT owned instances - these are the SAME SkillPointPool
        // objects that live on GameState (per PieceColor), passed in at Setup.
        // Whatever Current ends at when combat finishes is automatically what
        // the next encounter starts with - no explicit save/carry-over step.
        public SkillPointPool AttackerSP { get; private set; }
        public SkillPointPool DefenderSP { get; private set; }

        ICombatDecisionProvider attackerProvider;
        ICombatDecisionProvider defenderProvider;

        readonly HashSet<CombatUnit> announcedDefeats = new HashSet<CombatUnit>();

        public event Action<CombatUnit> OnUnitTurnStart;
        public event Action<CombatUnit, CombatUnit, int> OnDamageDealt;
        public event Action<CombatUnit, CombatUnit, int> OnHealDealt;
        public event Action<CombatUnit> OnUnitDefeated;
        public event Action<CombatOutcome> OnCombatEnd;

        public void Setup(
            List<CombatUnit> attackers,
            List<CombatUnit> defenders,
            SkillPointPool attackerSP,
            SkillPointPool defenderSP,
            ICombatDecisionProvider attackerProvider,
            ICombatDecisionProvider defenderProvider)
        {
            AttackerTeam = attackers;
            DefenderTeam = defenders;
            AttackerSP = attackerSP;
            DefenderSP = defenderSP;
            this.attackerProvider = attackerProvider;
            this.defenderProvider = defenderProvider;
            Outcome = CombatOutcome.None;

            foreach (var u in AttackerTeam) TurnOrder.Register(u);
            foreach (var u in DefenderTeam) TurnOrder.Register(u);
        }

        public SkillPointPool GetSPPool(CombatTeam team) => team == CombatTeam.Attacker ? AttackerSP : DefenderSP;

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

        // Data-driven now: reads the Ability's Kind for SP handling, then
        // runs every effect the ability carries. Damage/Heal/SpeedChange/
        // AdvanceTurn are all just AbilityEffect implementations - CombatState
        // no longer needs to know what a skill "does".
        void ResolveAction(CombatUnit actor, CombatAction action)
        {
            var ability = action.Ability;
            var spPool = GetSPPool(actor.Team);

            if (ability.Kind == ActionKind.Skill)
                spPool.TrySpend(ability.SPCost); // UI is expected to have already validated affordability before submitting
            else if (ability.Kind == ActionKind.Basic)
                spPool.Generate();

            actor.GainEnergy(ability.SelfEnergyGain);

            foreach (var effect in ability.Effects)
                effect.Apply(actor, action.Targets, this);

            AnnounceNewlyDefeated();

            CheckForWinner();
            if (Outcome == CombatOutcome.None)
                AdvanceTurn();
        }

        // Public hooks effects use to fire the shared events, since the
        // events themselves are only invocable from within CombatState.
        public void RaiseDamageDealt(CombatUnit source, CombatUnit target, int amount) => OnDamageDealt?.Invoke(source, target, amount);
        public void RaiseHealDealt(CombatUnit source, CombatUnit target, int amount) => OnHealDealt?.Invoke(source, target, amount);

        void AnnounceNewlyDefeated()
        {
            foreach (var u in AttackerTeam) TryAnnounceDefeat(u);
            foreach (var u in DefenderTeam) TryAnnounceDefeat(u);
        }

        void TryAnnounceDefeat(CombatUnit u)
        {
            if (!u.IsDefeated || announcedDefeats.Contains(u)) return;
            announcedDefeats.Add(u);
            OnUnitDefeated?.Invoke(u);
            TurnOrder.Remove(u);
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