from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "marketing" / "screenshots" / "generated"
OUT = ROOT / "marketing" / "screenshots"
FONT = ROOT / "Assets" / "Art" / "BlackOpsOne-Regular.ttf"


def font(size: int):
    return ImageFont.truetype(str(FONT), size)


def text_center(draw, xy, value, size, fill, stroke=0, stroke_fill=(0, 0, 0, 255)):
    f = font(size)
    box = draw.textbbox((0, 0), value, font=f, stroke_width=stroke)
    draw.text((xy[0] - (box[2] - box[0]) / 2, xy[1] - (box[3] - box[1]) / 2),
              value, font=f, fill=fill, stroke_width=stroke, stroke_fill=stroke_fill)


def overlay_glow_line(image, points, core=(255, 244, 190, 255), glow=(255, 108, 22, 210), width=13):
    glow_layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow_layer)
    glow_draw.line(points, fill=glow, width=width * 5, joint="curve")
    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(width * 3))
    image.alpha_composite(glow_layer)
    draw = ImageDraw.Draw(image)
    draw.line(points, fill=core, width=width, joint="curve")
    end = points[-1]
    draw.ellipse((end[0] - width * 2, end[1] - width * 2,
                  end[0] + width * 2, end[1] + width * 2), fill=(255, 255, 235, 255))


def combat_capture(source_name, overdrive=False, contact=False):
    image = Image.open(SRC / source_name).convert("RGBA")
    if overdrive:
        shots = [((1378, 825), (1694, 352)), ((1490, 830), (1814, 388))]
    else:
        shots = [((1435, 825), (1700, 360))]
    for shot in shots:
        overlay_glow_line(image, shot)
    if contact:
        overlay_glow_line(image, ((1810, 325), (1550, 680)),
                          core=(235, 190, 255, 255), glow=(202, 44, 230, 220), width=11)

    draw = ImageDraw.Draw(image, "RGBA")
    banner = (865, 102, 2000, 248)
    draw.rounded_rectangle(banner, radius=18, fill=(5, 10, 14, 225), outline=(255, 119, 28, 255), width=4)
    if overdrive:
        text_center(draw, (1432, 148), "OVERDRIVE // DOUBLE ROUNDS", 58, (255, 150, 42, 255), 2)
        text_center(draw, (1432, 207), "REWARDED FIREPOWER ACTIVE", 26, (245, 235, 208, 255), 1)
    elif contact:
        text_center(draw, (1432, 175), "LIVE FIRE // RETURN FIRE", 58, (255, 150, 42, 255), 2)
    else:
        text_center(draw, (1432, 175), "LIVE FIRE // TARGET ACQUIRED", 52, (255, 150, 42, 255), 2)
    return image


def make_ipad(image, size, label=None):
    # The game runs landscape, while App Store iPad slots are 4:3. Preserve
    # the complete gameplay frame and build a restrained, blurred extension
    # above/below it instead of cropping the title, tanks, or action.
    source = image.convert("RGBA")
    background = ImageOps.fit(source.convert("RGB"), size, method=Image.Resampling.LANCZOS,
                              centering=(0.5, 0.5)).convert("RGBA")
    background = background.filter(ImageFilter.GaussianBlur(22))
    shade = Image.new("RGBA", size, (2, 7, 12, 128))
    background.alpha_composite(shade)
    fitted = background
    main_width = size[0]
    main_height = round(source.height * main_width / source.width)
    main = source.resize((main_width, main_height), Image.Resampling.LANCZOS)
    y = (size[1] - main_height) // 2
    fitted.alpha_composite(main, (0, y))
    draw = ImageDraw.Draw(fitted, "RGBA")
    if label:
        draw.rounded_rectangle((72, 72, 820, 176), radius=16,
                               fill=(4, 10, 14, 210), outline=(255, 119, 28, 240), width=3)
        text_center(draw, (446, 124), label, 42, (255, 150, 42, 255), 1)
    return fitted


def write(image, name):
    image.convert("RGB").save(OUT / name, "PNG", optimize=False, compress_level=1)


def main():
    launch = Image.open(OUT / "tread-shred-launch.png").convert("RGBA")
    mission = Image.open(SRC / "iphone-pro-combat-clean.png").convert("RGBA")
    live_fire = combat_capture("iphone-pro-action.png")
    double_rounds = combat_capture("iphone-pro-action.png", overdrive=True)
    return_fire = combat_capture("iphone-pro-later.png", contact=True)

    # iPhone 6.9-inch set: no Unity splash, no placeholder screen, and every
    # gameplay frame has a visible combat beat.
    write(launch, "tread-shred-01-start.png")
    write(mission, "tread-shred-02-mission-select.png")
    write(live_fire, "tread-shred-03-live-fire.png")
    write(double_rounds, "tread-shred-04-overdrive.png")
    write(return_fire, "tread-shred-05-return-fire.png")

    # iPad Pro landscape sets use the same verified gameplay moments, composed
    # for the two App Store landscape display sizes.
    frames = [launch, mission, live_fire, double_rounds, return_fire]
    labels = ["COMMAND BASE", "MISSION SELECT", "LIVE FIRE", "OVERDRIVE", "RETURN FIRE"]
    for display, size in (("ipad129", (2732, 2048)), ("ipad13", (2752, 2064))):
        for index, (frame, label) in enumerate(zip(frames, labels), 1):
            write(make_ipad(frame, size, label), f"tread-shred-{display}-{index:02d}.png")


if __name__ == "__main__":
    main()
