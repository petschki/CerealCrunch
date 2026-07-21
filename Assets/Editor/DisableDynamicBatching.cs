using System.Reflection;
using UnityEditor;
using UnityEngine;

/// Dynamic Batching ist in Unity 6 deprecated und standardmäßig trotzdem aktiv.
/// Dieses Skript schaltet es beim Editor-Start für alle Ziel-Plattformen ab
/// (Static Batching bleibt an).
[InitializeOnLoad]
public static class DisableDynamicBatching
{
    static DisableDynamicBatching()
    {
        EditorApplication.delayCall += Apply;
    }

    static void Apply()
    {
        // SetBatchingForPlatform ist je nach Unity-Version internal — daher Reflection
        var set = typeof(PlayerSettings).GetMethod("SetBatchingForPlatform",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var get = typeof(PlayerSettings).GetMethod("GetBatchingForPlatform",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (set == null) return;

        BuildTarget[] targets =
        {
            BuildTarget.StandaloneOSX,
            BuildTarget.iOS,
            BuildTarget.Android
        };

        bool changed = false;
        foreach (var target in targets)
        {
            if (get != null)
            {
                var args = new object[] { target, 0, 0 };
                get.Invoke(null, args);
                if ((int)args[2] == 0) continue; // Dynamic Batching ist schon aus
            }
            set.Invoke(null, new object[] { target, 1, 0 }); // static an, dynamic aus
            changed = true;
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("DisableDynamicBatching: Dynamic Batching wurde deaktiviert.");
        }
    }
}
