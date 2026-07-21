using System;

/// Abstraction over the ad provider. For production, write a class that
/// implements this interface using the provider's SDK (e.g. Unity LevelPlay,
/// AdMob) — the rest of the game stays unchanged.
public interface IAdsProvider
{
    void Initialize();

    bool InterstitialReady { get; }
    bool RewardedReady { get; }

    /// Shows an interstitial; onClosed is invoked after it is dismissed.
    void ShowInterstitial(Action onClosed);

    /// Shows a rewarded video; onFinished(true) only if the reward was
    /// earned (video watched to the end), otherwise onFinished(false).
    void ShowRewarded(Action<bool> onFinished);
}
