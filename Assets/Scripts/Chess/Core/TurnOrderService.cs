using System.Collections.Generic;
using System.Linq;
using Chess.Core;
using UnityEngine.Rendering;

namespace Combat.Core
{
    // Discrete-event turn order using Action Values (AV = BaseActionValue / SPD),
    // the same model Honkai: Star Rail uses. This is NOT a continuously filling
    // gauge - it's a priority queue keyed on "how much AV until this unit's next
    // action." That makes it fully deterministic and serializable (server can
    // replay it exactly), and makes speed changes / turn advances a single line
    // of arithmetic instead of special-cased gauge surgery.
    public class TurnOrderService
    {
        public const float BaseActionValue = 10000f;

        class Entry
        {
            public CombatUnit Unit;
            public float RemainingAV;
        }

        readonly List<Entry> entries = new();

        public void Register(CombatUnit unit)
        {
            entries.Add(new Entry { Unit = unit, RemainingAV = BaseActionValue / unit.Speed });
        }

        public void Remove(CombatUnit unit)
        {
            entries.RemoveAll(e => e.Unit == unit);
        }

        void PruneDefeated()
        {
            entries.RemoveAll(e => e.Unit.IsDefeated);
        }

        // Pops the unit with the least remaining AV, advances "now" for everyone
        // else by that same amount, then reschedules the acting unit's next
        // action at a fresh interval based on their current SPD.
        public CombatUnit PopNextActor()
        {
            PruneDefeated();
            if (entries.Count == 0) return null;

            var next = entries[0];
            foreach (var e in entries)
                if (e.RemainingAV < next.RemainingAV) next = e;

            float elapsed = next.RemainingAV;
            foreach (var e in entries) e.RemainingAV -= elapsed;

            next.RemainingAV = BaseActionValue / next.Unit.Speed;
            return next.Unit;
        }

        // Call this immediately after changing unit.Speed (e.g. Knight's buff).
        // Rescales the unit's remaining AV proportionally so a SPD increase
        // pulls their next turn earlier without touching anyone else's entry.
        public void ApplySpeedChange(CombatUnit unit, int previousSpeed)
        {
            if (previousSpeed <= 0 || unit.Speed <= 0) return;
            var entry = entries.FirstOrDefault(e => e.Unit == unit);
            if (entry == null) return;
            entry.RemainingAV *= (float)previousSpeed / unit.Speed;
        }

        // HSR-style "Advance Forward X%" - shrinks remaining AV by a percentage.
        // percent = 1.0 means the unit's next turn effectively happens right now.
        public void AdvanceTurn(CombatUnit unit, float percent)
        {
            if (percent < 0f) percent = 0f;
            if (percent > 1f) percent = 1f;

            var entry = entries.FirstOrDefault(e => e.Unit == unit);
            if (entry == null) return;
            entry.RemainingAV *= 1f - percent;
        }

        // Non-mutating look-ahead for turn-order UI (the row of upcoming portraits).
        // Simulates forward on a cloned copy of the queue so it never disturbs
        // the real state.
        public List<CombatUnit> PreviewUpcoming(int count)
        {
            var clones = entries
                .Where(e => !e.Unit.IsDefeated)
                .Select(e => new Entry { Unit = e.Unit, RemainingAV = e.RemainingAV })
                .ToList();

            var result = new List<CombatUnit>();
            for (int i = 0; i < count && clones.Count > 0; i++)
            {
                var next = clones[0];
                foreach (var c in clones)
                    if (c.RemainingAV < next.RemainingAV) next = c;

                result.Add(next.Unit);

                float elapsed = next.RemainingAV;
                foreach (var c in clones) c.RemainingAV -= elapsed;
                next.RemainingAV = BaseActionValue / next.Unit.Speed;
            }

            return result;
        }
    }
}