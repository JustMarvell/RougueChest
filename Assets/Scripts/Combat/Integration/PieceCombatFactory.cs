using Combat.Core;
using Chess.Core;
using System.Collections.Generic;

namespace Combat.Integration
{
    // Placeholder baseline stats per piece type. Numbers are not balanced -
    // just enough to prove combat resolves, feels roughly chess-shaped
    // (Queen strongest, Pawn weakest), and has some spread across HP/ATK/SPD
    // until the real kit/skill/card system exists.
    public static class PieceCombatFactory
    {
        struct Stats
        {
            public int HP;
            public int Attack;
            public int Speed;
        }

        static readonly Dictionary<PieceType, Stats> baseline = new Dictionary<PieceType, Stats>
        {
            { PieceType.Pawn, new Stats {HP = 80, Attack = 15, Speed = 90 }},
            { PieceType.Knight, new Stats {HP = 100, Attack = 20, Speed = 115 }},
            { PieceType.Bishop, new Stats {HP = 90, Attack = 22, Speed = 100 }},
            { PieceType.Rook, new Stats {HP = 140, Attack = 18, Speed = 85 }},
            { PieceType.Queen, new Stats {HP = 120, Attack = 28, Speed = 105 }},
            { PieceType.King, new Stats {HP = 150, Attack = 12, Speed = 95 }},
        };

        public static CombatUnit Create(Piece piece, CombatTeam team)
        {
            var s = baseline[piece.Type];
            var kit = DefaultCombatKits.Get(piece.Type);

            var unit = new CombatUnit($"{piece.Color}_{piece.Type}", team, s.HP, s.Attack, s.Speed);
            unit.Kit = kit;
            unit.MaxEnergy = kit.MaxEnergy;
            return unit;
        }
    }
}