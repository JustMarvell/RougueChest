using System.Collections.Generic;
using Chess.Core;
using Chess.View;
using Combat.Core;
using UnityEngine;

namespace Combat.View
{
    public class CombatStageController : MonoBehaviour
    {
        public CombatFormation formation;
        public PieceModelSet whiteModels;
        public PieceModelSet blackModels;
        public float actorScale = 1f;

        CombatState state;
        readonly Dictionary<CombatUnit, CombatUnitActor> actors = new();

        public void Bind(CombatState combatState)
        {
            state = combatState;

            SpawnTeam(state.AttackerTeam, isAttacker: true);
            SpawnTeam(state.DefenderTeam, isAttacker: false);

            state.OnUnitTurnStart += HandleTurnStart;
            state.OnDamageDealt += HandleDamageDealt;
            state.OnUnitDefeated += HandleUnitDefeated;
        }

        public void Unbind()
        {
            if (state == null) return;
            state.OnUnitTurnStart -= HandleTurnStart;
            state.OnDamageDealt -= HandleDamageDealt;
            state.OnUnitDefeated -= HandleUnitDefeated;

            foreach (var kv in actors)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            actors.Clear();
            state = null;
        }

        void SpawnTeam(List<CombatUnit> team, bool isAttacker)
        {
            for (int i = 0; i < team.Count; i++)
            {
                var unit = team[i];
                var modelSet = unit.Color == PieceColor.White ? whiteModels : blackModels;
                var prefab = modelSet?.Get(unit.PieceType);

                var pos = formation.GetSlotPoisition(isAttacker, i, team.Count);
                var rot = formation.GetSLotRotation(isAttacker);

                var actorObj = new GameObject($"Actor_{unit.Name}");
                actorObj.transform.SetParent(transform);
                var actor = actorObj.AddComponent<CombatUnitActor>();
                actor.Bind(unit, prefab, pos, rot, actorScale);

                actors[unit] = actor;
            }
        }

        void HandleTurnStart(CombatUnit actor)
        {
            foreach (var kv in actors)
                kv.Value.SetActingHighlight(kv.Key == actor);
        }

        void HandleDamageDealt(CombatUnit source, CombatUnit target, int amount)
        {
            if (actors.TryGetValue(source, out var sourceActor) && actors.TryGetValue(target, out var targetActor))
            {
                sourceActor.PlayAttack(targetActor.transform.position);
                targetActor.PlayHitReaction();
            }
        }

        void HandleUnitDefeated(CombatUnit unit)
        {
            if (actors.TryGetValue(unit, out var actor))
                actor.PlayDefeat();
        }
    }
}