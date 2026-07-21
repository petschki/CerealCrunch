using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Level progression map ("breakfast path"): a board-game style walkway
/// (path_map.png) with start arch, stations and goal podium. Level plates,
/// numbers and the mascot are placed at runtime on top of the artwork.
/// The anchor table below MUST match the path drawn in Tools/art/path_map.svg.
/// Cerealia art: replace Assets/Resources/Cereals/cerealia.png to swap.
public class LevelPathScreen : MonoBehaviour
{
    // Normalized positions (x, y from bottom) of the 8 node slots on the map
    static readonly Vector2[] NodeAnchors =
    {
        new Vector2(0.24f, 0.126f),
        new Vector2(0.42f, 0.126f),
        new Vector2(0.60f, 0.126f),
        new Vector2(0.78f, 0.219f),
        new Vector2(0.70f, 0.375f),
        new Vector2(0.48f, 0.375f),
        new Vector2(0.26f, 0.448f),
        new Vector2(0.38f, 0.585f)
    };

    const float HopDuration = 0.55f;
    const float HopHeight = 240f;
    const float MascotBaseY = 80f; // Cerealia stands slightly above the plate

    static readonly Color DoneColor = new Color(0.62f, 0.82f, 0.42f);
    static readonly Color CurrentColor = new Color(0.98f, 0.72f, 0.25f);
    static readonly Color LockedColor = new Color(0.88f, 0.84f, 0.78f);

    RectTransform mapRect;
    RectTransform nodesRoot;
    RectTransform mascotRt;
    readonly Dictionary<int, Vector2> nodeAnchors = new Dictionary<int, Vector2>();
    Action onDone;
    bool running;

    public static LevelPathScreen Create()
    {
        var canvas = GameUI.CreateCanvas("LevelPath", 50);
        var screen = canvas.gameObject.AddComponent<LevelPathScreen>();
        screen.Build();
        return screen;
    }

    /// Shows the path, hops Cerealia from fromLevel to toLevel, then calls
    /// doneAction and hides itself. Tapping anywhere skips the animation.
    public void Show(int fromLevel, int toLevel, Action doneAction)
    {
        onDone = doneAction;
        gameObject.SetActive(true);
        BuildNodes(fromLevel, toLevel);
        StopAllCoroutines();
        StartCoroutine(PlayRoutine(fromLevel, toLevel));
    }

    void Build()
    {
        // Map artwork, aspect-filled and cropped. Nodes and mascot live
        // INSIDE the map rect so they stay aligned with the drawn walkway.
        var holder = GameUI.CreateRect("Background", transform);
        GameUI.Stretch(holder);
        holder.gameObject.AddComponent<RectMask2D>();

        mapRect = GameUI.CreateRect("Map", holder);
        GameUI.Stretch(mapRect);
        var mapImage = mapRect.gameObject.AddComponent<Image>();
        mapImage.sprite = Resources.Load<Sprite>("Cereals/path_map");
        var fitter = mapRect.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 0.5f;

        var skip = mapRect.gameObject.AddComponent<Button>();
        skip.transition = Selectable.Transition.None;
        skip.onClick.AddListener(Skip);

        nodesRoot = GameUI.CreateRect("Nodes", mapRect);
        GameUI.Stretch(nodesRoot);

        var mascot = GameUI.CreateRect("Cerealia", mapRect);
        mascot.sizeDelta = new Vector2(180f, 180f);
        var mascotImage = mascot.gameObject.AddComponent<Image>();
        mascotImage.sprite = Resources.Load<Sprite>("Cereals/cerealia");
        mascotImage.preserveAspect = true;
        mascotImage.raycastTarget = false;
        mascotRt = mascot;

        gameObject.SetActive(false);
    }

    void BuildNodes(int fromLevel, int toLevel)
    {
        foreach (Transform child in nodesRoot)
            Destroy(child.gameObject);
        nodeAnchors.Clear();

        int start = Mathf.Max(1, toLevel - 4);
        var nodeSprite = Resources.Load<Sprite>("Cereals/path_node");
        for (int i = 0; i < NodeAnchors.Length; i++)
        {
            int nodeLevel = start + i;
            nodeAnchors[nodeLevel] = NodeAnchors[i];

            var node = GameUI.CreateRect($"Node {nodeLevel}", nodesRoot);
            SetAnchor(node, NodeAnchors[i]);
            bool isCurrent = nodeLevel == toLevel;
            node.sizeDelta = isCurrent ? new Vector2(160f, 160f) : new Vector2(132f, 132f);
            var nodeImage = node.gameObject.AddComponent<Image>();
            nodeImage.sprite = nodeSprite;
            nodeImage.raycastTarget = false;
            nodeImage.color = nodeLevel < toLevel ? DoneColor
                : isCurrent ? CurrentColor
                : LockedColor;

            var number = GameUI.CreateText("Number", node, nodeLevel.ToString(), isCurrent ? 62f : 50f,
                new Color(0.35f, 0.22f, 0.1f));
            GameUI.Stretch(number.rectTransform);
        }
    }

    IEnumerator PlayRoutine(int fromLevel, int toLevel)
    {
        running = true;
        PlaceMascot(nodeAnchors[fromLevel]);
        yield return new WaitForSeconds(0.5f);

        for (int lv = fromLevel; lv < toLevel && nodeAnchors.ContainsKey(lv + 1); lv++)
            yield return HopRoutine(nodeAnchors[lv], nodeAnchors[lv + 1]);

        yield return new WaitForSeconds(0.6f);
        Finish();
    }

    IEnumerator HopRoutine(Vector2 from, Vector2 to)
    {
        float t = 0f;
        while (t < HopDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / HopDuration);
            SetAnchor(mascotRt, Vector2.Lerp(from, to, p));
            float arc = HopHeight * 4f * p * (1f - p);
            mascotRt.anchoredPosition = new Vector2(0f, MascotBaseY + arc);
            // squash & stretch: stretch mid-air, squash on landing
            float stretch = 1f + 0.16f * Mathf.Sin(p * Mathf.PI);
            mascotRt.localScale = new Vector3(2f - stretch, stretch, 1f);
            yield return null;
        }
        PlaceMascot(to);
    }

    void PlaceMascot(Vector2 anchor)
    {
        SetAnchor(mascotRt, anchor);
        mascotRt.anchoredPosition = new Vector2(0f, MascotBaseY);
        mascotRt.localScale = Vector3.one;
    }

    void Skip()
    {
        StopAllCoroutines();
        Finish();
    }

    void Finish()
    {
        if (!running) return;
        running = false;
        gameObject.SetActive(false);
        var callback = onDone;
        onDone = null;
        callback?.Invoke();
    }

    static void SetAnchor(RectTransform rt, Vector2 anchor)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
    }
}
