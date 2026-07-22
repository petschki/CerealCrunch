using UnityEngine;

/// Persistent meta progression: stars earned in match-3 levels and the
/// renovation stage of the ground-floor café. Stage artwork lives in
/// Resources/CerealCrunchCafe (cafe_state_0..4 are placeholders until the
/// real Nano-Banana renders replace them; cafe_final is the finished room).
public static class RenovationState
{
    const string StarsKey = "cereal_stars";
    const string StageKey = "cereal_cafe_stage";
    const string StoryKey = "cereal_story_seen";

    /// 0 = Ruine, MaxStage = fertiges Café
    public const int MaxStage = 5;

    // Label + star cost of the NEXT renovation step at a given stage
    public static readonly string[] StepLabels =
    {
        "Schutt räumen & entrümpeln",
        "Boden schleifen & Wände streichen",
        "Fenster, Türen & Treppe erneuern",
        "Theke & Küche einbauen",
        "Möbel, Deko — Eröffnung!"
    };
    public static readonly int[] StepCosts = { 1, 1, 1, 1, 2 };

    public static int Stars
    {
        get => PlayerPrefs.GetInt(StarsKey, 0);
        set { PlayerPrefs.SetInt(StarsKey, value); PlayerPrefs.Save(); }
    }

    public static int Stage
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(StageKey, 0), 0, MaxStage);
        set { PlayerPrefs.SetInt(StageKey, Mathf.Clamp(value, 0, MaxStage)); PlayerPrefs.Save(); }
    }

    public static bool StorySeen
    {
        get => PlayerPrefs.GetInt(StoryKey, 0) == 1;
        set { PlayerPrefs.SetInt(StoryKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static bool CafeComplete => Stage >= MaxStage;
    public static int NextCost => CafeComplete ? 0 : StepCosts[Stage];
    public static string NextLabel => CafeComplete ? "" : StepLabels[Stage];
    public static bool CanRenovate => !CafeComplete && Stars >= NextCost;

    /// Spends the stars and advances one stage.
    public static void PayRenovation()
    {
        if (!CanRenovate) return;
        Stars -= NextCost;
        Stage++;
    }

    public static Sprite StageSprite(int stage)
    {
        string path = stage >= MaxStage
            ? "CerealCrunchCafe/cafe_final"
            : $"CerealCrunchCafe/cafe_state_{stage}";
        return Resources.Load<Sprite>(path);
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(StarsKey);
        PlayerPrefs.DeleteKey(StageKey);
        PlayerPrefs.DeleteKey(StoryKey);
    }
}
