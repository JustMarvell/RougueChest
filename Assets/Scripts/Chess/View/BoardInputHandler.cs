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

            if (boardView.State.Status == GameStatus.AwaitingCombat)
            {
                HandleSelectionClick(clicked);
                return;
            }

            if (selected == null)
            {
                var piece = boardView.State.Board.Get(clicked);
                if (piece != null && piece.Color == boardView.State.SideToMove)
                {
                    selected = clicked;
                    boardView.UpdateSelectedUI(selected);
                }
                return;
            }

            if (boardView.State.TryMakeMove(selected.Value, clicked))
            {
                boardView.RedrawPieces();
                boardView.UpdateTurnUI();
            }

            selected = null;
            boardView.UpdateSelectedUI(null);
        }

        // Clicking the mandatory piece (the attacker or defender itself)
        // confirms the current phase's picks and advances to the next step -
        // no separate UI button needed for the prototype. Clicking any other
        // eligible square toggles it on/off the current phase's team.
        void HandleSelectionClick(Square clicked)
        {
            var selection = boardView.ActiveSelection;
            if (selection == null) return;

            if (clicked == selection.CurrentOrigin)
                selection.ConfirmCurrentPhase();
            else
                selection.TogglePick(clicked);

            boardView.RefreshSelectionHighlights();

            if (selection.IsReady)
                boardView.ResolveActiveSelection();
        }

    }
}