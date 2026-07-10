using System;
using System.Collections;
using UnityEngine;

namespace Chess.View
{
    // Smoothly moves the Main Camera between the normal chess-board view and
    // the combat stage's camera marker, and also directs attention DURING a
    // turn - cutting to the acting unit, then to the clash on a hit. The
    // slow transitionDuration is for entering/exiting combat mode overall;
    // the snappier focusTransitionDuration is for these in-turn cuts.
    public class CombatCameraController : MonoBehaviour
    {
        public Camera targetCamera;
        public Transform combatCameraAnchor;
        public float transitionDuration = 0.6f;
        public AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Turn Focus Settings")]
        public float actorFocusDistance = 2.2f;
        public float actorFocusHeight = 1.4f;
        public float clashFocusDistance = 3f;
        public float clashFocusHeight = 1.6f;
        public float focusTransitionDuration = 0.35f;

        Vector3 savedPosition;
        Quaternion savedRotation;
        Coroutine activeRoutine;

        void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        public void EnterCombatView(Action onComplete = null)
        {
            savedPosition = targetCamera.transform.position;
            savedRotation = targetCamera.transform.rotation;
            MoveTo(combatCameraAnchor.position, combatCameraAnchor.rotation, onComplete, transitionDuration);
        }

        public void ExitCombatView(Action onComplete = null)
        {
            MoveTo(savedPosition, savedRotation, onComplete, transitionDuration);
        }

        // Over-the-shoulder-ish framing behind the acting unit, looking past
        // them toward the opposing side. Uses the actor's own forward - which
        // CombatFormation already points at the enemy row - so no attacker/
        // defender branching needed here.
        public void FocusOnActor(Transform actor)
        {
            if (actor == null) return;
            Vector3 camPos = actor.position - actor.forward * actorFocusDistance + Vector3.up * actorFocusHeight;
            Vector3 lookTarget = actor.position + Vector3.up * (actorFocusHeight * 0.5f);
            Quaternion camRot = Quaternion.LookRotation((lookTarget - camPos).normalized, Vector3.up);
            MoveTo(camPos, camRot, null, focusTransitionDuration);
        }

        // Side-angle shot framing both source and target around their
        // midpoint - the "clash" cut for when a hit actually lands.
        public void FocusOnClash(Transform source, Transform target)
        {
            if (source == null || target == null) return;
            Vector3 midpoint = (source.position + target.position) * 0.5f + Vector3.up * clashFocusHeight;
            Vector3 lineDir = (target.position - source.position).normalized;
            Vector3 side = Vector3.Cross(lineDir, Vector3.up);

            Vector3 camPos = midpoint + side * (clashFocusDistance * 0.5f) - lineDir * (clashFocusDistance * 0.3f);
            Quaternion camRot = Quaternion.LookRotation((midpoint - camPos).normalized, Vector3.up);
            MoveTo(camPos, camRot, null, focusTransitionDuration);
        }

        public void ReturnToWideCombatShot()
        {
            if (combatCameraAnchor == null) return;
            MoveTo(combatCameraAnchor.position, combatCameraAnchor.rotation, null, focusTransitionDuration);
        }

        void MoveTo(Vector3 pos, Quaternion rot, Action onComplete, float duration)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(TwerpCamera(pos, rot, onComplete, duration));
        }

        IEnumerator TwerpCamera(Vector3 targetPos, Quaternion targetRot, Action onComplete, float duration)
        {
            Vector3 startPos = targetCamera.transform.position;
            Quaternion startRot = targetCamera.transform.rotation;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float k = easing.Evaluate(Mathf.Clamp01(t / duration));
                targetCamera.transform.position = Vector3.Lerp(startPos, targetPos, k);
                targetCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
                yield return null;
            }

            targetCamera.transform.SetPositionAndRotation(targetPos, targetRot);
            onComplete?.Invoke();
        }
    }
}