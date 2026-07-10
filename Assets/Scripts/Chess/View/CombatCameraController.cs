using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.View
{
    // Moves the Main Camera between the chess-board view and the combat
    // stage, and directs attention DURING a turn (actor -> clash -> next
    // actor). Enter/Exit are immediate mode transitions. FocusOnActor/
    // FocusOnClash are QUEUED - each cut plays to completion and holds for
    // its duration before the next queued cut starts. This is what keeps
    // "cut to clash" and "cut to next actor's turn" (which CombatState fires
    // back-to-back in the same frame) from stomping on each other - they
    // just play in order instead of racing.
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
        public float clashHoldDuration = 0.5f;

        Vector3 savedPosition;
        Quaternion savedRotation;

        Coroutine modeTransitionRoutine;    // Enter/ExitCombatView only
        Coroutine queueRunner;              // per-turn focus cuts only
        readonly Queue<IEnumerator> cameraQueue = new Queue<IEnumerator>();

        void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        public void EnterCombatView(Action onComplete = null)
        {
            ClearFocusQueue();
            savedPosition = targetCamera.transform.position;
            savedRotation = targetCamera.transform.rotation;
            MoveImmediate(combatCameraAnchor.position, combatCameraAnchor.rotation, onComplete, transitionDuration);
        }

        public void ExitCombatView(Action onComplete = null)
        {
            ClearFocusQueue();
            MoveImmediate(savedPosition, savedRotation, onComplete, transitionDuration);
        }

        // Queued - cuts to the acting unit, waits here until the NEXT queued
        // cut (a clash, or the following turn's actor) is ready to play.
        public void FocusOnActor(Transform actor) => Enqueue(FocusOnActorRoutine(actor));

        // Queued - cuts to the clash angle and holds for clashHoldDuration
        // before letting the queue move on, so the hit is actually visible.
        public void FocusOnClash(Transform source, Transform target) => Enqueue(FocusOnClashRoutine(source, target));

        void Enqueue(IEnumerator routine)
        {
            cameraQueue.Enqueue(routine);
            if (queueRunner == null)
                queueRunner = StartCoroutine(ProcessQueue());
        }

        void ClearFocusQueue()
        {
            cameraQueue.Clear();
            if (queueRunner != null) StopCoroutine(queueRunner);
            queueRunner = null;
        }

        IEnumerator ProcessQueue()
        {
            while (cameraQueue.Count > 0)
                yield return StartCoroutine(cameraQueue.Dequeue());
            queueRunner = null;
        }

        IEnumerator FocusOnActorRoutine(Transform actor)
        {
            if (actor == null) yield break;
            Vector3 camPos = actor.position - actor.forward * actorFocusDistance + Vector3.up * actorFocusHeight;
            Vector3 lookTarget = actor.position + Vector3.up * (actorFocusHeight * 0.5f);
            Quaternion camRot = Quaternion.LookRotation((lookTarget - camPos).normalized, Vector3.up);
            yield return TweenCamera(camPos, camRot, focusTransitionDuration);
        }

        IEnumerator FocusOnClashRoutine(Transform source, Transform target)
        {
            if (source == null || target == null) yield break;
            Vector3 midpoint = (source.position + target.position) * 0.5f + Vector3.up * clashFocusHeight;
            Vector3 lineDir = (target.position - source.position).normalized;
            Vector3 side = Vector3.Cross(lineDir, Vector3.up);

            Vector3 camPos = midpoint + side * (clashFocusDistance * 0.5f) - lineDir * (clashFocusDistance * 0.3f);
            Quaternion camRot = Quaternion.LookRotation((midpoint - camPos).normalized, Vector3.up);
            yield return TweenCamera(camPos, camRot, focusTransitionDuration);
            yield return new WaitForSeconds(clashHoldDuration);
        }

        void MoveImmediate(Vector3 pos, Quaternion rot, Action onComplete, float duration)
        {
            if (modeTransitionRoutine != null) StopCoroutine(modeTransitionRoutine);
            modeTransitionRoutine = StartCoroutine(RunImmediate(pos, rot, onComplete, duration));
        }

        IEnumerator RunImmediate(Vector3 pos, Quaternion rot, Action onComplete, float duration)
        {
            yield return TweenCamera(pos, rot, duration);
            onComplete?.Invoke();
        }

        IEnumerator TweenCamera(Vector3 targetPos, Quaternion targetRot, float duration)
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
        }
    }
}