using System;

/// Abstraktion über den Werbeanbieter. Für den echten Betrieb eine Klasse
/// schreiben, die dieses Interface mit dem SDK des Anbieters implementiert
/// (z.B. Unity LevelPlay, AdMob) — der Rest des Spiels bleibt unverändert.
public interface IAdsProvider
{
    void Initialize();

    bool InterstitialReady { get; }
    bool RewardedReady { get; }

    /// Zeigt ein Interstitial; onClosed wird nach dem Schließen aufgerufen.
    void ShowInterstitial(Action onClosed);

    /// Zeigt ein Rewarded Video; onFinished(true) nur, wenn die Belohnung
    /// verdient wurde (Video zu Ende geschaut), sonst onFinished(false).
    void ShowRewarded(Action<bool> onFinished);
}
