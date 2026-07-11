using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace Combat.View
{
    // Spawns and animates a single floating combat-text popup (damage, heal,
    // reaction name). Self-destructs after playing - callers fire-and-forget,
    // same pattern as CombatUnitActor's hit reactions.
    public class DamangeNumberPopup : MonoBehaviour
    {
        const float Duration = 0.9f;
        const float RiseHeight = 0.8f;
        const float FadeStartFraction = 0.6f;

        public static void Spawn(Vector3 worldPosition, string text, Color color, float fontSize = 3f)
        {
            var go = new GameObject("Damage Number");
            go.transform.position = worldPosition;
            
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            if (tmp.font == null) tmp.font = TMP_Settings.defaultFontAsset;

            go.AddComponent<DamangeNumberPopup>().StartCoroutine(PlayAndDestroy(go.transform, tmp));
        }

        static IEnumerator PlayAndDestroy(Transform t, TextMeshPro tmp)
        {
            Vector3 start = t.position;
            Vector3 end = start + Vector3.up * RiseHeight;
            Color startColor = tmp.color;

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float k = elapsed / Duration;

                t.position = Vector3.Lerp(start, end, k);
                if (Camera.main != null) t.rotation = Camera.main.transform.rotation;

                if (k > FadeStartFraction)
                {
                    float fadeK = (k - FadeStartFraction) / (1f - FadeStartFraction);
                    tmp.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, fadeK));
                }

                yield return null;
            }

            Destroy(t.gameObject);
        }
    }
}