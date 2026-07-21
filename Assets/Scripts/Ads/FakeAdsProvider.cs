using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// Placeholder provider for development: simulates load times and shows a
/// fullscreen fake ad on its own high-priority canvas. Behaves like a real
/// SDK (preloading, countdown, cancelling a rewarded ad yields no reward).
public class FakeAdsProvider : MonoBehaviour, IAdsProvider
{
    enum AdKind { None, Interstitial, Rewarded }

    const float InterstitialDuration = 3f;
    const float RewardedDuration = 5f;
    const float ReloadDelay = 2f;

    AdKind current = AdKind.None;
    float remaining;
    Action onInterstitialClosed;
    Action<bool> onRewardedFinished;

    Canvas adCanvas;
    TMP_Text titleText, countdownText, finishLabel;
    GameObject cancelGo, finishGo;

    public bool InterstitialReady { get; private set; }
    public bool RewardedReady { get; private set; }

    public void Initialize()
    {
        StartCoroutine(ReloadInterstitial());
        StartCoroutine(ReloadRewarded());
    }

    IEnumerator ReloadInterstitial()
    {
        InterstitialReady = false;
        yield return new WaitForSecondsRealtime(ReloadDelay);
        InterstitialReady = true;
    }

    IEnumerator ReloadRewarded()
    {
        RewardedReady = false;
        yield return new WaitForSecondsRealtime(ReloadDelay);
        RewardedReady = true;
    }

    public void ShowInterstitial(Action onClosed)
    {
        onInterstitialClosed = onClosed;
        Show(AdKind.Interstitial, InterstitialDuration);
        StartCoroutine(ReloadInterstitial());
    }

    public void ShowRewarded(Action<bool> onFinished)
    {
        onRewardedFinished = onFinished;
        Show(AdKind.Rewarded, RewardedDuration);
        StartCoroutine(ReloadRewarded());
    }

    void Show(AdKind kind, float duration)
    {
        BuildOverlay();
        current = kind;
        remaining = duration;

        adCanvas.gameObject.SetActive(true);
        titleText.text = kind == AdKind.Rewarded ? "REWARDED-WERBUNG" : "WERBUNG";
        finishLabel.text = kind == AdKind.Rewarded ? "Belohnung abholen" : "Schließen";
        countdownText.gameObject.SetActive(true);
        countdownText.text = Mathf.CeilToInt(duration).ToString();
        cancelGo.SetActive(kind == AdKind.Rewarded);
        finishGo.SetActive(false);
    }

    void Update()
    {
        if (current == AdKind.None || remaining <= 0f) return;

        remaining -= Time.unscaledDeltaTime;
        countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
        if (remaining <= 0f)
        {
            countdownText.gameObject.SetActive(false);
            cancelGo.SetActive(false);
            finishGo.SetActive(true);
        }
    }

    void Finish(bool rewarded)
    {
        var kind = current;
        current = AdKind.None;
        adCanvas.gameObject.SetActive(false);
        if (kind == AdKind.Interstitial)
            onInterstitialClosed?.Invoke();
        else
            onRewardedFinished?.Invoke(rewarded);
    }

    /// Built lazily on first use; sortingOrder above the game UI so the ad
    /// blocks all interaction underneath.
    void BuildOverlay()
    {
        if (adCanvas != null) return;

        adCanvas = GameUI.CreateCanvas("AdCanvas", 100);
        adCanvas.transform.SetParent(transform, false);

        var dim = GameUI.CreateRect("Dim", adCanvas.transform);
        GameUI.Stretch(dim);
        var image = dim.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.09f, 0.11f, 0.18f, 0.98f);

        titleText = GameUI.CreateText("Title", adCanvas.transform, "WERBUNG", 80f, Color.white);
        PlaceCentered(titleText.rectTransform, 320f, new Vector2(1000f, 120f));

        var subtitle = GameUI.CreateText("Subtitle", adCanvas.transform,
            "(Platzhalter — hier läuft später das Anbieter-SDK)", 40f,
            new Color(0.75f, 0.78f, 0.88f));
        PlaceCentered(subtitle.rectTransform, 210f, new Vector2(1000f, 80f));

        countdownText = GameUI.CreateText("Countdown", adCanvas.transform, "5", 160f, Color.white);
        PlaceCentered(countdownText.rectTransform, 0f, new Vector2(400f, 220f));

        var finishButton = GameUI.CreateButton("FinishButton", adCanvas.transform, "Schließen",
            new Vector2(640f, 150f), () => Finish(true));
        PlaceCentered(finishButton.GetComponent<RectTransform>(), -60f, new Vector2(640f, 150f));
        finishGo = finishButton.gameObject;
        finishLabel = finishButton.GetComponentInChildren<TMP_Text>();

        var cancelButton = GameUI.CreateButton("CancelButton", adCanvas.transform,
            "Abbrechen (keine Belohnung)", new Vector2(720f, 130f), () => Finish(false));
        PlaceCentered(cancelButton.GetComponent<RectTransform>(), -300f, new Vector2(720f, 130f));
        cancelGo = cancelButton.gameObject;

        adCanvas.gameObject.SetActive(false);
    }

    static void PlaceCentered(RectTransform rt, float yOffset, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, yOffset);
        rt.sizeDelta = size;
    }
}
