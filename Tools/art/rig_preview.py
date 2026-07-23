"""Composes the Cerealia cutout rig in 2D to tune joint offsets before they go
into CerealiaRig.cs. Coordinates are in source pixels, origin = hip joint,
y up. Run: uv run --with pillow python rig_preview.py [pose]
"""
import math
import sys

from PIL import Image

D = "../../Assets/Resources/Cerealia/"

# joint layout (must stay in sync with CerealiaRig.cs)
HIP = (0, 0)
SHOULDER_L = (-92, 228)   # viewer left = her right; where the bare arm
SHOULDER_R = (92, 228)    # emerges from the sleeve painted on the torso
NECK = (0, 322)
ELBOW_LEN = 74            # distance shoulder -> elbow along the upper arm

# Arm angles: 0° = hanging down. Positive swings the arm to the viewer's
# right, so the right arm needs positive values to raise outward, the left
# arm negative ones.
POSES = {
    "idle":  dict(armL=(-10, -6), armR=(10, 6), head=0, hair="smile"),
    "wave":  dict(armL=(-10, -6), armR=(142, 26), head=6, hair="open"),
    "cheer": dict(armL=(-138, -22), armR=(138, 22), head=0, hair="open"),
}


def load(name):
    return Image.open(D + name + ".png")


def place(canvas, img, joint, anchor, angle_deg, origin):
    """Rotates img around its anchor (fraction of size) and pastes it so the
    anchor lands on joint (rig coords). Returns the rotated image + position."""
    rot = img.rotate(angle_deg, resample=Image.BICUBIC, expand=True)
    ax, ay = anchor[0] * img.width, anchor[1] * img.height
    # anchor position after rotation around the image center with expand
    cx, cy = img.width / 2, img.height / 2
    dx, dy = ax - cx, ay - cy
    a = math.radians(angle_deg)
    rx = dx * math.cos(a) + dy * math.sin(a)
    ry = -dx * math.sin(a) + dy * math.cos(a)
    ax2, ay2 = rot.width / 2 + rx, rot.height / 2 + ry
    px = int(origin[0] + joint[0] - ax2)
    py = int(origin[1] - joint[1] - ay2)
    canvas.alpha_composite(rot, (px, py))
    return rot, (px, py)


def elbow_of(shoulder, angle_deg):
    a = math.radians(angle_deg - 90)  # 0° = hanging down
    return (shoulder[0] + ELBOW_LEN * math.cos(a),
            shoulder[1] + ELBOW_LEN * math.sin(a))


def compose(pose_name):
    p = POSES[pose_name]
    head = load("head_open" if p["hair"] == "open" else "head_smile")
    torso, legs = load("torso"), load("legs")
    aLU, aLL = load("arm_l_upper"), load("arm_l_lower")
    aRU, aRL = load("arm_r_upper"), load("arm_r_lower")

    canvas = Image.new("RGBA", (900, 1100), (245, 240, 230, 255))
    origin = (450, 780)  # hip position on canvas

    # back arm (her right / viewer left)
    up, lo = p["armL"]
    place(canvas, aLU, SHOULDER_L, (0.5, 0.12), up, origin)
    place(canvas, aLL, elbow_of(SHOULDER_L, up), (0.5, 0.08), up + lo, origin)

    place(canvas, legs, HIP, (0.5, 0.06), 0, origin)
    place(canvas, torso, HIP, (0.5, 0.94), 0, origin)
    place(canvas, head, NECK, (0.5, 0.88), p["head"], origin)

    # front arm sits on top of the head so raised gestures stay visible
    up, lo = p["armR"]
    place(canvas, aRU, SHOULDER_R, (0.5, 0.12), up, origin)
    place(canvas, aRL, elbow_of(SHOULDER_R, up), (0.5, 0.08), up + lo, origin)
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
