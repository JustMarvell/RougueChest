using Chess.Core;
using UnityEngine;

namespace Combat.Core
{
    public enum ElementType
    {
        Gambit, // fire
        Tempo, // Water
        Fortress, // Earth
        Blitz, // Wind
        Zugzwang // Ice
    }

    public enum ReactionType
    {
        None,
        Check,
        Endgame,
        EnPassant,
        Stalemate,
        Castling
    }

    // A single active elemental aura on a unit. Only one tag can be active
    // at a time (per the design doc's "core 5 reactions" baseline scope) -
    // a same-element hit refreshes duration; a different-element hit reacts
    // and consumes it.
    public class ElementTag
    {
        public ElementType Element;
        public int RemainingTurns;

        public ElementTag(ElementType element, int duration)
        {
            Element = element;
            RemainingTurns = duration;
        }
    }

    // Damage-over-time from the Endgame (Gambit+Zugzwang) reaction. Ticks on
    // the AFFLICTED unit's own turn - same convention as IsFrozen.
    public class DotInstance
    {
        public int DamagePerTick;
        public int RemainingTicks;
        public CombatUnit Source;
    }

    public struct ReactionOutcome
    {
        public ReactionType Type;
        public bool TagConsumend;
    }

    // Central place resolving "unit already tagged with X gets hit by
    // element Y" into one of the 5 named reactions, then applying that
    // reaction's mechanical side effect. Keeps ElementalDamageEffect thin.
    public static class ElementalReactionResolver
    {
        public const int DefaultTagDuration = 2; // in the tagged unit's own turns

        public static ReactionOutcome Resolve(CombatUnit target, ElementType incoming)
        {
            var existing = target.ActiveTag;
            if (existing == null)
                return new ReactionOutcome { Type = ReactionType.None, TagConsumend = false };
            
            if (existing.Element == incoming)
                return new ReactionOutcome { Type = ReactionType.None, TagConsumend = false }; // same element - refresh only

            return new ReactionOutcome { Type = ClassifyPair(existing.Element, incoming), TagConsumend = true };
        }

        // Exhaustive over all 5 elements: the 3 specific pairs first, then
        // Blitz/Fortress's "+ any" fallbacks. Doc lists En Passant before
        // Castling, so Blitz wins if a pair could match both (Blitz+Fortress).
        static ReactionType ClassifyPair(ElementType a, ElementType b)
        {
            bool Has(ElementType e) => a == e || b == e;
            
            if (Has(ElementType.Gambit) && Has(ElementType.Tempo)) return ReactionType.Check;
            if (Has(ElementType.Gambit) && Has(ElementType.Zugzwang)) return ReactionType.Endgame;
            if (Has(ElementType.Zugzwang) && Has(ElementType.Tempo)) return ReactionType.Stalemate;
            if (Has(ElementType.Blitz)) return ReactionType.EnPassant;
            if (Has(ElementType.Fortress)) return ReactionType.Castling;
            return ReactionType.None;
        }

        // Check ("increased damage") applies to the triggering hit itself.
        public static float GetDamageMultiplier(ReactionType reaction) =>
            reaction == ReactionType.Check ? 1.5f : 1f;

        // Everything besides the triggering hit's own damage: DoT, freeze,
        // splash, shield.
        public static void ApplyReactionEffects(CombatUnit caster, CombatUnit target, ReactionType reaction, CombatState state)
        {
            switch (reaction)
            {
                case ReactionType.Endgame:
                    target.ActiveDots.Add(new DotInstance
                    {
                        DamagePerTick = Mathf.RoundToInt(caster.Attack * 0.3f),
                        RemainingTicks = 3,
                        Source = caster
                    });
                    break;
                case ReactionType.Stalemate:
                    target.IsFrozen = true; // consumed by CombatState.AdvanceTurn's existing freeze-skip
                    break;
                case ReactionType.EnPassant:
                    int splash = Mathf.RoundToInt(caster.Attack * 0.5f);
                    foreach (var enemy in CombatTargeting.GetEnemyTeam(caster, state))
                    {
                        if (enemy == target || enemy.IsDefeated) continue;
                        enemy.TakeDamage(splash);
                        state.RaiseDamageDealt(caster, enemy, splash);
                    }
                    break;
                case ReactionType.Castling:
                    var ally = FindNextAllyInTurnOrder(target, state);
                    if (ally != null)
                        ally.Shield += Mathf.RoundToInt(caster.Attack * 0.8f);
                        break;
            }
        }

        static CombatUnit FindNextAllyInTurnOrder(CombatUnit reactingUnit, CombatState state)
        {
            var allyTeam = CombatTargeting.GetAllyTeam(reactingUnit, state);
            foreach (var unit in state.TurnOrder.PreviewUpcoming(allyTeam.Count + 1))
                if (unit != reactingUnit && allyTeam.Contains(unit) && !unit.IsDefeated)
                    return unit;
            return null;
        }

        // Applies a fresh tag, refreshes a same-element tag, or clears it
        // entirely if a reaction just consumed it.
        public static void ApplyOrRefreshTag(CombatUnit target, ElementType incoming, ReactionOutcome outcome)
        {
            if (outcome.TagConsumend)
            {
                target.ActiveTag = null;
                return;
            }

            if (target.ActiveTag != null && target.ActiveTag.Element == incoming)
                target.ActiveTag.RemainingTurns = DefaultTagDuration;
            else
                target.ActiveTag = new  ElementTag(incoming, DefaultTagDuration);
        }

        // Placeholder colors for future UI (weakness icons / aura rings) -
        // no art assets needed yet, just keeps the mapping in one place.
        public static Color GetElementColor(ElementType e) => e switch
        {
            ElementType.Gambit => new Color(1f, 0.35f ,0.15f),
            ElementType.Tempo => new Color(0.2f, 0.55f, 1f),
            ElementType.Fortress => new Color(0.55f, 0.4f, 0.2f),
            ElementType.Blitz => new Color(0.4f, 1f, 0.6f),
            ElementType.Zugzwang => new Color(0.6f, 0.85f, 1f),
            _ => Color.white
        };
    }
}