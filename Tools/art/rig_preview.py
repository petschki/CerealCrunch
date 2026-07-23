"""Composes the Cerealia cutout rig in 2D to tune joint offsets before they go
into CerealiaRig.cs. Coordinates are in source pixels, origin = hip joint,
y up. Run: uv run --with pillow python rig_preview.py [pose ...]

Parts come from cerealia_parts2.png: sleeveless torso (with shorts), upper
arms carrying the striped sleeve, head with a neck stub, separate legs.
"""
import math
import sys

from PIL import Image

D = "../../Assets/Resources/Cerealia/"

# joint layout — must stay in sync with CerealiaRig.cs
SHOULDER_L = (-80, 385)   # viewer left = her right
SHOULDER_R = (80, 385)
NECK = (5, 406)   # stub tucked behind the collar
HIP_L = (-43, 5)
HIP_R = (43, 5)
ELBOW_LENGTH = 210

# anchors as (x, y) fractions of the part image, y from the TOP (PIL order)
ANCHOR = {
    "head": (0.55, 1.0),      # neck stub, bottom edge
    "torso": (0.49, 1.0),     # hip, bottom edge
    "upper": (0.5, 0.07),     # shoulder, top
    "lower": (0.5, 0.05),     # elbow, top
    "leg": (0.5, 0.03),       # hip, top
}

# Arm angles: 0° = hanging down, positive rotates counter-clockwise, so the
# right arm needs positive values to raise outward.
POSES = {
    "idle":  dict(armL=(-8, -5), armR=(8, 5), head=0, hair="smile"),
    "wave":  dict(armL=(-8, -5), armR=(140, 24), head=6, hair="open"),
    "cheer": dict(armL=(-136, -20), armR=(136, 20), head=0, hair="open"),
}


def load(name):
    return Image.open(D + name + ".png")


def place(canvas, img, joint, anchor, angle_deg, origin):
    """Rotates img around its anchor and pastes it so the anchor lands on the
    joint (rig coords, y up)."""
    rot = img.rotate(angle_deg, resample=Image.BICUBIC, expand=True)
    ax, ay = anchor[0] * img.width, anchor[1] * img.height
    cx, cy = img.width / 2, img.height / 2
    dx, dy = ax - cx, ay - cy
    a = math.radians(angle_deg)
    rx = dx * math.cos(a) + dy * math.sin(a)
    ry = -dx * math.sin(a) + dy * math.cos(a)
    canvas.alpha_composite(rot, (int(origin[0] + joint[0] - (rot.width / 2 + rx)),
                                 int(origin[1] - joint[1] - (rot.height / 2 + ry))))


def elbow_of(shoulder, angle_deg):
    a = math.radians(angle_deg - 90)  # 0° = hanging down
    return (shoulder[0] + ELBOW_LENGTH * math.cos(a),
            shoulder[1] + ELBOW_LENGTH * math.sin(a))


def compose(pose_name):
    p = POSES[pose_name]
    head = load("head_open" if p["hair"] == "open" else "head_smile")
    canvas = Image.new("RGBA", (1100, 1400), (245, 240, 230, 255))
    origin = (550, 1020)  # hip position on canvas

    up, lo = p["armL"]
    place(canvas, load("arm_l_upper"), SHOULDER_L, ANCHOR["upper"], up, origin)
    place(canvas, load("arm_l_lower"), elbow_of(SHOULDER_L, up), ANCHOR["lower"], up + lo, origin)

    place(canvas, load("leg_l"), HIP_L, ANCHOR["leg"], 0, origin)
    place(canvas, load("leg_r"), HIP_R, ANCHOR["leg"], 0, origin)
    place(canvas, load("torso"), (0, 0), ANCHOR["torso"], 0, origin)
    place(canvas, head, NECK, ANCHOR["head"], p["head"], origin)

    # front arm on top of the head so raised gestures stay visible
    up, lo = p["armR"]
    place(canvas, load("arm_r_upper"), SHOULDER_R, ANCHOR["upper"], up, origin)
    place(canvas, load("arm_r_lower"), elbow_of(SHOULDER_R, up), ANCHOR["lower"], up + lo, origin)
    return canvas


if __name__ == "__main__":
    poses = sys.argv[1:] or list(POSES)
    imgs = [compose(n) for n in poses]
    sheet = Image.new("RGB", (sum(i.width for i in imgs), imgs[0].height))
    x = 0
    for i in imgs:
        sheet.paste(i.convert("RGB"), (x, 0))
        x += i.width
    sheet.save("../../Temp/rig_preview.png")
    print("ok ->", poses)
