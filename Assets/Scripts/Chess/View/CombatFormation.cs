using UnityEngine;

namespace Combat.View
{
    // Defines where each team's units stand in the 3D combat stage. Assign
    // Transform markers in the Inspector for full control over stage layout;
    // if a team's slot array is left empty, GetSlotPosition falls back to an
    // auto-generated row so the stage still works before you've placed markers.
    public class CombatFormation : MonoBehaviour
    {
        [Header("Manual slot markers (optional, up to 5 per team)")]
        public Transform[] attackerSlots;
        public Transform[] defenderSlots;

        [Header("Auto-layout fallback (used when a slot array above is empty)")]
        public Vector3 attackerRowCenter = new Vector3(0 ,0, -4);
        public Vector3 defenderRowCenter = new Vector3(0, 0, 4);
        public float slotSpacing = 1.5f;

        public Vector3 GetSlotPoisition(bool isAttacker, int index, int teamSize)
        {
            var manual = isAttacker ? attackerSlots : defenderSlots;
            if (manual != null && index < manual.Length && manual[index] != null) 
                return manual[index].position;

            var center = isAttacker ? attackerRowCenter : defenderRowCenter;
            float totalWidth = (teamSize - 1) * slotSpacing;
            float x = -totalWidth / 2f + index * slotSpacing;
            return center + new Vector3(x, 0, 0);
        }

        // Attacker faces "forward" (+Z, toward defender row); defender faces back.
        public Quaternion GetSLotRotation(bool isAttacker) =>
            isAttacker ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
    }
}