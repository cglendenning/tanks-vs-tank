"""Builds the D-019 promotion's shell rain from CC0 recordings of real brass casings hitting concrete.

Cuts every impact out of the source recordings, keeps the bright ringing hits (brass) over dull thuds, writes the best
as one-shots, then mixes a rain as long as the promotion's march: a dense first burst, a steady fall, and a thinning
tail as the march's last chord rings out, each casing with its own gain, slight pitch and pan.
Output: Assets/Resources/Audio/ShellRain.ogg (sound approved by the owner 2026-11-06, length 2026-11-07).

Sources, all CC0 (public domain: commercial use, no attribution required), from Freesound's public previews:
  "Brass bullet shell casing drop onto concrete, multiple takes" by GrayJoy, https://freesound.org/people/GrayJoy/sounds/210102/
  ".30 Shell Bouncing" by Danwardvs, https://freesound.org/people/Danwardvs/sounds/205126/
  "Bouncing Shell Casings (Various Sizes)" by GryffDavid, https://freesound.org/people/GryffDavid/sounds/318964/

Rebuild: convert each preview to 44.1 kHz mono WAV with ffmpeg (each download in its own folder), then
  python3 -E scripts/make-shell-rain.py <out_dir> <source.wav>...
  ffmpeg -i <out_dir>/shell-rain.wav -c:a libvorbis -q:a 7 Assets/Resources/Audio/ShellRain.ogg
The mix is seeded, so the same sources give the same file.

The promotion's march (owner request 2026-11-07): the closing phrase and final chord of John Philip Sousa's
"The Washington Post" (1889; composition public domain, Sousa died 1932), performed by the U.S. Army Band, a work of
the U.S. federal government and so public domain ({{PD-USGov-Army}} on Wikimedia Commons). Source:
  https://commons.wikimedia.org/wiki/File:Washington_Post_March_-_U.S._Army_Band.ogg
  (from https://www.usarmyband.com/Audio/ceremonial_music_guide.html)
Rebuild: python3 -E scripts/make-shell-rain.py --march <downloaded.ogg> Assets/Resources/Audio/PromotionMarch.ogg
"""
import subprocess
import sys
import wave
from pathlib import Path

import numpy as np

RATE = 44100
# The rain runs as long as the march: MARCH_SECONDS plus the last chord's ring-out.
MARCH_START, MARCH_SECONDS = 143.5, 10.0
SECONDS = MARCH_SECONDS + 0.3
rng = np.random.default_rng(20261106)


def load(path):
    with wave.open(str(path)) as w:
        assert w.getframerate() == RATE and w.getnchannels() == 1 and w.getsampwidth() == 2, path
        return np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float32) / 32768


def save(path, audio):
    audio = np.clip(audio, -1, 1)
    stereo = audio.ndim == 2
    with wave.open(str(path), "wb") as w:
        w.setnchannels(2 if stereo else 1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((audio * 32767).astype(np.int16).tobytes())


def envelope(x, window=0.005):
    n = int(RATE * window)
    return np.sqrt(np.convolve(x * x, np.ones(n) / n, mode="same"))


def impacts(x):
    """Each impact from its onset until the sound falls quiet for 0.12 s (at most 1.2 s)."""
    env = envelope(x)
    loud = env > max(env.max() * 0.08, 0.004)
    quiet = env < env.max() * 0.02
    out, i = [], 0
    while i < len(x):
        if not loud[i]:
            i += 1
            continue
        start = max(i - int(0.004 * RATE), 0)
        end, gap = i, 0
        while end < len(x) and end - start < 1.2 * RATE:
            gap = gap + 1 if quiet[end] else 0
            if gap > 0.12 * RATE:
                break
            end += 1
        seg = x[start:end].copy()
        if len(seg) > 0.04 * RATE:
            fade = int(0.03 * RATE)
            seg[:int(0.003 * RATE)] *= np.linspace(0, 1, int(0.003 * RATE))
            seg[-fade:] *= np.linspace(1, 0, fade)
            out.append(seg)
        i = end + 1
    return out


def brightness(seg):
    """Share of energy above 2.5 kHz: brass rings bright; a thud or handling noise does not."""
    spectrum = np.abs(np.fft.rfft(seg * np.hanning(len(seg)))) ** 2
    freqs = np.fft.rfftfreq(len(seg), 1 / RATE)
    return spectrum[freqs > 2500].sum() / max(spectrum.sum(), 1e-12)


def repitch(seg, ratio):
    idx = np.arange(0, len(seg) - 1, ratio)
    return np.interp(idx, np.arange(len(seg)), seg)


def main(out_dir, sources):
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)
    hits = [h for src in sources for h in impacts(load(src))]
    scored = sorted(((brightness(h), np.abs(h).max(), h) for h in hits), key=lambda t: -t[0])
    bright = [h for b, peak, h in scored if b > 0.35 and peak > 0.02 and peak < 0.99]
    print(f"{len(hits)} impacts cut; {len(bright)} bright, unclipped brass hits kept")
    for n, h in enumerate(bright[:16]):
        save(out / f"casing-{n + 1:02d}.wav", h / np.abs(h).max() * 0.89)

    # The rain: a dense first burst, then about nine casings a second, thinning over the last chord, then the ring-out.
    length = int(SECONDS * RATE)
    mix = np.zeros((length, 2), dtype=np.float32)
    steady = np.cumsum(rng.exponential(1 / 9.0, 160)) + 0.8
    onsets = np.sort(np.concatenate([rng.gamma(1.6, 0.28, 28), steady[steady < SECONDS - 1.6],
                                     rng.uniform(SECONDS - 1.6, SECONDS - 0.9, 4)]))
    onsets = onsets[onsets < SECONDS - 0.8]
    for t in onsets:
        h = bright[rng.integers(len(bright))]
        h = repitch(h / np.abs(h).max(), rng.uniform(0.93, 1.08)) * 10 ** (rng.uniform(-14, -2) / 20)
        pan = rng.uniform(-0.8, 0.8)
        start = int(t * RATE)
        end = min(start + len(h), length)
        mix[start:end, 0] += h[: end - start] * np.sqrt((1 - pan) / 2)
        mix[start:end, 1] += h[: end - start] * np.sqrt((1 + pan) / 2)
    mix /= np.abs(mix).max() / 0.89
    save(out / "shell-rain.wav", mix)
    print(f"shell-rain.wav: {len(onsets)} casings, {length / RATE:.1f} s")


def march(source, out_ogg):
    """Cuts the promotion's march: the closing phrase, a short fade-in, the final chord's own decay, loudness-matched."""
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-ss", str(MARCH_START), "-t", str(MARCH_SECONDS), "-i", str(source),
                    "-af", "afade=t=in:st=0:d=0.3,afade=t=out:st=" + str(MARCH_SECONDS - 1.0) + ":d=1.0,"
                    "loudnorm=I=-14:TP=-1.5:LRA=11", "-ar", str(RATE), "-ac", "2", "-c:a", "libvorbis", "-q:a", "6",
                    str(out_ogg)], check=True)
    print(f"{out_ogg}: {MARCH_SECONDS:.1f} s of the march from {MARCH_START:.1f} s")


if __name__ == "__main__":
    if sys.argv[1] == "--march":
        march(sys.argv[2], sys.argv[3])
    else:
        main(sys.argv[1], sys.argv[2:])
