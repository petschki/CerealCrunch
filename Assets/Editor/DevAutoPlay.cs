using UnityEditor;
using UnityEngine;

/// Dev helper for automated visual checks:
///   Unity -projectPath . -executeMethod DevAutoPlay.Run
/// opens the editor, switches to the landscape game view, enters play mode
/// and saves periodic game-view screenshots to Temp/AutoShots/.
[InitializeOnLoad]
public static class DevAutoPlay
{
    const string Flag = "CerealCrunch.AutoShots";
    static double next, nextAction, playStart = -1;
    static int count;
    static bool playPressed;

    static DevAutoPlay()
    {
        EditorApplication.update += OnUpdate;
    }

    public static void Run()
    {
        SessionState.SetBool(Flag, true);
        EditorApplication.delayCall += () =>
        {
            LandscapeGameView.Apply();
            EditorApplication.EnterPlaymode();
        };
    }

    static void OnUpdate()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(Flag, false)) return;
        double now = EditorApplication.timeSinceStartup;
        if (playStart < 0) playStart = now;
        double t = now - playStart;

        // click through the flow so the shots cover story -> café -> board
        if (t > 3.0 && now >= nextAction)
        {
            nextAction = now + 0.8;
            var story = Object.FindFirstObjectByType<StoryIntroScreen>();
            if (story != null)
            {
                story.SendMessage("Advance");
            }
            else if (!playPressed && t > 13.0)
            {
                var cafe = Object.FindFirstObjectByType<CafeScreen>();
                if (cafe != null && cafe.gameObject.activeSelf)
                {
                    cafe.SendMessage("Play");
                    playPressed = true;
                }
            }
        }

        if (count >= 6 || now < next) return;
        next = now + 2.0;
        System.IO.Directory.CreateDirectory("Temp/AutoShots");
        ScreenCapture.CaptureScreenshot($"Temp/AutoShots/shot_{count++}.png");
    }
}
