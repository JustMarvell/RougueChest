using System.Collections.Generic;
using Combat.Core;
using UnityEngine;

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

        public ElementTag ActiveTag;
        public readonly List<DotInstance> ActiveDots = new List<DotInstance>();
        public int Shield;

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
            if (Shield > 0)
            {
                int absorbed = Mathf.Min(Shield, amount);
                Shield -= absorbed;
                amount -= absorbed;
            }

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
            var clone = new CombatUnit(Name, Team, MaxHP, Attack, Speed);
            clone.CurrentHP = CurrentHP;
            clone.IsFrozen = IsFrozen;
            clone.Energy = Energy;
            clone.MaxEnergy = MaxEnergy;
            clone.Kit = Kit;
            clone.Shield = Shield;
            return clone;
        }
    }
}