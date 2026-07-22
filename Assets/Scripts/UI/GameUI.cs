using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Runtime-built uGUI interface: HUD (level badge, score progress bar, moves
/// counter) plus the win/lose overlays. Built entirely from code because the
/// whole scene is code-generated. Also provides static factory helpers reused
/// by other overlays (e.g. the fake ad provider).
public class GameUI : MonoBehaviour
{
    static Sprite panelSprite, buttonSprite, buttonPressedSprite;
    static TMP_FontAsset displayFont, bodyFont;

    /// Chunky comic display font for headlines, buttons and HUD chips.
    /// Created as dynamic TMP asset at runtime (glyphs added on demand).
    public static TMP_FontAsset DisplayFont =>
        displayFont != null ? displayFont : displayFont = LoadFont("Fonts/LilitaOne-Regular");

    /// Rounded comic body font for dialog and description text.
    public static TMP_FontAsset BodyFont =>
        bodyFont != null ? bodyFont : bodyFont = LoadFont("Fonts/Baloo2-ExtraBold");

    static TMP_FontAsset LoadFont(string path)
    {
        var font = Resources.Load<Font>(path);
        return font != null ? TMP_FontAsset.CreateFontAsset(font) : null;
    }

    // Modal overlay counter: while any full-screen screen (story, café,
    // level path) is open, world input on the board is blocked.
    static int modalCount;
    public static bool InputBlocked => modalCount > 0;
    public static void PushModal() => modalCount++;
    public static void PopModal() => modalCount = Mathf.Max(0, modalCount - 1);

    public static Sprite PanelSprite =>
        panelSprite != null ? panelSprite : panelSprite = Resources.Load<Sprite>("Cereals/ui_panel");
    public static Sprite ButtonSprite =>
        buttonSprite != null ? buttonSprite : buttonSprite = Resources.Load<Sprite>("Cereals/ui_button");
    public static Sprite ButtonPressedSprite =>
        buttonPressedSprite != null ? buttonPressedSprite : buttonPressedSprite = Resources.Load<Sprite>("Cereals/ui_button_pressed");

    static readonly Color ChipColor = new Color(0.24f, 0.13f, 0.05f, 0.85f);
    static readonly Color FillColor = new Color(0.96f, 0.62f, 0.12f);
    static readonly Color WarnColor = new Color(1f, 0.45f, 0.35f);

    TMP_Text levelText, scoreText, movesText, continueLabel;
    RectTransform scoreFillRect;
    GameObject winOverlay, loseOverlay, rescueButtonGo;
    Action onContinue, onRescue, onRetry;

    public static GameUI Create()
    {
        EnsureEventSystem();
        var canvas = CreateCanvas("GameUI", 10);
        var ui = canvas.gameObject.AddComponent<GameUI>();
        ui.Build();
        return ui;
    }

    // ---------- public API ----------

    public void SetHud(int level, int score, int target, int moves)
    {
        levelText.text = $"Level {level}";
        scoreText.text = $"{score} / {target}";
        scoreFillRect.anchorMax = new Vector2(Mathf.Clamp01(score / (float)target), 1f);
        movesText.text = $"Züge: {moves}";
        movesText.color = moves <= 3 ? WarnColor : Color.white;
    }

    public void ShowWin(int nextLevel, Action continueAction)
    {
        onContinue = continueAction;
        continueLabel.text = $"Weiter zu Level {nextLevel}";
        winOverlay.SetActive(true);
        loseOverlay.SetActive(false);
    }

    public void ShowLose(bool rescueAvailable, Action rescueAction, Action retryAction)
    {
        onRescue = rescueAction;
        onRetry = retryAction;
        rescueButtonGo.SetActive(rescueAvailable);
        loseOverlay.SetActive(true);
        winOverlay.SetActive(false);
    }

    public void HideOverlays()
    {
        winOverlay.SetActive(false);
        loseOverlay.SetActive(false);
    }

    // ---------- construction ----------

    void Build()
    {
        var safe = CreateRect("SafeArea", transform);
        Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaFitter>();

        // Top HUD row: level badge | score bar | moves chip
        var badge = CreatePanel("LevelBadge", safe, ChipColor);
        Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(280f, 96f));
        levelText = CreateText("Text", badge.transform, "Level 1", 44f, Color.white);
        Stretch(levelText.rectTransform);

        var bar = CreatePanel("ScoreBar", safe, ChipColor);
        Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(520f, 96f));
        var fillArea = CreateRect("FillArea", bar.transform);
        Stretch(fillArea, 12f);
        var fill = CreatePanel("Fill", fillArea, FillColor);
        scoreFillRect = fill.rectTransform;
        scoreFillRect.anchorMin = Vector2.zero;
        scoreFillRect.anchorMax = new Vector2(0f, 1f);
        scoreFillRect.offsetMin = Vector2.zero;
        scoreFillRect.offsetMax = Vector2.zero;
        scoreText = CreateText("Text", bar.transform, "0 / 0", 36f, Color.white);
        Stretch(scoreText.rectTransform);

        var movesChip = CreatePanel("MovesChip", safe, ChipColor);
        Place(movesChip.rectTransform, new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(280f, 96f));
        movesText = CreateText("Text", movesChip.transform, "Züge: 0", 40f, Color.white);
        Stretch(movesText.rectTransform);

        // Overlays
        winOverlay = BuildOverlay("WinOverlay", "Level geschafft!", out _);
        var starLine = CreateText("StarLine", winOverlay.transform, "+1 Stern für die Renovierung!", 56f,
            new Color(1f, 0.85f, 0.3f));
        Place(starLine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(1100f, 90f));
        var continueButton = CreateButton("ContinueButton", winOverlay.transform, "Weiter",
            new Vector2(640f, 150f), () => onContinue?.Invoke());
        Place(continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(640f, 150f));
        continueLabel = continueButton.GetComponentInChildren<TMP_Text>();

        loseOverlay = BuildOverlay("LoseOverlay", "Keine Züge mehr!", out _);
        var rescueButton = CreateButton("RescueButton", loseOverlay.transform, "+5 Züge — Werbung ansehen",
            new Vector2(720f, 150f), () => onRescue?.Invoke());
        Place(rescueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(720f, 150f));
        rescueButtonGo = rescueButton.gameObject;
        var retryButton = CreateButton("RetryButton", loseOverlay.transform, "Nochmal versuchen",
            new Vector2(640f, 150f), () => onRetry?.Invoke());
        Place(retryButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -250f), new Vector2(640f, 150f));

        HideOverlays();
    }

    GameObject BuildOverlay(string name, string title, out TMP_Text titleText)
    {
        var rt = CreateRect(name, transform);
        Stretch(rt);
        var dim = rt.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f); // also blocks clicks on UI below

        titleText = CreateText("Title", rt, title, 92f, Color.white);
        Place(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1000f, 140f));
        return rt.gameObject;
    }

    // ---------- static factory helpers ----------

    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2532f, 1170f);
        scaler.matchWidthOrHeight = 1f; // landscape: match height
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static Image CreatePanel(string name, Transform parent, Color color)
    {
        var rt = CreateRect(name, parent);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = PanelSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    public static TMP_Text CreateText(string name, Transform parent, string text, float size, Color color)
    {
        var rt = CreateRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        if (DisplayFont != null)
            tmp.font = DisplayFont; // already chunky — no faux bold on top
        else
            tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Button CreateButton(string name, Transform parent, string label, Vector2 size, Action onClick)
    {
        var rt = CreateRect(name, parent);
        rt.sizeDelta = size;
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = ButtonSprite;
        image.type = Image.Type.Sliced;

        var button = rt.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.SpriteSwap;
        var sprites = button.spriteState;
        sprites.highlightedSprite = ButtonSprite;
        sprites.selectedSprite = ButtonSprite;
        sprites.pressedSprite = ButtonPressedSprite;
        button.spriteState = sprites;
        button.onClick.AddListener(() =>
        {
            AudioManager.Play("button");
            onClick?.Invoke();
        });

        var text = CreateText("Label", rt, label, 44f, Color.white);
        Stretch(text.rectTransform);
        // keep the label centered on the button face, above the bottom lip
        text.rectTransform.offsetMin = new Vector2(16f, 30f);
        text.rectTransform.offsetMax = new Vector2(-16f, -12f);
        return button;
    }

    public static void Stretch(RectTransform rt, float padding = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    /// Anchors an element to a screen edge/corner (pivot = anchor).
    static void Place(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
}
