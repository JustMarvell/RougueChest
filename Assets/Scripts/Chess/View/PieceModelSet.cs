using Chess.Core;
using UnityEngine;

namespace Chess.View
{
    // Shared mesh-lookup used by both the board renderer (BoardView) and the
    // 3D combat stage (CombatStageController). Extracted out of BoardView so
    // combat doesn't need a BoardView reference just to know what a piece
    // looks like. Swappable later: a "player's equipped custom character"
    // provider can sit alongside this one without touching either caller.
    [System.Serializable]
    public class PieceModelSet
    {
        public GameObject pawn, knight, bishop, rook, queen, king;

        public GameObject Get(PieceType type) => type switch
        {
            PieceType.Pawn => pawn,
            PieceType.Knight => knight,
            PieceType.Bishop => bishop,
            PieceType.Rook => rook,
            PieceType.Queen => queen,
            PieceType.King => king,
            _ => null
        };
    }
}