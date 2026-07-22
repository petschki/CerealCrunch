using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// One-time story intro: Großtante Ottilie schenkt Cerealia das alte Haus.
/// Key art as backdrop, comic-style dialog pages, tap to advance.
public class StoryIntroScreen : MonoBehaviour
{
    struct Page
    {
        public string Portrait;
        public string Speaker;
        public string Text;
    }

    static readonly Page[] Pages =
    {
        new Page
        {
            Portrait = "Cereals/aunt",
            Speaker = "Großtante Ottilie",
            Text = "Meine liebe Cerealia!\nMein altes Haus in der Stadt gehört jetzt Dir. " +
                   "Es hat schon bessere Tage gesehen — aber ich weiß: " +
                   "Du machst etwas Wunderbares daraus.\nIn Liebe, Deine Großtante"
        },
        new Page
        {
            Portrait = "Cereals/cerealia",
            Speaker = "Cerealia",
            Text = "Ein eigenes Haus?! Das Erdgeschoss wäre PERFEKT für mein kleines " +
                   "Frühstückscafé...\nAber zuerst muss hier ordentlich renoviert werden!"
        },
        new Page
        {
            Portrait = "Cereals/cerealia",
            Speaker = "Cerealia",
            Text = "Hilf mir dabei: Löse Müsli-Rätsel, verdiene Sterne — und bau mit mir " +
                   "Stück für Stück das Café auf!"
        }
    };

    Image portrait;
    TMP_Text speakerText, bodyText, tapHint;
    Action onDone;
    int page;

    public static void Show(Action doneAction)
    {
        GameUI.PushModal();
        var canvas = GameUI.CreateCanvas("StoryIntro", 60);
        var screen = canvas.gameObject.AddComponent<StoryIntroScreen>();
        screen.onDone = doneAction;
        screen.Build();
        screen.ShowPage(0);
    }

    void Build()
    {
        // key art backdrop, crop-filled
        var holder = GameUI.CreateRect("Backdrop", transform);
        GameUI.Stretch(holder);
        holder.gameObject.AddComponent<RectMask2D>();
        var art = GameUI.CreateRect("KeyArt", holder);
        GameUI.Stretch(art);
        var artImage = art.gameObject.AddComponent<Image>();
        artImage.sprite = Resources.Load<Sprite>("CerealCrunchCafe/keyart_title");
        var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 1408f / 768f;

        // soft dim so the dialog pops
        var dim = GameUI.CreateRect("Dim", transform);
        GameUI.Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        // full-screen tap catcher
        var tap = dim.gameObject.AddComponent<Button>();
        tap.transition = Selectable.Transition.None;
        tap.onClick.AddListener(Advance);

        // dialog panel along the bottom
        var panel = GameUI.CreatePanel("Dialog", transform, new Color(0.99f, 0.97f, 0.92f, 0.97f));
        var prt = panel.rectTransform;
        prt.anchorMin = new Vector2(0.5f, 0f);
        prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0f, 40f);
        prt.sizeDelta = new Vector2(1720f, 420f);

        var portraitRect = GameUI.CreateRect("Portrait", panel.transform);
        portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(36f, 0f);
        portraitRect.sizeDelta = new Vector2(320f, 320f);
        portrait = portraitRect.gameObject.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;

        speakerText = GameUI.CreateText("Speaker", panel.transform, "", 52f, new Color(0.72f, 0.34f, 0.1f));
        speakerText.alignment = TextAlignmentOptions.Left;
        var srt = speakerText.rectTransform;
        srt.anchorMin = new Vector2(0f, 1f);
        srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.offsetMin = new Vector2(400f, -110f);
        srt.offsetMax = new Vector2(-40f, -30f);

        bodyText = GameUI.CreateText("Body", panel.transform, "", 44f, new Color(0.25f, 0.16f, 0.08f));
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.fontStyle = FontStyles.Normal;
        if (GameUI.BodyFont != null) bodyText.font = GameUI.BodyFont; // Fließtext: runde Lesevariante
        var brt = bodyText.rectTransform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(400f, 24f);
        brt.offsetMax = new Vector2(-40f, -120f);

        tapHint = GameUI.CreateText("TapHint", panel.transform, "Weiter »", 36f, new Color(0.6f, 0.45f, 0.3f));
        var trt = tapHint.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(1f, 0f);
        trt.pivot = new Vector2(1f, 0f);
        trt.anchoredPosition = new Vector2(-36f, 18f);
        trt.sizeDelta = new Vector2(260f, 54f);
    }

    void ShowPage(int index)
    {
        page = index;
        var p = Pages[index];
        portrait.sprite = Resources.Load<Sprite>(p.Portrait);
        speakerText.text = p.Speaker;
        bodyText.text = p.Text;
        tapHint.text = index == Pages.Length - 1 ? "Los geht's »" : "Weiter »";
    }

    void Advance()
    {
        AudioManager.Play("button");
        if (page + 1 < Pages.Length)
        {
            ShowPage(page + 1);
            return;
        }
        GameUI.PopModal();
        var callback = onDone;
        onDone = null;
        Destroy(gameObject);
        callback?.Invoke();
    }
}
