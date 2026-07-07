using UnityEngine;

namespace Combat.Core
{
    // Shared per TEAM (not per unit). Basic attacks generate SP, Skills spend
    // it. Persists across encounters - lives on GameState (per PieceColor),
    // NOT on CombatState, which is rebuilt fresh every capture. CombatState
    // just holds a reference to the same instance, so whatever it ends at
    // carries into the next fight automatically.
    public class SkillPointPool
    {
        public const int Max = 5;
        public int Current;

        public SkillPointPool(int startingValue = 0)
        {
            Current = Mathf.Clamp(startingValue, 0, Max);
        }

        public void Generate(int amount = 1) => Current = Mathf.Min(Current + amount, Max);
        public bool CanAfford(int cost) => Current >= cost;

        public bool TrySpend(int cost)
        {
            if (Current < cost) return false;
            Current -= cost;
            return true;
        }
    }
}