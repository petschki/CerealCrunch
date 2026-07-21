using UnityEditor;
using UnityEngine;

/// Locks the app to portrait orientation (relevant for device builds).
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
        Debug.Log("ConfigurePortrait: app locked to portrait orientation.");
    }
}
