using System.Collections;
using UnityEngine;

/// Aufsteigender, ausblendender Text in Weltkoordinaten (z.B. Charakternamen
/// bei großen Matches). Nutzt TextMesh, damit kein TMP-Paket nötig ist.
public class FloatingText : MonoBehaviour
{
    public static void Spawn(Vector3 pos, string text)
    {
        Create(pos + new Vector3(0.05f, -0.05f, 0f), text, new Color(0.2f, 0.1f, 0.03f), 51); // Schatten
        Create(pos, text, Color.white, 52);
    }

    static void Create(Vector3 pos, string text, Color color, int sortingOrder)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 72;
        tm.characterSize = 0.055f;
        tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        go.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
        go.AddComponent<FloatingText>();
    }

    IEnumerator Start()
    {
        var tm = GetComponent<TextMesh>();
        Vector3 origin = transform.position;
        Color baseColor = tm.color;
        float t = 0f;
        const float duration = 1.1f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            transform.position = origin + Vector3.up * (0.9f * (1f - (1f - p) * (1f - p)));
            float alpha = p < 0.65f ? 1f : 1f - (p - 0.65f) / 0.35f;
            tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }
        Destroy(gameObject);
    }
}
