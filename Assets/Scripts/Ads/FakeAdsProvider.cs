using System;
using System.Collections;
using UnityEngine;

/// Placeholder provider for development: simulates load times and shows
/// fullscreen fake ads via OnGUI. Behaves like a real SDK (preloading,
/// countdown, cancelling a rewarded ad yields no reward).
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
        current = AdKind.Interstitial;
        remaining = InterstitialDuration;
        StartCoroutine(ReloadInterstitial());
    }

    public void ShowRewarded(Action<bool> onFinished)
    {
        onRewardedFinished = onFinished;
        current = AdKind.Rewarded;
        remaining = RewardedDuration;
        StartCoroutine(ReloadRewarded());
    }

    void Update()
    {
        if (current != AdKind.None && remaining > 0f)
            remaining -= Time.unscaledDeltaTime;
    }

    void Finish(bool rewarded)
    {
        var kind = current;
        current = AdKind.None;
        if (kind == AdKind.Interstitial)
            onInterstitialClosed?.Invoke();
        else
            onRewardedFinished?.Invoke(rewarded);
    }

    void OnGUI()
    {
        if (current == AdKind.None) return;
        GUI.depth = -100; // draw on top of everything else

        GUI.color = new Color(0.09f, 0.11f, 0.18f, 0.98f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var big = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(Screen.height * 0.05f),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        big.normal.textColor = Color.white;

        var small = new GUIStyle(big) { fontSize = Mathf.RoundToInt(Screen.height * 0.03f) };
        small.normal.textColor = new Color(0.75f, 0.78f, 0.88f);

        var button = GameGui.Button;

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Label(new Rect(0, cy - Screen.height * 0.2f, Screen.width, Screen.height * 0.1f),
            current == AdKind.Rewarded ? "REWARDED-WERBUNG" : "WERBUNG", big);
        GUI.Label(new Rect(0, cy - Screen.height * 0.1f, Screen.width, Screen.height * 0.06f),
            "(Platzhalter — hier läuft später das Anbieter-SDK)", small);

        if (remaining > 0f)
        {
            GUI.Label(new Rect(0, cy, Screen.width, Screen.height * 0.08f),
                Mathf.CeilToInt(remaining).ToString(), big);

            // Rewarded ads can be cancelled — no reward in that case
            if (current == AdKind.Rewarded &&
                GUI.Button(new Rect(cx - 140, cy + Screen.height * 0.12f, 280, Screen.height * 0.06f),
                    "Abbrechen (keine Belohnung)", button))
                Finish(false);
        }
        else
        {
            string label = current == AdKind.Rewarded ? "Belohnung abholen" : "Schließen";
            if (GUI.Button(new Rect(cx - 140, cy + Screen.height * 0.02f, 280, Screen.height * 0.07f), label, button))
                Finish(true);
        }
    }
}
