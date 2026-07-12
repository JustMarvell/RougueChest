using Chess.Core;
using Combat.Core;
using UnityEngine;

namespace Combat.View
{
    // Listens to CombatState's existing combat-log events and spawns floating
    // text above the relevant actor. Purely visual, same as CombatStageController's
    // tween reactions - CombatState has no idea this exists.
    public class DamageNumberSpawner : MonoBehaviour
    {
        public CombatStageController stage;

        static readonly Color DamageColor = new Color(1f, 0.9f, 0.85f);
        static readonly Color HealColor = new Color(0.4f, 1f, 0.5f);
        static readonly Color ReactionColor = new Color(1f, 0.85f, 0.2f);

        CombatState state;

        public void Bind(CombatState combatState)
        {
            state = combatState;
            state.OnDamageDealt += HandleDamage;
            state.OnHealDealt += HandleHeal;
            state.OnReactionTriggered += HandleReaction; 
        }

        public void Unbind()
        {
            if (state == null) return;
            state.OnDamageDealt -= HandleDamage;
            state.OnHealDealt -= HandleHeal;
            state.OnReactionTriggered -= HandleReaction;
            state = null;
        }

        void HandleDamage(CombatUnit source, CombatUnit target, int amount)
        {
            var pos = stage.GetActorPosition(target);
            if (pos == null) return;
            DamageNumberPopup.Spawn(pos.Value + Vector3.up * 1.6f, amount.ToString(), DamageColor);
        }

        void HandleHeal(CombatUnit source, CombatUnit target, int amount)
        {
            var pos = stage.GetActorPosition(target);
            if (pos == null) return;
            DamageNumberPopup.Spawn(pos.Value + Vector3.up * 1.6f, $"+{amount}", HealColor, fontSize: 2.6f);
        }

        void HandleReaction(CombatUnit source, CombatUnit target, ReactionType reaction)
        {
            if (reaction == ReactionType.None) return;
            var pos = stage.GetActorPosition(target);
            if (pos == null) return;
            DamageNumberPopup.Spawn(pos.Value + Vector3.up * 2.1f, $"{reaction}!", ReactionColor, fontSize: 3.6f);
        }
    }
}