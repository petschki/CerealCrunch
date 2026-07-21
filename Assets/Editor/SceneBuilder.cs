using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Builds the main scene in batch mode:
/// Unity -batchmode -executeMethod SceneBuilder.Build
public static class SceneBuilder
{
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.985f, 0.945f, 0.87f);
        cam.transform.position = new Vector3(3.5f, 3.8f, -10f);
        cam.orthographicSize = 6f; // CerealBoard adjusts this to the aspect ratio at runtime

        var board = new GameObject("Board");
        board.AddComponent<CerealBoard>();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
        AssetDatabase.SaveAssets();

        Debug.Log("SceneBuilder: Main.unity created.");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
}
