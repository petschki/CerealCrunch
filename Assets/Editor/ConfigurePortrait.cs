using UnityEditor;
using UnityEngine;

/// Stellt die App auf reines Hochformat um (relevant für Device-Builds).
[InitializeOnLoad]
public static class ConfigurePortrait
{
    static ConfigurePortrait()
    {
        EditorApplication.delayCall += Apply;
    }

    static void Apply()
    {
        if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait &&
            !PlayerSettings.allowedAutorotateToLandscapeLeft)
            return;

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        AssetDatabase.SaveAssets();
        Debug.Log("ConfigurePortrait: App auf Hochformat (Portrait) umgestellt.");
    }
}
