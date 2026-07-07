using System;
using System.Collections;
using UnityEditorInternal;
using UnityEngine;

namespace Chess.View
{
    // Smoothly moves the Main Camera between the normal chess-board view and
    // the combat stage's camera marker. Remembers whatever pose the camera
    // was in right before combat started (so it doesn't matter which POV
    // button - White/Black/Top/Left/Right - was last pressed) and restores
    // exactly that pose when combat ends, rather than snapping to a fixed
    // "chess mode" position.
    public class COmbatCameraController : MonoBehaviour
    {
        public Camera targetCamera;
        public Transform combatCameraAnchor;
        public float transitionDuration = 0.6f;
        public AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);

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
            MoveTo(combatCameraAnchor.position, combatCameraAnchor.rotation, onComplete);
        }

        public void ExitCombatView(Action onComplete = null)
        {
            MoveTo(savedPosition, savedRotation, onComplete);
        }

        void MoveTo(Vector3 pos, Quaternion rot, Action onComplete)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(TwerpCamera(pos, rot, onComplete));
        }

        IEnumerator TwerpCamera(Vector3 targetPos, Quaternion targetRot, Action onComplete)
        {
            Vector3 startPos = targetCamera.transform.position;
            Quaternion startRot = targetCamera.transform.rotation;
            float t = 0f;

            while (t < transitionDuration)
            {
                t += Time.deltaTime;
                float k = easing.Evaluate(Mathf.Clamp01(t / transitionDuration));
                targetCamera.transform.position = Vector3.Lerp(startPos, targetPos, k);
                targetCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
                yield return null;
            }

            targetCamera.transform.SetPositionAndRotation(targetPos, targetRot);
            onComplete?.Invoke();
        }
    }
}