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
        public Button skillButton;
        public Button ultimateButton;
        public CombatStageController combatStage;       // optional; drives 3D ring highlights alongside the 2D bars

        public bool IsAwaitingTarget => awaitingTarget;

        // Optional - if left unassigned, CombatView just skips updating them.
        // Lets you wire these up incrementally in the Inspector without
        // breaking anything that's already working.
        public TextMeshProUGUI skillButtonLabel;
        public TextMeshProUGUI ultimateButtonLabel;
        public TextMeshProUGUI spLabel;
        public TextMeshProUGUI promptLabel;

        CombatState state;
        PlayerDecisionProvider attackerProvider;
        PlayerDecisionProvider defenderProvider;

        readonly Dictionary<CombatUnit, CombatUnitBarEntry> unitEntries = new();
        readonly List<CombatUnitBarEntry> railEntries = new();

        CombatUnit currentActor;
        PlayerDecisionProvider pendingProvider;
        AbilityDefinition pendingAbility;
        bool awaitingTarget;

        // entry point for 3D raycast clicks; funnels into the same
        // validation/confirm path as clicking a CombatUnitBarEntry.
        public void TrySelectTarget(CombatUnit clicked) => HandleUnitEntryClicked(clicked);

        void BeginTargeting(AbilityDefinition ability)
        {
            pendingAbility = ability;

            switch (ability.TargetType)
            {
                case TargetType.SingleEnemy:
                    awaitingTarget = true;
                    SetPrompt($"{currentActor.Name}: Select A Target");
                    foreach (var enemy in CombatTargeting.GetLivingEnemies(currentActor, state))
                    {
                        unitEntries[enemy].SetTargetable(true);
                        combatStage?.SetTargetable(enemy, true);
                    }
                    break;
                case TargetType.AllEnemies:
                    ConfirmAction(ability, CombatTargeting.GetLivingEnemies(currentActor, state));
                    break;
                case TargetType.AllAllies:
                    ConfirmAction(ability, CombatTargeting.GetLivingAllies(currentActor, state));
                    break;
                case TargetType.Self:
                    ConfirmAction(ability, new List<CombatUnit> { currentActor });
                    break;
            }
        }

        void ClearTargetableState()
        {
            foreach (var kv in unitEntries)
                kv.Value.SetTargetable(false);
            combatStage?.ClearAllTargetable();
        }

        public void Bind(CombatState combatState, PlayerDecisionProvider attacker, PlayerDecisionProvider defender)
        {
            ClearPreviousCombat();

            state = combatState;
            attackerProvider = attacker;
            defenderProvider = defender;

            state.OnUnitTurnStart += HandleUnitTurnStart;
            state.OnDamageDealt += HandleDamageDealt;
            state.OnHealDealt += HandleHealDealt;
            state.OnUnitDefeated += HandleUnitDefeated;
            state.OnCombatEnd += HandleCombatEnd;

            attackerProvider.OnDecisionNeeded += HandleDecisionNeeded;
            defenderProvider.OnDecisionNeeded += HandleDecisionNeeded;

            BuildTeamBars(state.AttackerTeam, attackerBarContainer);
            BuildTeamBars(state.DefenderTeam, defenderBarContainer);

            if (basicAttackButton != null) basicAttackButton.onClick.AddListener(OnBasicAttackClicked);
            if (skillButton != null) skillButton.onClick.AddListener(OnSkillClicked);
            if (ultimateButton != null) ultimateButton.onClick.AddListener(OnUltimateClicked);

            SetActionUIVisible(false);
        }

        void ClearPreviousCombat()
        {
            foreach (var kv in unitEntries)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            unitEntries.Clear();

            foreach (var e in railEntries)
                if (e != null) Destroy(e.gameObject);
            railEntries.Clear();
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
            RefreshActionButtons();

            foreach (var kv in unitEntries)
                kv.Value.SetHighlighted(kv.Key == actor);
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

        // ---- Action buttons ----

        void OnBasicAttackClicked()
        {
            if (pendingProvider == null || currentActor?.Kit?.Basic == null) return;
            BeginTargeting(currentActor.Kit.Basic);
        }

        void OnSkillClicked()
        {
            if (pendingProvider == null || currentActor?.Kit?.Skill == null) return;

            var ability = currentActor.Kit.Skill;
            var sp = state.GetSPPool(currentActor.Team);
            if (!sp.CanAfford(ability.SPCost))
            {
                SetPrompt("Not enough Skill Points");
                return;
            }

            BeginTargeting(ability);
        }

        void OnUltimateClicked()
        {
            if (pendingProvider == null || currentActor?.Kit?.Ultimate == null) return;

            if (!currentActor.UltimateReady)
            {
                SetPrompt("Ultimate not ready yet");
                return;
            }

            BeginTargeting(currentActor.Kit.Ultimate);
        }

        void HandleUnitEntryClicked(CombatUnit clicked)
        {
            if (!awaitingTarget || pendingAbility == null || clicked.IsDefeated) return;
            ConfirmAction(pendingAbility, new List<CombatUnit> { clicked });
        }

        void ConfirmAction(AbilityDefinition ability, List<CombatUnit> targets)
        {
            if (pendingProvider == null) return;

            ClearTargetableState();
            awaitingTarget = false;
            pendingAbility = null;
            SetActionUIVisible(false);

            var provider = pendingProvider;
            pendingProvider = null;
            provider.SubmitAction(new CombatAction { Ability = ability, Targets = targets });
        }

        void HandleDecisionNeeded(CombatUnit actor, CombatState _)
        {
            var provider = actor.Team == CombatTeam.Attacker ? attackerProvider : defenderProvider;
            pendingProvider = provider;
            RefreshActionButtons();
            SetActionUIVisible(true);
            SetPrompt($"{actor.Name}'s turn - choose an action");
        }

        // ---- Event handlers ----

        void HandleDamageDealt(CombatUnit attacker, CombatUnit target, int amount)
        {
            if (unitEntries.TryGetValue(target, out var entry))
                entry.RefreshHP();
        }

        void HandleHealDealt(CombatUnit healer, CombatUnit target, int amount)
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

        // ---- UI helpers ----

        // Updates interactability + labels for all three buttons based on
        // the current actor's kit, SP pool, and Energy - called whenever the
        // acting unit changes or a decision is about to be requested.
        void RefreshActionButtons()
        {
            if (currentActor == null) return;

            var kit = currentActor.Kit;
            var sp = state.GetSPPool(currentActor.Team);

            if (basicAttackButton != null)
                basicAttackButton.interactable = kit?.Basic != null;

            if (skillButton != null)
            {
                bool canUseSkill = kit?.Skill != null && sp.CanAfford(kit.Skill.SPCost);
                skillButton.interactable = canUseSkill;
                if (skillButtonLabel != null && kit?.Skill != null)
                    skillButtonLabel.text = $"{kit.Skill.DisplayName}\n({kit.Skill.SPCost} SP)";
            }

            if (ultimateButton != null)
            {
                bool canUseUlt = kit?.Ultimate != null && currentActor.UltimateReady;
                ultimateButton.interactable = canUseUlt;
                if (ultimateButtonLabel != null && kit?.Ultimate != null)
                    ultimateButtonLabel.text = $"{kit.Ultimate.DisplayName}\n({currentActor.Energy}/{currentActor.MaxEnergy})";
            }

            if (spLabel != null)
                spLabel.text = $"SP: {sp.Current}/{SkillPointPool.Max}";
        }

        void SetActionUIVisible(bool visible)
        {
            if (basicAttackButton != null) basicAttackButton.gameObject.SetActive(visible);
            if (skillButton != null) skillButton.gameObject.SetActive(visible);
            if (ultimateButton != null) ultimateButton.gameObject.SetActive(visible);
        }

        void SetPrompt(string text)
        {
            if (promptLabel != null) promptLabel.text = text;
        }
    }
}