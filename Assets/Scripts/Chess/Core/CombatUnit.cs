namespace Chess.Core
{
    public enum CombatTeam
    {
        Attacker,
        Defender
    }

    // Bare combat participant. Intentionally minimal - no skills, elements,
    // or cards yet. This exists to prove the turn-order engine and a basic
    // attack loop end to end before layering content on top of it.
    public class CombatUnit
    {
        public string Name;
        public CombatTeam Team;
        public int MaxHP;
        public int CurrentHP;
        public int Attack;
        public int Speed; // SPD stat - drives how often this unit acts (see TurnOrderService)

        // Placeholder for the Stalemate (Zugzwang + Tempo) reaction: skips
        // this unit's next action once, then clears itself. See CombatState.RunNextTurn.
        public bool IsFrozen;

        public bool IsDefeated => CurrentHP <= 0;

        public CombatUnit(string name, CombatTeam team, int maxHp, int attack, int speed)
        {
            Name = name;
            Team = team;
            MaxHP = maxHp;
            CurrentHP = maxHp;
            Attack = attack;
            Speed = speed;
        }

        public void TakeDamage(int amount)
        {
            CurrentHP -= amount;
            if (CurrentHP < 0) CurrentHP = 0;
        }

        public CombatUnit Clone()
        {
            var clone = new CombatUnit(Name, Team, MaxHP, Attack, Speed);
            clone.CurrentHP = CurrentHP;
            clone.IsFrozen = IsFrozen;
            return clone;
        }
    }
}