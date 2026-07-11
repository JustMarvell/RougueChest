using UnityEngine;
using UnityEngine.EventSystems;

namespace Combat.View
{
    // Click-to-target input for the 3D stage - same raycast pattern as
    // BoardInputHandler, but resolves to a CombatUnitActor instead of a
    // chess Square. Only acts while CombatView is actually awaiting a
    // target; every other click is a no-op, so this can safely coexist
    // with BoardInputHandler running in the background.
    public class CombatStageInputHandler : MonoBehaviour
    {
        public CombatView combatView;
        public Camera raycastCamera;        // defaults to Camera.Main if left unset

        void Awake()
        {
            if (raycastCamera == null) raycastCamera = Camera.main;
        }

        void Update()
        {
            if (combatView == null || !combatView.IsAwaitingTarget) return;
            if (!Input.GetMouseButton(0)) return;

            // Don't let a click on an overlapping UI button (Basic/Skill/
            // Ultimate) also register as a world-space target click.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            var ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit)) return;

            var actor = hit.collider.GetComponentInParent<CombatUnitActor>();
            if (actor == null || actor.BoundUnit == null) return;

            combatView.TrySelectTarget(actor.BoundUnit);
        }
    }
}