using UnityEngine;

/// Shared IMGUI styling for the chunky casual look.
/// Textures live in Resources/Cereals (ui_button, ui_button_pressed).
public static class GameGui
{
    static GUIStyle buttonStyle;

    public static GUIStyle Button
    {
        get
        {
            if (buttonStyle == null)
            {
                var up = Resources.Load<Texture2D>("Cereals/ui_button");
                var down = Resources.Load<Texture2D>("Cereals/ui_button_pressed");
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    // 9-slice borders matching the PNG's rounded corners and lip
                    border = new RectOffset(36, 36, 34, 44),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                buttonStyle.normal.background = up;
                buttonStyle.hover.background = up;
                buttonStyle.focused.background = up;
                buttonStyle.active.background = down != null ? down : up;
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.hover.textColor = Color.white;
                buttonStyle.focused.textColor = Color.white;
                buttonStyle.active.textColor = new Color(1f, 0.93f, 0.8f);
            }
            buttonStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.028f);
            return buttonStyle;
        }
    }

    /// Draws the label twice (dark offset + colored) for a soft drop shadow,
    /// keeping text readable on any background.
    public static void ShadowLabel(Rect rect, string text, GUIStyle style, Color color)
    {
        Color original = style.normal.textColor;
        style.normal.textColor = new Color(0.16f, 0.09f, 0.03f, 0.9f);
        GUI.Label(new Rect(rect.x + 2, rect.y + 3, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
        style.normal.textColor = original;
    }
}
