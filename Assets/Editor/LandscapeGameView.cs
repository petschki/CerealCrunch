using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// Adds a "19.5:9 Landscape" aspect to the Game view and selects it once per
/// editor session, so the editor preview matches the phone layout.
/// The Game view size list has no public API, hence the reflection.
[InitializeOnLoad]
public static class LandscapeGameView
{
    const string Label = "19.5:9 Landscape";
    const string SessionKey = "CerealCrunch.LandscapeApplied";

    static LandscapeGameView()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            Apply();
        };
    }

    [MenuItem("Tools/CerealCrunch/Use Landscape Game View")]
    public static void Apply()
    {
        try
        {
            var asm = typeof(Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleType.GetProperty("instance").GetValue(null, null);

            var groupType = instance.GetType().GetProperty("currentGroupType").GetValue(instance, null);
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { groupType });

            int index = FindSize(group, Label);
            if (index < 0)
            {
                // 39:18 == 19.5:9 (aspect ratios must be integers)
                AddCustomSize(asm, group, 39, 18, Label);
                index = FindSize(group, Label);
            }
            if (index < 0)
            {
                Debug.LogWarning("LandscapeGameView: could not register the landscape size.");
                return;
            }

            var gameViewType = asm.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetMethod("SizeSelectionCallback")
                .Invoke(window, new object[] { index, null });
            Debug.Log($"LandscapeGameView: Game view switched to '{Label}'.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("LandscapeGameView: reflection failed (" + e.Message +
                "). Please select a landscape aspect manually in the Game view dropdown.");
        }
    }

    static int FindSize(object group, string label)
    {
        int builtin = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
        int custom = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
        var getSize = group.GetType().GetMethod("GetGameViewSize");
        for (int i = 0; i < builtin + custom; i++)
        {
            var size = getSize.Invoke(group, new object[] { i });
            var text = (string)size.GetType().GetProperty("baseText").GetValue(size, null);
            if (text == label) return i;
        }
        return -1;
    }

    static void AddCustomSize(Assembly asm, object group, int w, int h, string label)
    {
        var sizeType = asm.GetType("UnityEditor.GameViewSize");
        var kindType = asm.GetType("UnityEditor.GameViewSizeType");
        var ctor = sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) });
        var newSize = ctor.Invoke(new[] { Enum.Parse(kindType, "AspectRatio"), (object)w, h, label });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
    }
}
