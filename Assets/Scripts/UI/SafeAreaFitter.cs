using UnityEngine;

/// Keeps a full-stretch RectTransform inside the device safe area
/// (notch, home indicator). No-op on screens without insets.
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    Rect applied;

    void Update()
    {
        Rect safeArea = Screen.safeArea;
        if (safeArea == applied) return;
        applied = safeArea;

        var rt = (RectTransform)transform;
        Vector2 min = safeArea.position;
        Vector2 max = safeArea.position + safeArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
