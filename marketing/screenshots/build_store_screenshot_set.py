"""Build store screenshots from real game captures (spec D-030).

Captures come from the store capture mode on the "Tread Shred iPhone 6.9"
and "Tread Shred iPad 12.9" simulators and live in generated/<device>/.
Images are never painted over with effects the game does not render; only a
headline band is added.
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
HERE = ROOT / "marketing" / "screenshots"
FONT = ROOT / "Assets" / "Art" / "BlackOpsOne-Regular.ttf"
GOLD = (255, 184, 71, 255)
WHITE = (255, 255, 255, 255)

# (capture name, headline, subline) in store order.
SHOTS = [
    ("combat", "40 MISSIONS OF ARMORED WARFARE", "BANK YOUR SHOTS. DESTROY EVERY TANK."),
    ("upgrades", "BUILD YOUR WAR MACHINE", "UPGRADE ARMOR, FIREPOWER AND SPEED"),
    ("tiers", "BEAT 40. UNLOCK VETERAN AND ELITE.", "TOUGHER ENEMIES. BIGGER PAYOUTS."),
    ("victory", "EVERY MISSION PAYS", "EARN COINS AND ARM OVERDRIVE"),
    ("revive", "TANK DOWN? REVIVE.", "FINISH THE FIGHT WITH FULL ARMOR"),
    ("start", "NO ADS. JUST COMBAT.", "FREE TO PLAY"),
]

# (output folder, capture device, output size or None for native capture size)
SETS = [
    ("apple-iphone-6.9-landscape", "iphone", None),
    ("apple-ipad-12.9-landscape", "ipad", None),
    ("google-play-landscape", "iphone", (1280, 720)),
]


def fit_to(image, size):
    """Center-crop to the target aspect ratio, then resize."""
    width, height = image.size
    target = size[0] / size[1]
    if width / height > target:
        new_width = round(height * target)
        left = (width - new_width) // 2
        image = image.crop((left, 0, left + new_width, height))
    else:
        new_height = round(width / target)
        top = (height - new_height) // 2
        image = image.crop((0, top, width, top + new_height))
    return image.resize(size, Image.LANCZOS)


def centered(draw, y, text, size, fill, width):
    font = ImageFont.truetype(str(FONT), size)
    while draw.textlength(text, font=font) > width * 0.92 and size > 10:
        size -= 2
        font = ImageFont.truetype(str(FONT), size)
    stroke = max(2, size // 18)
    box = draw.textbbox((0, 0), text, font=font, stroke_width=stroke)
    draw.text(((width - (box[2] - box[0])) / 2, y), text, font=font, fill=fill,
              stroke_width=stroke, stroke_fill=(0, 0, 0, 255))
    return box[3] - box[1]


def compose(capture, headline, subline):
    """Solid headline band on top; the real capture scaled to fit below it."""
    width, height = capture.size
    band = int(height * (0.22 if width / height > 1.6 else 0.17))
    canvas = Image.new("RGB", (width, height), (4, 9, 12))
    scale = min(width / capture.width, (height - band) / capture.height)
    shot = capture.resize((round(capture.width * scale), round(capture.height * scale)), Image.LANCZOS)
    canvas.paste(shot, ((width - shot.width) // 2, band + (height - band - shot.height) // 2))
    draw = ImageDraw.Draw(canvas)
    draw.line((0, band - 3, width, band - 3), fill=(255, 92, 20), width=max(3, height // 300))
    top = int(band * 0.12)
    used = centered(draw, top, headline, int(band * 0.40), GOLD, width)
    centered(draw, top + used + int(band * 0.12), subline, int(band * 0.20), WHITE, width)
    return canvas


def main():
    for folder, device, size in SETS:
        out = HERE / "store-ready" / folder
        out.mkdir(parents=True, exist_ok=True)
        for old in out.glob("*.jpg"):
            old.unlink()
        for index, (name, headline, subline) in enumerate(SHOTS, start=1):
            capture = Image.open(HERE / "generated" / device / f"{name}.png").convert("RGB")
            if size:
                capture = fit_to(capture, size)
            compose(capture, headline, subline).save(out / f"{index:02d}-{name}.jpg", quality=92)
        print(folder, len(SHOTS))


if __name__ == "__main__":
    main()
