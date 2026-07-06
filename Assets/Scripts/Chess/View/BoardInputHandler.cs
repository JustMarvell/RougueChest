using UnityEngine;
using Chess.Core;

namespace Chess.View
{
    public class BoardInputHandler : MonoBehaviour
    {
        public BoardView boardView;
        Square? selected;

        void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit)) return;

            var clicked = boardView.WorldToSquare(hit.point);
            if (!clicked.IsValid) return;

            if (selected == null)
            {
                var piece = boardView.State.Board.Get(clicked);
                if (piece != null && piece.Color == boardView.State.SideToMove)
                    selected = clicked;
                return;
            }

            if (boardView.State.TryMakeMove(selected.Value, clicked))
                boardView.RedrawPieces();

            selected = null;
        }
    }
}