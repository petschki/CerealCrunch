using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// Adds a "9:19.5 Portrait" aspect to the Game view and selects it once per
/// editor session, so the editor preview matches the phone layout.
/// The Game view size list has no public API, hence the reflection.
[InitializeOnLoad]
public static class PortraitGameView
{
    const string Label = "9:19.5 Portrait";
    const string SessionKey = "CerealCrunch.PortraitApplied";

    static PortraitGameView()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            Apply();
        };
    }

    [MenuItem("Tools/CerealCrunch/Use Portrait Game View")]
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
                // 18:39 == 9:19.5 (aspect ratios must be integers)
                AddCustomSize(asm, group, 18, 39, Label);
                index = FindSize(group, Label);
            }
            if (index < 0)
            {
                Debug.LogWarning("PortraitGameView: could not register the portrait size.");
                return;
            }

            var gameViewType = asm.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetMethod("SizeSelectionCallback")
                .Invoke(window, new object[] { index, null });
            Debug.Log($"PortraitGameView: Game view switched to '{Label}'.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("PortraitGameView: reflection failed (" + e.Message +
                "). Please select a portrait aspect manually in the Game view dropdown.");
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
