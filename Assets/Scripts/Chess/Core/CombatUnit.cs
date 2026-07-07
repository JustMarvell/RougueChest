using Combat.Core;

namespace Chess.Core
{
    public enum CombatTeam
    {
        Attacker,
        Defender
    }

    public class CombatUnit
    {
        public string Name;
        public CombatTeam Team;

        // needed by the 3D stage to pick the right visual (mesh today,
        // maybe a custom equipped character later). Not used by combat math at all.
        public PieceColor Color;
        public PieceType PieceType;

        public int MaxHP;
        public int CurrentHP;
        public int Attack;
        public int Speed; // SPD stat - drives how often this unit acts (see TurnOrderService)

        // per-unit Energy toward Ultimate. MaxEnergy is per-piece (via
        // Kit), not a flat constant - cheap/spammy pieces sit low, strong
        // ones sit high.
        public int Energy;
        public int MaxEnergy;
        public bool UltimateReady => Energy >= MaxEnergy;

        // which Basic/Skill/Ultimate this unit can use. Set by
        // PieceCombatFactory at creation time from a PieceCombatKit.
        public PieceCombatKit Kit;

        public bool IsFrozen;

        public bool IsDefeated => CurrentHP <= 0;

        public CombatUnit(string name, CombatTeam team, int maxHp, int attack, int speed, PieceColor color, PieceType pieceType)
        {
            Name = name;
            Team = team;
            MaxHP = maxHp;
            CurrentHP = maxHp;
            Attack = attack;
            Speed = speed;
            Color = color;
            PieceType = pieceType;
        }

        public void TakeDamage(int amount)
        {
            CurrentHP -= amount;
            if (CurrentHP < 0) CurrentHP = 0;
        }

        public void Heal(int amount)
        {
            if (IsDefeated) return; // no reviving via heal - out is out for this encounter
            CurrentHP = System.Math.Min(MaxHP, CurrentHP + amount);
        }

        public void GainEnergy(int amount)
        {
            Energy = System.Math.Min(MaxEnergy, Energy + amount);
        }

        public CombatUnit Clone()
        {
            var clone = new CombatUnit(Name, Team, MaxHP, Attack, Speed, Color, PieceType);
            clone.CurrentHP = CurrentHP;
            clone.IsFrozen = IsFrozen;
            clone.Energy = Energy;
            clone.MaxEnergy = MaxEnergy;
            clone.Kit = Kit;
            clone.Color = Color;
            clone.PieceType = PieceType;
            return clone;
        }
    }
}