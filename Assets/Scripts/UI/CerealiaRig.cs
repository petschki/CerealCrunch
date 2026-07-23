using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Cutout rig for Cerealia: the character sheet parts (Resources/Cerealia)
/// are assembled into a joint hierarchy and animated by rotating the joints —
/// the classic paper-doll technique, no extra packages needed.
///
/// Joint layout is in source-sheet pixels with the hip at (0, 0) and y up;
/// the numbers were tuned with Tools/art/rig_preview.py, keep both in sync.
/// Angles: 0° = arm hanging down, positive rotates counter-clockwise, so the
/// front arm (viewer right) needs positive values to raise outward.
public class CerealiaRig : MonoBehaviour
{
    static readonly Vector2 ShoulderBack = new Vector2(-92f, 228f);
    static readonly Vector2 ShoulderFront = new Vector2(92f, 228f);
    static readonly Vector2 Neck = new Vector2(0f, 322f);
    const float ElbowLength = 74f;
    const float FootDrop = 295f; // hip → sole, for placing her on the floor

    struct Pose
    {
        public float BackUpper, BackLower, FrontUpper, FrontLower, Head;
        public bool OpenMouth;
    }

    static readonly Pose Idle = new Pose
    { BackUpper = -10f, BackLower = -6f, FrontUpper = 10f, FrontLower = 6f, Head = 0f };
    static readonly Pose Waving = new Pose
    { BackUpper = -10f, BackLower = -6f, FrontUpper = 142f, FrontLower = 26f, Head = 6f, OpenMouth = true };
    static readonly Pose Cheering = new Pose
    { BackUpper = -138f, BackLower = -22f, FrontUpper = 138f, FrontLower = 22f, Head = 0f, OpenMouth = true };

    RectTransform body, backShoulder, backElbow, frontShoulder, frontElbow, headJoint, torso;
    Image headImage;
    Sprite headSmile, headOpen;
    Pose current = Idle;
    Coroutine gesture;
    float idlePhase;

    /// Builds the rig under `parent`. `height` is her total height in canvas
    /// units; `floorPosition` is where her feet should stand (anchored to the
    /// parent's centre).
    public static CerealiaRig Create(RectTransform parent, float height, Vector2 floorPosition)
    {
        var root = GameUI.CreateRect("Cerealia", parent);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = Vector2.zero;

        var rig = root.gameObject.AddComponent<CerealiaRig>();
        rig.Build();

        // source sheet: sole at -FootDrop, top of head ≈ +561
        const float sourceHeight = FootDrop + 561f;
        float scale = height / sourceHeight;
        root.localScale = new Vector3(scale, scale, 1f);
        root.anchoredPosition = floorPosition + new Vector2(0f, FootDrop * scale);
        return rig;
    }

    void Build()
    {
        headSmile = Resources.Load<Sprite>("Cerealia/head_smile");
        headOpen = Resources.Load<Sprite>("Cerealia/head_open");

        body = GameUI.CreateRect("Body", transform);
        body.anchorMin = body.anchorMax = new Vector2(0.5f, 0.5f);
        body.sizeDelta = Vector2.zero;

        // sibling order = draw order: back arm, legs, torso, head, front arm
        BuildArm("Back", ShoulderBack, "arm_l_upper", "arm_l_lower",
            out backShoulder, out backElbow);
        AddPart("Legs", body, "legs", Vector2.zero, new Vector2(0.5f, 0.94f));
        torso = AddPart("Torso", body, "torso", Vector2.zero, new Vector2(0.5f, 0.06f)).rectTransform;

        headJoint = Joint("HeadJoint", body, Neck);
        headImage = AddPart("Head", headJoint, "head_smile", Vector2.zero, new Vector2(0.5f, 0.12f));

        BuildArm("Front", ShoulderFront, "arm_r_upper", "arm_r_lower",
            out frontShoulder, out frontElbow);

        ApplyPose(Idle, 1f);
    }

    void BuildArm(string name, Vector2 shoulder, string upperSprite, string lowerSprite,
        out RectTransform shoulderJoint, out RectTransform elbowJoint)
    {
        shoulderJoint = Joint(name + "Shoulder", body, shoulder);
        AddPart(name + "Upper", shoulderJoint, upperSprite, Vector2.zero, new Vector2(0.5f, 0.88f));
        elbowJoint = Joint(name + "Elbow", shoulderJoint, new Vector2(0f, -ElbowLength));
        AddPart(name + "Lower", elbowJoint, lowerSprite, Vector2.zero, new Vector2(0.5f, 0.92f));
    }

    static RectTransform Joint(string name, Transform parent, Vector2 position)
    {
        var rt = GameUI.CreateRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = position;
        return rt;
    }

    static Image AddPart(string name, Transform parent, string sprite, Vector2 position, Vector2 pivot)
    {
        var rt = GameUI.CreateRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = pivot;
        rt.anchoredPosition = position;

        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = Resources.Load<Sprite>("Cerealia/" + sprite);
        image.raycastTarget = false;
        if (image.sprite != null)
            rt.sizeDelta = image.sprite.rect.size;
        return image;
    }

    // ---------- public API ----------

    public void Wave() => Play(Waving, 2.6f, frontElbow, 20f, 3.4f);
    public void Cheer() => Play(Cheering, 1.6f, null, 0f, 0f);

    void Play(Pose pose, float duration, RectTransform shakeJoint, float shakeAngle, float shakeSpeed)
    {
        if (!isActiveAndEnabled) return;
        if (gesture != null) StopCoroutine(gesture);
        gesture = StartCoroutine(GestureRoutine(pose, duration, shakeJoint, shakeAngle, shakeSpeed));
    }

    IEnumerator GestureRoutine(Pose pose, float duration, RectTransform shakeJoint,
        float shakeAngle, float shakeSpeed)
    {
        yield return BlendRoutine(current, pose, 0.28f);

        float baseAngle = shakeJoint != null ? shakeJoint.localEulerAngles.z : 0f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (shakeJoint != null)
                shakeJoint.localRotation = Quaternion.Euler(0f, 0f,
                    baseAngle + Mathf.Sin(t * shakeSpeed * Mathf.PI) * shakeAngle);
            yield return null;
        }

        yield return BlendRoutine(pose, Idle, 0.35f);
        gesture = null;
    }

    IEnumerator BlendRoutine(Pose from, Pose to, float duration)
    {
        headImage.sprite = to.OpenMouth ? headOpen : headSmile;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            ApplyPose(Lerp(from, to, p), 1f);
            yield return null;
        }
        current = to;
        ApplyPose(to, 1f);
    }

    static Pose Lerp(Pose a, Pose b, float t) => new Pose
    {
        BackUpper = Mathf.Lerp(a.BackUpper, b.BackUpper, t),
        BackLower = Mathf.Lerp(a.BackLower, b.BackLower, t),
        FrontUpper = Mathf.Lerp(a.FrontUpper, b.FrontUpper, t),
        FrontLower = Mathf.Lerp(a.FrontLower, b.FrontLower, t),
        Head = Mathf.Lerp(a.Head, b.Head, t),
        OpenMouth = b.OpenMouth
    };

    void ApplyPose(Pose p, float weight)
    {
        SetAngle(backShoulder, p.BackUpper);
        SetAngle(backElbow, p.BackLower);
        SetAngle(frontShoulder, p.FrontUpper);
        SetAngle(frontElbow, p.FrontLower);
        SetAngle(headJoint, p.Head);
    }

    static void SetAngle(RectTransform rt, float angle)
    {
        if (rt != null) rt.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// Subtle life while no gesture plays: breathing, a slow bob and arm sway.
    void Update()
    {
        idlePhase += Time.deltaTime;
        float breath = Mathf.Sin(idlePhase * 1.9f);
        torso.localScale = new Vector3(1f - breath * 0.008f, 1f + breath * 0.012f, 1f);
        body.anchoredPosition = new Vector2(0f, breath * 2.5f);

        if (gesture != null) return; // gestures own the joints while running
        SetAngle(backShoulder, Idle.BackUpper + breath * 2.5f);
        SetAngle(frontShoulder, Idle.FrontUpper - breath * 2.5f);
        SetAngle(headJoint, Mathf.Sin(idlePhase * 1.3f) * 1.6f);
    }
}
