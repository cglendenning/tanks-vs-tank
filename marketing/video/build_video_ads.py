"""Build the paid-acquisition video ads and store videos (spec D-031).

Every frame comes from real game captures (D-031-AC-06); the only additions
are cuts, punch-in crops, text overlays and the end card.

    python3 marketing/video/build_video_ads.py --device <owner screen recording.mp4> \
        --tiers <simulator mission-select recording.mp4>

Both captures are stored rotated (landscape game, portrait container), so
every clip is transposed first. Coordinates below are in landscape pixels of
the source after that rotation. Output goes to marketing/video/out/.
"""
import argparse
import shutil
import subprocess
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
HERE = ROOT / "marketing" / "video"
OUT = HERE / "out"
FONT = ROOT / "Assets" / "Art" / "BlackOpsOne-Regular.ttf"
MUSIC = ROOT / "Assets" / "Music" / "InGame.mp3"
END_SFX = ROOT / "Assets" / "Music" / "Explosion.mp3"
APPLE_BADGE = HERE / "badges" / "app-store-black.svg"
GOOGLE_BADGE = HERE / "badges" / "google-play.png"

FPS = 30
WHITE = "white"
GOLD = "0xFFB847"
ACCENT = (255, 92, 20)
BACKGROUND = (4, 9, 12)

# Output formats: name -> (width, height, foreground aspect inside the frame).
LANDSCAPE = ("16x9", 1920, 1080, 16 / 9)
PORTRAIT = ("9x16", 1080, 1920, 1.4)
PREVIEW = ("app-preview", 1920, 886, 1920 / 886)


def clip(source, start, duration, center, zoom, lines=(), band=False, portrait_zoom=None):
    """A cut from a source: (source key, start s, length s, crop centre, zoom, text lines).

    Each text line is (text, appears at s, colour). band darkens the strip behind
    landscape text where the footage has its own text underneath.
    portrait_zoom overrides zoom for the narrower 9:16 footage band.
    """
    return dict(source=source, start=start, duration=duration, center=center, zoom=zoom, lines=lines,
                band=band, portrait_zoom=portrait_zoom or zoom)


HOOKS = {
    # A: a real kill at 1:47 of the owner's recording; player tank and fireball in frame.
    "A": clip("device", 105.9, 2.6, (1010, 600), 1.7,
              [("ONE TANK.", 0.0, WHITE), ("ZERO MERCY.", 1.0, GOLD)]),
    # B: the tank is destroyed, revives and the fight explodes back to life at 1:24.
    "B": clip("device", 83.9, 2.6, (760, 540), 1.6,
              [("TANK DOWN?", 0.0, WHITE), ("COME BACK HARDER.", 1.0, GOLD)]),
    # C: the first kill of the session at 0:37.
    "C": clip("device", 34.8, 3.0, (760, 330), 1.8,
              [("CLEAR THE ARENA.", 0.0, WHITE), ("NO BACKUP.", 1.4, GOLD)]),
}

# The power-fantasy arc (D-031-AC-03): struggle, upgrade, dominate, rank up.
STRUGGLE_30 = clip("device", 79.4, 3.2, (1218, 563), 1.15, [("OUTGUNNED?", 0.3, WHITE)])
UPGRADE_30 = clip("device", 10.2, 4.0, (1258, 563), 1.0,
                  [("UPGRADE.", 0.0, WHITE), ("ARMOR. FIREPOWER. SPEED.", 0.9, GOLD)], band=True)
ENGAGE = clip("device", 47.6, 2.0, (1218, 563), 1.0)
FIGHT_30 = clip("device", 92.5, 5.0, (1000, 620), 1.35, [("HUNT THEM DOWN.", 0.3, WHITE)])
KILL_30 = clip("device", 104.9, 3.6, (1010, 600), 1.5, [("DOMINATE.", 2.4, GOLD)])
TIERS_30 = clip("tiers", 9.0, 3.6, (2090, 1100), 1.9,
                [("RANK UP.", 0.0, WHITE), ("VETERAN. THEN ELITE.", 1.0, GOLD)], band=True, portrait_zoom=1.5)

STRUGGLE_15 = clip("device", 80.4, 1.8, (1218, 563), 1.15, [("OUTGUNNED?", 0.0, WHITE)])
UPGRADE_15 = clip("device", 11.0, 2.2, (1258, 563), 1.0, [("UPGRADE.", 0.0, GOLD)], band=True)
KILL_15 = clip("device", 105.2, 3.2, (1010, 600), 1.5, [("DOMINATE.", 2.0, GOLD)])
TIERS_15 = clip("tiers", 9.4, 2.0, (2090, 1100), 1.9, [("RANK UP.", 0.0, GOLD)], band=True, portrait_zoom=1.5)


def timeline(hook, seconds):
    """Cuts for one variant and length, end card excluded; returns (cuts, end card length)."""
    if seconds == 30:
        cuts = [hook, STRUGGLE_30, UPGRADE_30, ENGAGE, FIGHT_30, KILL_30, TIERS_30]
    else:
        short_hook = dict(hook, duration=min(hook["duration"], 2.6))
        cuts = [short_hook, STRUGGLE_15, UPGRADE_15, KILL_15, TIERS_15]
    used = sum(c["duration"] for c in cuts)
    return cuts, round(seconds - used, 3)


def run(args):
    subprocess.run(args, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)


def probe_size(path):
    out = subprocess.run(["ffprobe", "-v", "error", "-select_streams", "v:0", "-show_entries",
                          "stream=width,height", "-of", "csv=p=0", str(path)],
                         check=True, capture_output=True, text=True).stdout.strip()
    width, height = (int(v) for v in out.split(","))
    return height, width  # landscape size after the transpose


def has_audio(path):
    out = subprocess.run(["ffprobe", "-v", "error", "-select_streams", "a", "-show_entries",
                          "stream=index", "-of", "csv=p=0", str(path)],
                         check=True, capture_output=True, text=True).stdout.strip()
    return bool(out)


def crop_rect(source_size, center, zoom, aspect):
    src_w, src_h = source_size
    h = src_h / zoom
    w = h * aspect
    if w > src_w:
        w, h = src_w, src_w / aspect
    x = min(max(center[0] - w / 2, 0), src_w - w)
    y = min(max(center[1] - h / 2, 0), src_h - h)
    even = lambda v: int(v) // 2 * 2
    return even(w), even(h), even(x), even(y)


def fitted_size(text, size, max_width):
    font = ImageFont.truetype(str(FONT), size)
    width = font.getbbox(text)[2]
    return size if width <= max_width else int(size * max_width / width)


def text_filters(lines, fmt, duration, band):
    name, width, height, _ = fmt
    portrait = name == PORTRAIT[0]
    size = 104 if portrait else 96
    step = size + 26
    first_y = 250 if portrait else 48
    filters = []
    if band and lines and not portrait:
        filters.append(f"drawbox=x=0:y=0:w=iw:h={first_y + step * len(lines) + 10}:color=black@0.82:t=fill")
    for index, (text, at, colour) in enumerate(lines):
        line_size = fitted_size(text, size, width - 100)
        alpha = f"if(lt(t-{at},0.15),(t-{at})/0.15,1)"
        filters.append(
            f"drawtext=fontfile='{FONT}':text='{text}':fontsize={line_size}:fontcolor={colour}"
            f":borderw=7:bordercolor=black:shadowx=0:shadowy=6:shadowcolor=black@0.6"
            f":x=(w-text_w)/2:y={first_y + index * step}"
            f":alpha='{alpha}':enable='between(t,{at},{duration})'")
    return filters


def render_cut(cut, sources, fmt, path):
    name, width, height, aspect = fmt
    src = sources[cut["source"]]
    zoom = cut["portrait_zoom"] if name == PORTRAIT[0] else cut["zoom"]
    w, h, x, y = crop_rect(src["size"], cut["center"], zoom, aspect)
    base = f"transpose=2,crop={w}:{h}:{x}:{y}"
    if name == PORTRAIT[0]:
        fg_h = int(width / aspect) // 2 * 2
        graph = (f"[0:v]{base},split[a][b];"
                 f"[a]scale={width}:{height}:force_original_aspect_ratio=increase,crop={width}:{height},"
                 f"boxblur=24:2,eq=brightness=-0.25[bg];"
                 f"[b]scale={width}:{fg_h}[fg];"
                 f"[bg][fg]overlay=0:(H-h)/2+90,setsar=1,fps={FPS}")
    else:
        graph = f"[0:v]{base},scale={width}:{height},setsar=1,fps={FPS}"
    graph = ",".join([graph] + text_filters(cut["lines"], fmt, cut["duration"], cut["band"])) + "[v]"
    inputs = ["-ss", str(cut["start"]), "-t", str(cut["duration"]), "-i", str(src["path"])]
    if src["audio"]:
        audio = ["-map", "0:a", "-af", f"volume=0.45,afade=t=in:d=0.05,afade=t=out:st={cut['duration'] - 0.08}:d=0.08"]
    else:
        inputs += ["-f", "lavfi", "-t", str(cut["duration"]), "-i", "anullsrc=r=48000:cl=stereo"]
        audio = ["-map", "1:a"]
    run(["ffmpeg", "-v", "error", "-y", *inputs, "-filter_complex", graph, "-map", "[v]", *audio,
         "-t", str(cut["duration"]), "-c:v", "libx264", "-preset", "veryfast", "-crf", "14",
         "-pix_fmt", "yuv420p", "-c:a", "aac", "-ar", "48000", "-ac", "2", "-b:a", "192k", str(path)])


def end_card(fmt, still, badges, cta, path):
    """Game name, NO ADS. JUST COMBAT., PLAY FREE and the store badges (D-031-AC-05)."""
    name, width, height, _ = fmt
    card = Image.open(still).convert("RGB")
    scale = max(width / card.width, height / card.height)
    card = card.resize((int(card.width * scale) + 1, int(card.height * scale) + 1))
    left, top = (card.width - width) // 2, (card.height - height) // 2
    card = card.crop((left, top, left + width, top + height)).filter(ImageFilter.GaussianBlur(18))
    card = Image.blend(card, Image.new("RGB", card.size, BACKGROUND), 0.72)
    draw = ImageDraw.Draw(card)
    portrait = height > width
    unit = width / 1080 if portrait else min(width / 1920, height / 1080)
    max_width = int(width * 0.88)

    def font_for(text, size):
        return ImageFont.truetype(str(FONT), fitted_size(text, int(size * unit), max_width))

    title = ("TREAD SHRED", font_for("TREAD SHRED", 190), (255, 184, 71))
    tagline = ("NO ADS. JUST COMBAT.", font_for("NO ADS. JUST COMBAT.", 84), (255, 255, 255))
    button_font = font_for("PLAY FREE", 96)
    pad_x, pad_y, gap = int(70 * unit), int(34 * unit), int(56 * unit)
    badge_h = int(150 * unit)
    images = [b.resize((int(b.width * badge_h / b.height), badge_h), Image.LANCZOS) for b in badges]

    def text_height(font, text):
        return font.getbbox(text)[3]

    blocks = [text_height(title[1], title[0]), text_height(tagline[1], tagline[0])]
    if cta:
        blocks.append(text_height(button_font, "PLAY FREE") + 2 * pad_y)
    if images:
        blocks.append(badge_h)
    y = (height - sum(blocks) - gap * (len(blocks) - 1)) // 2

    for text, font, fill in (title, tagline):
        box = draw.textbbox((0, 0), text, font=font)
        draw.text(((width - box[2]) // 2, y), text, font=font, fill=fill,
                  stroke_width=max(2, int(4 * unit)), stroke_fill=(0, 0, 0))
        y += box[3] + gap
    if cta:
        box = draw.textbbox((0, 0), "PLAY FREE", font=button_font)
        bw, bh = box[2] + 2 * pad_x, box[3] + 2 * pad_y
        bx = (width - bw) // 2
        draw.rectangle((bx, y, bx + bw, y + bh), fill=ACCENT)
        draw.text((bx + pad_x, y + pad_y), "PLAY FREE", font=button_font, fill=(255, 255, 255))
        y += bh + gap
    if images:
        spacing = int(40 * unit)
        x = (width - sum(i.width for i in images) - spacing * (len(images) - 1)) // 2
        for image in images:
            card.paste(image, (x, y), image)
            x += image.width + spacing
    card.save(path)


def load_badges(tmp):
    apple = tmp / "apple.png"
    run(["rsvg-convert", "-h", "480", "-o", str(apple), str(APPLE_BADGE)])
    google = Image.open(GOOGLE_BADGE).convert("RGBA")
    google = google.crop(google.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox())
    return Image.open(apple).convert("RGBA"), google


def render_end(card_png, seconds, path):
    run(["ffmpeg", "-v", "error", "-y", "-loop", "1", "-framerate", str(FPS), "-t", str(seconds), "-i", str(card_png),
         "-i", str(END_SFX), "-filter_complex",
         f"[0:v]fade=t=in:d=0.25,format=yuv420p[v];[1:a]volume=0.8,apad,atrim=0:{seconds}[a]",
         "-map", "[v]", "-map", "[a]", "-t", str(seconds), "-c:v", "libx264", "-preset", "veryfast",
         "-crf", "14", "-pix_fmt", "yuv420p", "-c:a", "aac", "-ar", "48000", "-ac", "2", str(path)])


def assemble(parts, seconds, path, audio_bitrate="192k", sample_rate="48000"):
    listing = path.with_suffix(".txt")
    listing.write_text("".join(f"file '{p}'\n" for p in parts))
    fade_at = seconds - 1.5
    run(["ffmpeg", "-v", "error", "-y", "-f", "concat", "-safe", "0", "-i", str(listing),
         "-stream_loop", "-1", "-i", str(MUSIC), "-filter_complex",
         f"[1:a]volume=0.55,atrim=0:{seconds},afade=t=out:st={fade_at}:d=1.5[m];"
         f"[0:a][m]amix=inputs=2:normalize=0,loudnorm=I=-14:TP=-1.5:LRA=11[a]",
         # A hair under the nominal length: App Store previews must not exceed 30 s.
         "-map", "0:v", "-map", "[a]", "-t", str(seconds - 0.1), "-c:v", "libx264", "-profile:v", "high",
         "-preset", "slow", "-crf", "20", "-r", str(FPS), "-pix_fmt", "yuv420p",
         "-c:a", "aac", "-b:a", audio_bitrate, "-ar", sample_rate, "-ac", "2",
         "-movflags", "+faststart", str(path)])
    listing.unlink()


def build(device, tiers, only=None):
    OUT.mkdir(parents=True, exist_ok=True)
    sources = {
        "device": dict(path=device, size=probe_size(device), audio=has_audio(device)),
        "tiers": dict(path=tiers, size=probe_size(tiers), audio=has_audio(tiers)),
    }
    with tempfile.TemporaryDirectory() as tmp_name:
        tmp = Path(tmp_name)
        apple, google = load_badges(tmp)
        still = tmp / "title.png"
        run(["ffmpeg", "-v", "error", "-y", "-ss", "6.5", "-i", str(device), "-vf", "transpose=2",
             "-frames:v", "1", str(still)])

        jobs = []  # (output name, hook, seconds, format, badges, call to action, audio bitrate)
        for variant, hook in HOOKS.items():
            for seconds in (15, 30):
                for fmt in (LANDSCAPE, PORTRAIT):
                    jobs.append((f"tread-shred-{variant}-{seconds}s-{fmt[0]}", hook, seconds, fmt,
                                 [apple, google], True, "192k"))
        # Google Play promo: Google badge only. App Store preview: no badges or price (App Store rules).
        jobs.append(("tread-shred-play-promo-30s-16x9", HOOKS["A"], 30, LANDSCAPE, [google], True, "192k"))
        jobs.append(("tread-shred-app-store-preview-30s", HOOKS["A"], 30, PREVIEW, [], False, "256k"))

        rendered = {}
        for name, hook, seconds, fmt, badges, cta, bitrate in jobs:
            if only and only not in name:
                continue
            cuts, end_seconds = timeline(hook, seconds)
            parts = []
            for cut in cuts:
                key = (repr(sorted((k, repr(v)) for k, v in cut.items())), fmt[0])
                if key not in rendered:
                    rendered[key] = tmp / f"cut{len(rendered)}.mp4"
                    render_cut(cut, sources, fmt, rendered[key])
                parts.append(rendered[key])
            card_png = tmp / f"{name}-card.png"
            end_card(fmt, still, badges, cta, card_png)
            end_mp4 = tmp / f"{name}-end.mp4"
            render_end(card_png, end_seconds, end_mp4)
            shutil.copy(card_png, OUT / f"{name}-end-card.png")
            assemble(parts + [end_mp4], seconds, OUT / f"{name}.mp4", audio_bitrate=bitrate)
            print(f"built {name}.mp4")


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--device", type=Path, required=True, help="owner's on-device screen recording")
    parser.add_argument("--tiers", type=Path, required=True, help="simulator mission-select capture")
    parser.add_argument("--only", help="build only outputs whose name contains this text")
    args = parser.parse_args()
    build(args.device.resolve(), args.tiers.resolve(), args.only)


if __name__ == "__main__":
    main()
