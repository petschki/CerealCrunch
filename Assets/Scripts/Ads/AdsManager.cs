using System;
using UnityEngine;

/// Central entry point for ads, with frequency capping for interstitials.
/// Creates itself on first use and survives scene changes.
public class AdsManager : MonoBehaviour
{
    // Interstitials only once the player is invested, and never too frequent
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

    /// While true, the game should neither accept input nor draw its own UI.
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

    /// Shows an interstitial if level and cooldown allow it — otherwise
    /// (or if none is loaded) continues directly with onClosed.
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
