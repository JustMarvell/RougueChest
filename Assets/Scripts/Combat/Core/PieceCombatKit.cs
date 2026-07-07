using Chess.Core;
using UnityEngine;

namespace Combat.Core
{
    // Basic/Skill/Ultimate triplet for one PieceType, plus that piece's
    // Ultimate cost. MaxEnergy is intentionally per-piece (per your call) -
    // NOT a flat 100 for everyone. Cheap/spammy pieces get a low value,
    // strong/expensive ones get a high value.
    [CreateAssetMenu(menuName = "Combat/Piece Kit")]
    public class PieceCombatKit : ScriptableObject
    {
        public PieceType Type;
        public int MaxEnergy = 100;

        public AbilityDefinition Basic;
        public AbilityDefinition Skill;
        public AbilityDefinition Ultimate;
    }
}