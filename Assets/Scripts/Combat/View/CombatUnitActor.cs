using System.Collections;
using Chess.Core;
using UnityEngine;

namespace Combat.View
{
    public class CombatUnitActor : MonoBehaviour
    {
        public CombatUnit BoundUnit { get; private set; }

        GameObject visualInstance;
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