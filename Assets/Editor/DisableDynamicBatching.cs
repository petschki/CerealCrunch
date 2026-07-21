using System.Reflection;
using UnityEditor;
using UnityEngine;

/// Dynamic batching is deprecated in Unity 6 yet still enabled by default.
/// This script disables it for all target platforms on editor startup
/// (static batching stays enabled).
[InitializeOnLoad]
public static class DisableDynamicBatching
{
    static DisableDynamicBatching()
    {
        EditorApplication.delayCall += Apply;
    }

    static void Apply()
    {
        // SetBatchingForPlatform is internal in some Unity versions — hence reflection
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
                if ((int)args[2] == 0) continue; // dynamic batching already off
            }
            set.Invoke(null, new object[] { target, 1, 0 }); // static on, dynamic off
            changed = true;
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("DisableDynamicBatching: dynamic batching has been disabled.");
        }
    }
}
