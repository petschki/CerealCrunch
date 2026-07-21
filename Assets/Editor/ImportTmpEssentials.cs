using System.IO;
using UnityEditor;
using UnityEngine;

/// Imports the TMP Essential Resources once (default font asset etc.) so
/// runtime-created TextMeshPro components have a font to render with.
[InitializeOnLoad]
public static class ImportTmpEssentials
{
    static ImportTmpEssentials()
    {
        EditorApplication.delayCall += Apply;
    }

    static void Apply()
    {
        if (Directory.Exists("Assets/TextMesh Pro")) return;

        // TMP moved from its own package into com.unity.ugui in newer Unity versions
        string[] candidates =
        {
            "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
            "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage"
        };
        foreach (var candidate in candidates)
        {
            string fullPath;
            try { fullPath = Path.GetFullPath(candidate); }
            catch { continue; }
            if (!File.Exists(fullPath)) continue;

            AssetDatabase.ImportPackage(fullPath, false);
            Debug.Log("ImportTmpEssentials: TMP Essential Resources imported.");
            return;
        }
        Debug.LogWarning("ImportTmpEssentials: package not found — import manually via " +
            "Window > TextMeshPro > Import TMP Essential Resources.");
    }
}
