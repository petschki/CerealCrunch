using UnityEditor;
using UnityEngine;

/// Locks the app to landscape orientation (relevant for device builds).
[InitializeOnLoad]
public static class ConfigureLandscape
{
    static ConfigureLandscape()
    {
        EditorApplication.delayCall += Apply;
    }

    public static void Apply()
    {
        if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation &&
            !PlayerSettings.allowedAutorotateToPortrait &&
            PlayerSettings.allowedAutorotateToLandscapeRight)
            return;

        // Auto-rotation restricted to the two landscape orientations:
        // the game follows a 180° flip of the phone, but never goes portrait.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        AssetDatabase.SaveAssets();
        Debug.Log("ConfigureLandscape: app locked to landscape orientation.");
    }
}
