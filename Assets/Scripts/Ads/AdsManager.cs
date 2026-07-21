using System;
using UnityEngine;

/// Zentrale Anlaufstelle für Werbung, mit Frequency Capping für Interstitials.
/// Erstellt sich bei erster Verwendung selbst und überlebt Szenenwechsel.
public class AdsManager : MonoBehaviour
{
    // Interstitials erst, wenn der Spieler "investiert" ist, und nie zu dicht
    const int MinLevelForInterstitials = 3;
    const float InterstitialCooldownSeconds = 75f;

    static AdsManager instance;

    IAdsProvider provider;
    float lastInterstitialTime = float.NegativeInfinity;

    public static AdsManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("AdsManager");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AdsManager>();
            }
            return instance;
        }
    }

    /// Solange true, soll das Spiel weder Eingaben annehmen noch eigene UI zeichnen.
    public static bool IsShowingAd { get; private set; }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        provider = gameObject.AddComponent<FakeAdsProvider>();
        provider.Initialize();
    }

    public bool RewardedAvailable => provider.RewardedReady;

    /// Zeigt ein Interstitial, wenn Level und Cooldown es erlauben — sonst
    /// (oder wenn keines geladen ist) geht es direkt mit onClosed weiter.
    public void MaybeShowInterstitial(int completedLevel, Action onClosed)
    {
        bool allowed = completedLevel >= MinLevelForInterstitials
            && Time.realtimeSinceStartup - lastInterstitialTime >= InterstitialCooldownSeconds
            && provider.InterstitialReady;
        if (!allowed)
        {
            onClosed();
            return;
        }

        IsShowingAd = true;
        provider.ShowInterstitial(() =>
        {
            IsShowingAd = false;
            lastInterstitialTime = Time.realtimeSinceStartup;
            onClosed();
        });
    }

    public void ShowRewarded(Action<bool> onFinished)
    {
        if (!provider.RewardedReady)
        {
            onFinished(false);
            return;
        }

        IsShowingAd = true;
        provider.ShowRewarded(earned =>
        {
            IsShowingAd = false;
            onFinished(earned);
        });
    }
}
