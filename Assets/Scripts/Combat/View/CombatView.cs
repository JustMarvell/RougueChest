using System.Collections.Generic;
using Chess.Core;
using Combat.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Combat.View
{
    public class CombatView : MonoBehaviour
    {
        [Header("Turn Order Rail")]
        public Transform turnRailContainer;
        public CombatUnitBarEntry turnRailEntryPrefab;
        public int turnRailPreviewCount = 6;

        [Header("Team HP Bars")]
        public Transform attackerBarContainer;
        public Transform defenderBarContainer;
        public CombatUnitBarEntry teamEntryPrefab;

        [Header("Action UI")]
        public Button basicAttackButton;
        public TextMeshProUGUI promptLabel;

        CombatState state;
        PlayerDecisionProvider attackerProvider;
        PlayerDecisionProvider defenderProvider;

        readonly Dictionary<CombatUnit, CombatUnitBarEntry> unitEntries = new();
        readonly List<CombatUnitBarEntry> railEntries = new();

        CombatUnit currentActor;
        PlayerDecisionProvider pendingProvider;
        bool awaitingTarget;

        public void Bind(CombatState combatState, PlayerDecisionProvider attacker, PlayerDecisionProvider defender)
        {
            state = combatState;
            attackerProvider = attacker;
            defenderProvider = defender;

            state.OnUnitTurnStart += HandleUnitTurnStart;
            state.OnDamageDealt += HandleDamageDealt;
            state.OnUnitDefeated += HandleUnitDefeated;
            state.OnCombatEnd += HandleCombatEnd;

            attackerProvider.OnDecisionNeeded += HandleDecisionNeeded;
            defenderProvider.OnDecisionNeeded += HandleDecisionNeeded;

            BuildTeamBars(state.AttackerTeam, attackerBarContainer);
            BuildTeamBars(state.DefenderTeam, defenderBarContainer);

            basicAttackButton.onClick.AddListener(OnBasicAttackClicked);
            SetActionUIVisible(false);
        }

        void BuildTeamBars(List<CombatUnit> team, Transform container)
        {
            foreach (var unit in team)
            {
                var entry = Instantiate(teamEntryPrefab, container);
                entry.Bind(unit);
                entry.OnClicked += HandleUnitEntryClicked;
                unitEntries[unit] = entry;
            }
        }

        void HandleUnitTurnStart(CombatUnit actor)
        {
            currentActor = actor;
            RefreshTurnRail();

            foreach (var kv in unitEntries)
                kv.Value.SetHighlighted(kv.Key == actor);

            // Only show the action bar if a player provider owns this actor;
            // an AI provider (future) would decide instantly and never hit this.

            var provider = actor.Team == CombatTeam.Attacker ? attackerProvider : defenderProvider;
            if (provider.IsAwaitingInput)
            {
                pendingProvider = provider;
                SetActionUIVisible(true);
                SetPrompt($"{actor.Name}'s Turn - choose an action");
            }
        }

        void RefreshTurnRail()
        {
            foreach (var e in railEntries) Destroy(e.gameObject);
            railEntries.Clear();

            foreach (var unit in state.TurnOrder.PreviewUpcoming(turnRailPreviewCount))
            {
                var entry = Instantiate(turnRailEntryPrefab, turnRailContainer);
                entry.Bind(unit);
                entry.SetTargetable(false); // rail is read-only, never clickable
                railEntries.Add(entry);
            }
        }

        void OnBasicAttackClicked()
        {
            if (pendingProvider == null || currentActor == null) return;

            awaitingTarget = true;
            SetPrompt("Select a Target");

            var enemies = CombatTargeting.GetLivingEnemies(currentActor, state);
            foreach (var enemy in enemies)
                unitEntries[enemy].SetTargetable(true);
        }

        void HandleUnitEntryClicked(CombatUnit clicked)
        {
            if (!awaitingTarget || pendingProvider == null || clicked.IsDefeated) return;

            ClearTargetableState();
            awaitingTarget = false;
            SetActionUIVisible(false);

            var provider = pendingProvider;
            pendingProvider = null;
            provider.SubmitAction(CombatAction.BasicAttack(clicked));
        }

        void HandleDecisionNeeded(CombatUnit actor, CombatState _)
        {
            // Already handled via HandleUnitTurnStart's provider check; kept
            // as a separate hook point since Skill/Ultimate flows will likely
            // need their own entry here later without touching turn-start logic.
        }

        void HandleDamageDealt(CombatUnit attacker, CombatUnit target, int amount)
        {
            if (unitEntries.TryGetValue(target, out var entry))
                entry.RefreshHP();
        }

        void HandleUnitDefeated(CombatUnit unit)
        {
            if (unitEntries.TryGetValue(unit, out var entry))
            {
                entry.RefreshHP();
                entry.SetTargetable(false);
            }
        }

        void HandleCombatEnd(CombatOutcome outcome)
        {
            SetActionUIVisible(false);
            SetPrompt(outcome == CombatOutcome.AttackerWon ? "Attacker Wins!" : "Defender Wins!");
        }

        void ClearTargetableState()
        {
            foreach (var kv in unitEntries)
                kv.Value.SetTargetable(false);
        }

        void SetActionUIVisible(bool visible)
        {
            if (basicAttackButton != null) basicAttackButton.gameObject.SetActive(visible);
        }

        void SetPrompt(string text)
        {
            if (promptLabel != null) promptLabel.text = text;
        }
    }
}