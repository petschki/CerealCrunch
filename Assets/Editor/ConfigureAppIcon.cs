using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// Sets Assets/Icons/app_icon.png as the default application icon
/// (Unity derives all iOS slot sizes from it at build time).
[InitializeOnLoad]
public static class ConfigureAppIcon
{
    const string IconPath = "Assets/Icons/app_icon.png";

    static ConfigureAppIcon()
    {
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("Tools/CerealCrunch/Apply App Icon")]
    public static void Apply()
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (tex == null) return;

        var current = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application);
        if (current.Length > 0 && current[0] == tex) return;

        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { tex }, IconKind.Application);
        AssetDatabase.SaveAssets();
        Debug.Log("ConfigureAppIcon: app icon set from " + IconPath);
    }
}
