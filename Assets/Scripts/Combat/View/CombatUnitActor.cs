using System.Collections;
using Chess.Core;
using UnityEngine;

namespace Combat.View
{
    public class CombatUnitActor : MonoBehaviour
    {
        public CombatUnit BoundUnit { get; private set; }

        GameObject visualInstance;
        GameObject targetRing;
        Vector3 homePosition;
        Coroutine activeRoutine;

        public void Bind(CombatUnit unit, GameObject modelPrefab, Vector3 position, Quaternion rotation, float scale)
        {
            BoundUnit = unit;
            homePosition = position;
            transform.SetPositionAndRotation(position, rotation);

            if (modelPrefab != null)
            {
                visualInstance = Instantiate(modelPrefab, transform);
                visualInstance.transform.localPosition = Vector3.zero;
                visualInstance.transform.localScale = Vector3.one * scale;
            }
            else
            {
                // Fallback primitive, mirrors BoardView's missing-model behavior.
                var primitive = unit.PieceType == PieceType.King ? PrimitiveType.Cylinder : PrimitiveType.Capsule;
                visualInstance = GameObject.CreatePrimitive(primitive);
                visualInstance.transform.SetParent(transform);
                visualInstance.transform.localPosition = Vector3.zero;
                visualInstance.transform.localScale = new Vector3(0.6f, unit.PieceType == PieceType.Pawn ? 0.4f : 0.6f, 0.6f);
                visualInstance.GetComponent<Renderer>().material.color =
                    unit.Color == PieceColor.White ? Color.white : Color.gray;
            }

            SetupClickCollider();
        }

        // Added regardless of whether the model prefab has its own colliders,
        // so raycasting for target-selection is reliable across chess meshes,
        // primitive fallbacks, and future custom characters alike.
        void SetupClickCollider()
        {
            var box = gameObject.AddComponent<BoxCollider>();
            var renderers = visualInstance.GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                box.center = transform.InverseTransformPoint(bounds.center);
                box.size = bounds.size;
            }
            else
            {
                box.center = Vector3.up * 0.5f;
                box.size = new Vector3(0.6f, 1f, 0.6f);
            }
        }

        // Simple flat ring at the actor's feet - placeholder visual until a
        // real decal/outline shader exists. Toggled by CombatView whenever
        // this unit is (or stops being) a legal target for the pending ability.
        public void SetTargetable(bool targetable)
        {
            if (targetable) EnsureTargetRing();
            if (targetRing != null) targetRing.SetActive(targetable);
        }

        void EnsureTargetRing()
        {
            if (targetRing != null) return;

            targetRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(targetRing.GetComponent<Collider>()); // don't let the ring itself block raycast
            targetRing.name = "TargetRing";
            targetRing.transform.SetParent(transform);
            targetRing.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            targetRing.transform.localScale = new Vector3(0.9f, 0.02f, 0.9f);

            var renderer = targetRing.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = new Color(1f, 0.25f, 0.2f, 0.85f);
            renderer.material = mat;
        }

        public void SetActingHighlight(bool active)
        {
            StopActive();
            activeRoutine = StartCoroutine(active ? BobLoop() : ReturnHome());
        }

        public void PlayAttack(Vector3 towardTarget)
        {
            StopActive();
            activeRoutine = StartCoroutine(LungeAndReturn(towardTarget));
        }

        public void PlayHitReaction() => StartCoroutine(ShakeOnce());

        public void PlayDefeat()
        {
            StopActive();
            StartCoroutine(SinkAndFade());
        }

        void StopActive()
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            transform.position = homePosition;
        }

        IEnumerator ReturnHome() => Tween(transform.position, homePosition, 0.2f);

        IEnumerator BobLoop()
        {
            while (true)
            {
                float y = homePosition.y + Mathf.Sin(Time.time * 4f) * 0.08f;
                transform.position = new Vector3(homePosition.x, y, homePosition.z);
                yield return null;
            }
        }

        IEnumerator LungeAndReturn(Vector3 towardTarget)
        {
            Vector3 direction = (towardTarget - homePosition).normalized;
            Vector3 lungePoint = homePosition + direction * 0.6f;
            yield return Tween(homePosition, lungePoint, 0.15f);
            yield return Tween(lungePoint, homePosition, 0.15f);
        }

        IEnumerator ShakeOnce()
        {
            Vector3 origin = transform.position;
            for (int i = 0; i < 4; i++)
            {
                transform.position = origin + Random.insideUnitSphere * 0.05f;
                yield return new WaitForSeconds(0.03f);
            }
            transform.position = origin;
        }

        IEnumerator SinkAndFade()
        {
            yield return Tween(transform.position, transform.position + Vector3.down * 0.5f, 0.6f);
            gameObject.SetActive(false);
        }

        IEnumerator Tween(Vector3 from, Vector3 to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, t / duration);
                yield return null;
            }
            transform.position = to;
        }
    }
}