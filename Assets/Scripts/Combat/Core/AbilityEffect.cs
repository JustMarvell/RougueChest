using System;
using System.Collections.Generic;
using Chess.Core;
using UnityEngine;

namespace Combat.Core
{
    public enum ActionKind { Basic, Skill, Ultimate }

    public enum TargetType
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        AllAllies,
        Self
    }

    // One discrete piece of what an ability does. An AbilityDefinition holds
    // a list of these so a single skill can e.g. deal damage AND apply a
    // speed change in one cast. [SerializeReference] on the holding list is
    // what lets this be polymorphic in the Inspector without a ScriptableObject
    // per effect.
    [Serializable]
    public abstract class AbilityEffect
    {
        public abstract void Apply(CombatUnit caster, List<CombatUnit> targets, CombatState state);
    }

    [Serializable]
    public class DamageEffect : AbilityEffect
    {
        public float AttackMultiplier = 1.0f;

        public override void Apply(CombatUnit caster, List<CombatUnit> targets, CombatState state)
        {
            foreach (var target in targets)
            {
                if (target == null || target.IsDefeated) continue;
                int dmg = Mathf.RoundToInt(caster.Attack * AttackMultiplier);
                target.TakeDamage(dmg);
                state.RaiseDamageDealt(caster, target, dmg);
            }
        }
    }

    [Serializable]
    public class HealEffect : AbilityEffect
    {
        public int FlatAmount = 0;
        public float MaxHPPercent = 0f; // Additive with FlatAmount

        public override void Apply(CombatUnit caster, List<CombatUnit> targets, CombatState state)
        {
            foreach (var target in targets)
            {
                if (target == null || target.IsDefeated) continue;
                int amount = FlatAmount + Mathf.RoundToInt(target.MaxHP * MaxHPPercent);
                target.Heal(amount);
                state.RaiseHealDealt(caster, target, amount); 
            }
        }
    }

    // Knight's "increase SPD" skill - maps directly onto the rescaling
    // TurnOrderService already supports.
    [Serializable]
    public class SpeedChangeEffect : AbilityEffect
    {
        public int Delta = 0;
        public override void Apply(CombatUnit caster, List<CombatUnit> targets, CombatState state)
        {
            foreach (var target in targets)
            {
                if (target == null || target.IsDefeated) continue;
                int previous = target.Speed;
                target.Speed = Mathf.Max(1, target.Speed + Delta);
                state.TurnOrder.ApplySpeedChange(target, previous);
            }
        }
    }

    // Knight's "Advance Forward" style ultimate - maps directly onto
    // TurnOrderService.AdvanceTurn, which already existed as a placeholder.
    [Serializable]
    public class AdvanceTurnEffect : AbilityEffect
    {
        [Range(0f, 1f)] public float Percent = 1f;

        public override void Apply(CombatUnit caster, List<CombatUnit> targets, CombatState state)
        {
            foreach (var target in targets)
            {
                if (target == null || target.IsDefeated) continue;
                state.TurnOrder.AdvanceTurn(target, Percent);
            }
        }
    }

    [Serializable]
    public class ElementalDamageEffect : AbilityEffect
    {
        public float  AttackMultiplier = 1.0f;
        public ElementType Element;

        public override void Apply(CombatUnit caster, List<CombatUnit> targets, CombatState state)
        {
            foreach (var target in targets)
            {
                if (target == null || target.IsDefeated) continue;

                var outcome = ElementalReactionResolver.Resolve(target, Element);
                float multiplier = AttackMultiplier * ElementalReactionResolver.GetDamageMultiplier(outcome.Type);

                int dmg = Mathf.RoundToInt(caster.Attack * multiplier);
                target.TakeDamage(dmg);
                state.RaiseDamageDealt(caster, target, dmg);

                if (outcome.Type != ReactionType.None)
                    state.RaiseReactionTriggered(caster, target, outcome.Type);

                ElementalReactionResolver.ApplyReactionEffects(caster, target, outcome.Type, state);
                ElementalReactionResolver.ApplyOrRefreshTag(target, Element, outcome);
            }
        }
    }
}