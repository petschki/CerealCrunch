using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// The renovation home screen: shows the café in its current stage,
/// lets the player spend stars on the next renovation step (full-image
/// crossfade between stages) and continues into the next match-3 level.
public class CafeScreen : MonoBehaviour
{
    const float FadeDuration = 1.1f;

    Image stageImage, fadeImage;
    TMP_Text starText, stepText, progressText;
    Button renovateButton, playButton;
    TMP_Text renovateLabel, playLabel;
    Action onPlay;
    int nextLevel;
    bool busy;

    public static CafeScreen Create()
    {
        var canvas = GameUI.CreateCanvas("CafeScreen", 40);
        var screen = canvas.gameObject.AddComponent<CafeScreen>();
        screen.Build();
        return screen;
    }

    public void Show(int upcomingLevel, Action playAction)
    {
        GameUI.PushModal();
        nextLevel = upcomingLevel;
        onPlay = playAction;
        gameObject.SetActive(true);
        stageImage.sprite = RenovationState.StageSprite(RenovationState.Stage);
        SetFadeAlpha(0f);
        Refresh();
    }

    void Hide()
    {
        GameUI.PopModal();
        gameObject.SetActive(false);
    }

    void Build()
    {
        // café artwork, crop-filled
        var holder = GameUI.CreateRect("Backdrop", transform);
        GameUI.Stretch(holder);
        holder.gameObject.AddComponent<RectMask2D>();

        stageImage = BuildArtLayer(holder, "Stage");
        fadeImage = BuildArtLayer(holder, "StageFade");

        // top-left: title + renovation progress
        var titleChip = GameUI.CreatePanel("TitleChip", transform, new Color(0.24f, 0.13f, 0.05f, 0.85f));
        PlaceCorner(titleChip.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(560f, 120f));
        var title = GameUI.CreateText("Title", titleChip.transform, "Cerealias Café", 52f, new Color(1f, 0.9f, 0.6f));
        var trt = title.rectTransform;
        GameUI.Stretch(trt);
        trt.offsetMin = new Vector2(20f, 52f);
        progressText = GameUI.CreateText("Progress", titleChip.transform, "", 34f, Color.white);
        var prt = progressText.rectTransform;
        GameUI.Stretch(prt);
        prt.offsetMax = new Vector2(-20f, -58f);

        // top-right: star balance
        var starChip = GameUI.CreatePanel("StarChip", transform, new Color(0.24f, 0.13f, 0.05f, 0.85f));
        PlaceCorner(starChip.rectTransform, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(280f, 120f));
        var starIcon = GameUI.CreateRect("Icon", starChip.transform);
        starIcon.anchorMin = starIcon.anchorMax = new Vector2(0f, 0.5f);
        starIcon.pivot = new Vector2(0f, 0.5f);
        starIcon.anchoredPosition = new Vector2(24f, 0f);
        starIcon.sizeDelta = new Vector2(72f, 72f);
        var starImage = starIcon.gameObject.AddComponent<Image>();
        starImage.sprite = Resources.Load<Sprite>("Cereals/star");
        starImage.preserveAspect = true;
        starImage.raycastTarget = false;
        starText = GameUI.CreateText("Count", starChip.transform, "0", 56f, Color.white);
        var srt = starText.rectTransform;
        GameUI.Stretch(srt);
        srt.offsetMin = new Vector2(100f, 0f);

        // bottom bar: next step + buttons
        var bar = GameUI.CreatePanel("BottomBar", transform, new Color(0.24f, 0.13f, 0.05f, 0.8f));
        var brt = bar.rectTransform;
        brt.anchorMin = new Vector2(0.5f, 0f);
        brt.anchorMax = new Vector2(0.5f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = new Vector2(0f, 24f);
        brt.sizeDelta = new Vector2(2200f, 170f);

        stepText = GameUI.CreateText("Step", bar.transform, "", 40f, Color.white);
        stepText.alignment = TextAlignmentOptions.Left;
        if (GameUI.BodyFont != null) stepText.font = GameUI.BodyFont;
        var strt = stepText.rectTransform;
        GameUI.Stretch(strt);
        strt.offsetMin = new Vector2(40f, 0f);
        strt.offsetMax = new Vector2(-1300f, 0f);

        renovateButton = GameUI.CreateButton("RenovateButton", bar.transform, "Renovieren", new Vector2(560f, 130f), Renovate);
        var rrt = renovateButton.GetComponent<RectTransform>();
        rrt.anchorMin = rrt.anchorMax = new Vector2(1f, 0.5f);
        rrt.pivot = new Vector2(1f, 0.5f);
        rrt.anchoredPosition = new Vector2(-660f, 0f);
        renovateLabel = renovateButton.GetComponentInChildren<TMP_Text>();

        playButton = GameUI.CreateButton("PlayButton", bar.transform, "Spielen", new Vector2(560f, 130f), Play);
        var plrt = playButton.GetComponent<RectTransform>();
        plrt.anchorMin = plrt.anchorMax = new Vector2(1f, 0.5f);
        plrt.pivot = new Vector2(1f, 0.5f);
        plrt.anchoredPosition = new Vector2(-40f, 0f);
        playLabel = playButton.GetComponentInChildren<TMP_Text>();

        gameObject.SetActive(false);
    }

    Image BuildArtLayer(RectTransform holder, string name)
    {
        var rect = GameUI.CreateRect(name, holder);
        GameUI.Stretch(rect);
        var image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 1408f / 768f;
        return image;
    }

    void Refresh()
    {
        starText.text = RenovationState.Stars.ToString();
        progressText.text = RenovationState.CafeComplete
            ? "Eröffnet!"
            : $"Renovierung {RenovationState.Stage} / {RenovationState.MaxStage}";

        if (RenovationState.CafeComplete)
        {
            stepText.text = "Das Café ist eröffnet! Bald: Obergeschoss & Garten...";
            renovateButton.gameObject.SetActive(false);
        }
        else
        {
            int cost = RenovationState.NextCost;
            stepText.text = $"Nächster Schritt: {RenovationState.NextLabel}";
            renovateButton.gameObject.SetActive(true);
            renovateLabel.text = cost == 1 ? "Renovieren (1 Stern)" : $"Renovieren ({cost} Sterne)";
            bool canRenovate = RenovationState.CanRenovate && !busy;
            renovateButton.interactable = canRenovate;
            // SpriteSwap has no disabled visual — dim the button manually
            renovateButton.image.color = canRenovate ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            renovateLabel.alpha = canRenovate ? 1f : 0.6f;
        }
        playLabel.text = $"Level {nextLevel} spielen";
    }

    void Renovate()
    {
        if (busy || !RenovationState.CanRenovate) return;
        StartCoroutine(RenovateRoutine());
    }

    IEnumerator RenovateRoutine()
    {
        busy = true;
        playButton.interactable = false;
        renovateButton.interactable = false;

        RenovationState.PayRenovation();
        AudioManager.Play("build");

        // crossfade to the new stage image
        fadeImage.sprite = RenovationState.StageSprite(RenovationState.Stage);
        float t = 0f;
        while (t < FadeDuration)
        {
            t += Time.deltaTime;
            SetFadeAlpha(Mathf.SmoothStep(0f, 1f, t / FadeDuration));
            yield return null;
        }
        stageImage.sprite = fadeImage.sprite;
        SetFadeAlpha(0f);

        if (RenovationState.CafeComplete)
            AudioManager.Play("win");

        busy = false;
        playButton.interactable = true;
        Refresh();
    }

    void Play()
    {
        if (busy) return;
        Hide();
        var callback = onPlay;
        onPlay = null;
        callback?.Invoke();
    }

    void SetFadeAlpha(float a)
    {
        var c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
        fadeImage.enabled = a > 0.001f;
    }

    static void PlaceCorner(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
    }
}
