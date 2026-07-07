using System;
using Chess.Core;
using Unity.VisualScripting;

namespace Combat.Core
{
    // Drives one team's decisions from external input. RequestAction doesn't
    // decide anything itself - it stashes the callback and raises an event so
    // a CombatView/CombatInputHandler (not built yet) can show buttons/target
    // picker and eventually call SubmitAction. Until real UI exists, tests or
    // temporary code can call SubmitAction directly to simulate a player pick.
    public class PlayerDecisionProvider : ICombatDecisionProvider
    {
        public event Action<CombatUnit, CombatState> OnDecisionNeeded;

        Action<CombatAction> pendingCallback;

        public CombatUnit PendingActor { get; private set; }
        public bool IsAwaitingInput => pendingCallback != null;

        public void RequestAction(CombatUnit actor, CombatState state, Action<CombatAction> onDecided)
        {
            PendingActor = actor;
            pendingCallback = onDecided;
            OnDecisionNeeded?.Invoke(actor, state);
        }

        // Called by input/UI once the player has chosen an action + target(s).
        public void SubmitAction(CombatAction action)
        {
            if (pendingCallback == null) return; // stray submit, no request pending - ignore
            var callback = pendingCallback;
            pendingCallback = null;
            PendingActor = null;
            callback(action);
        }
    }
}