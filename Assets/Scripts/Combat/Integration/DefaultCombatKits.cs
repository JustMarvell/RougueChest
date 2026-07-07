using System;
using System.Collections.Generic;
using Chess.Core;
using Combat.Core;
using UnityEngine;

namespace Combat.Integration
{
    // Placeholder kits built at runtime, same spirit as PieceCombatFactory's
    // baseline stats: numbers are NOT balanced, just enough to prove Skill/SP/
    // Energy/Ultimate all actually resolve end to end. Replace with real
    // hand-authored .asset files (via [CreateAssetMenu]) once numbers matter.
    public static class DefaultCombatKits
    {
        static Dictionary<PieceType, PieceCombatKit> kits;

        public static PieceCombatKit Get(PieceType type)
        {
            if (kits == null) Build();
            return kits[type];
        }

        static AbilityDefinition Ability(string name, ActionKind kind, TargetType target, int spCost, int energyGain, params AbilityEffect[] effects)
        {
            var a = ScriptableObject.CreateInstance<AbilityDefinition>();
            a.DisplayName = name;
            a.Kind = kind;
            a.TargetType = target;
            a.SPCost = spCost;
            a.SelfEnergyGain = energyGain;
            a.Effects = new List<AbilityEffect>(effects);
            return a;
        }

        static void Build()
        {
            kits = new Dictionary<PieceType, PieceCombatKit>();

            // still placeholder
            // Pawn - cheap, spammy, low ultimate cost.
            Add(PieceType.Pawn, maxEnergy: 60,
                basic: Ability("Jab", ActionKind.Basic, TargetType.SingleEnemy, 0, 20,
                    new DamageEffect { AttackMultiplier = 1.0f}),
                skill: Ability("Advance", ActionKind.Skill, TargetType.SingleEnemy, 1, 20,
                    new DamageEffect { AttackMultiplier = 1.3f }),
                ultimate: Ability("Promotion Strike", ActionKind.Ultimate, TargetType.AllEnemies, 0, 0,
                    new DamageEffect { AttackMultiplier = 1.6f }));

            // Knight - turn manipulation support, per the design doc.
            Add(PieceType.Knight, maxEnergy: 100,
                basic: Ability("Slash", ActionKind.Basic, TargetType.SingleEnemy, 0, 20,
                    new DamageEffect { AttackMultiplier = 1.0f }),
                skill: Ability("Spur", ActionKind.Skill, TargetType.SingleAlly, 1, 15,
                    new SpeedChangeEffect { Delta = 30 }),
                ultimate: Ability("Chavalry Charge", ActionKind.Ultimate, TargetType.AllAllies, 0, 0,
                    new AdvanceTurnEffect { Percent = 1f }));

            // Bishop - ranged/magic AoE skill, big single-target burst ult.
            Add(PieceType.Bishop, maxEnergy: 90,
                basic: Ability("Bolt", ActionKind.Basic, TargetType.SingleEnemy, 0, 20,
                    new DamageEffect { AttackMultiplier = 1.0f }),
                skill: Ability("Diagonal Ray", ActionKind.Skill, TargetType.AllEnemies, 1, 15,
                    new DamageEffect { AttackMultiplier = 0.6f }),
                ultimate: Ability("Prism Beam", ActionKind.Ultimate, TargetType.SingleEnemy, 0, 0,
                    new DamageEffect {AttackMultiplier = 3.0f }));

            // Rook - tanky/defense. Skill/Ult are heal-on-self placeholders
            // until a real Shield/Taunt effect exists.
            Add(PieceType.Rook, maxEnergy: 110,
                basic: Ability("Bash", ActionKind.Basic, TargetType.SingleEnemy, 0, 20,
                    new DamageEffect { AttackMultiplier = 1.0f }),
                skill: Ability("Brace", ActionKind.Skill, TargetType.Self, 1, 15,
                    new HealEffect { MaxHPPercent = 0.15f }),
                ultimate: Ability("Bulwark", ActionKind.Ultimate, TargetType.AllAllies, 0, 0,
                    new HealEffect { MaxHPPercent = 0.20f }));

            // Queen - strongest all-rounder, most expensive ultimate.
            Add(PieceType.Queen, maxEnergy: 140,
                basic: Ability("Strike", ActionKind.Basic, TargetType.SingleEnemy, 0, 20,
                    new DamageEffect { AttackMultiplier = 1.0f }),
                skill: Ability("Royal Decree", ActionKind.Skill, TargetType.SingleEnemy, 1, 15,
                    new DamageEffect { AttackMultiplier = 1.5f }),
                ultimate: Ability("Sovereign's Wrath", ActionKind.Ultimate, TargetType.AllEnemies, 0, 0,
                    new DamageEffect { AttackMultiplier = 2.5f }));

            // King - defensive/support-leaning. Losing = game over, handled
            // at the GameState level, not here.
            Add(PieceType.King, maxEnergy: 100,
                basic: Ability("Guard Strike", ActionKind.Basic, TargetType.SingleEnemy, 0, 20,
                    new DamageEffect { AttackMultiplier = 1.0f }), 
                skill: Ability("Rally", ActionKind.Skill, TargetType.SingleAlly, 1, 15,
                    new HealEffect { MaxHPPercent = 0.15f }),
                ultimate: Ability("Royal Decree", ActionKind.Ultimate, TargetType.AllAllies, 0, 0, 
                    new HealEffect { MaxHPPercent = 0.25f }));
        }

        static void Add(PieceType type, int maxEnergy, AbilityDefinition basic, AbilityDefinition skill, AbilityDefinition ultimate)
        {
            var kit = ScriptableObject.CreateInstance<PieceCombatKit>();
            kit.Type = type;
            kit.MaxEnergy = maxEnergy;
            kit.Basic = basic;
            kit.Skill = skill;
            kit.Ultimate = ultimate;
            kits[type] = kit;
        }
    }
}