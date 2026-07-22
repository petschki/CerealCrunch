using System.Collections;
using TMPro;
using UnityEngine;

/// Poppy world-space callout text (character names on big matches):
/// bold TMP with dark outline, pops in with overshoot and a slight random
/// tilt, then rises and fades out.
public class FloatingText : MonoBehaviour
{
    const float PopDuration = 0.22f;
    const float RiseDuration = 0.95f;

    public static void Spawn(Vector3 pos, string text, Color color)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = pos;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 9f;
        if (GameUI.DisplayFont != null)
            tmp.font = GameUI.DisplayFont;
        else
            tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.color = color;
        tmp.outlineWidth = 0.28f;
        tmp.outlineColor = new Color32(40, 24, 12, 255);
        tmp.rectTransform.sizeDelta = new Vector2(10f, 3f);

        var meshRenderer = go.GetComponent<MeshRenderer>();
        if (meshRenderer != null) meshRenderer.sortingOrder = 60;

        go.AddComponent<FloatingText>();
    }

    IEnumerator Start()
    {
        var tmp = GetComponent<TMP_Text>();
        Color baseColor = tmp.color;
        Vector3 origin = transform.position;
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-8f, 8f));
        transform.localScale = Vector3.zero;

        // pop in with overshoot (ease-out-back)
        float t = 0f;
        while (t < PopDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / PopDuration);
            const float k = 2.2f;
            float scale = 1f + (k + 1f) * Mathf.Pow(p - 1f, 3f) + k * Mathf.Pow(p - 1f, 2f);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        transform.localScale = Vector3.one;

        // rise, grow slightly, fade near the end
        t = 0f;
        while (t < RiseDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / RiseDuration);
            transform.position = origin + Vector3.up * (0.85f * p);
            transform.localScale = Vector3.one * (1f + 0.08f * p);
            float alpha = p < 0.6f ? 1f : 1f - (p - 0.6f) / 0.4f;
            tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }
        Destroy(gameObject);
    }
}
