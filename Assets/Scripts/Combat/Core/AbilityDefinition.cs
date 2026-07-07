using System.Collections.Generic;
using UnityEngine;

namespace Combat.Core
{
    [CreateAssetMenu(menuName = "Combat/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        public string DisplayName = "Unnamed Ability";
        public ActionKind Kind;
        public TargetType TargetType;

        [Tooltip("SP cost if Kind == Skill. Ignored for Basic/Ultimate. Usually 1, but some piece may cost more.")]
        public int SPCost = 1;

        [Tooltip("Energy this action's cast grants it's own caster.")]
        public int SelfEnergyGain = 20;

        [SerializeReference]
        public List<AbilityEffect> Effects = new List<AbilityEffect>();
    }
}