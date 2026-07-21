using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Builds the iOS Xcode project into Builds/iOS.
/// Editor: Tools > CerealCrunch > Build iOS. Batch:
/// Unity -batchmode -executeMethod BuildIos.Build
public static class BuildIos
{
    const string BundleId = "at.kombinat.cerealcrunch";
    const string OutputPath = "Builds/iOS";

    [MenuItem("Tools/CerealCrunch/Build iOS (Xcode Project)")]
    public static void Build()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
        PlayerSettings.companyName = "Kombinat";
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = OutputPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"BuildIos: {report.summary.result} — output: {OutputPath}");
        if (Application.isBatchMode)
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
