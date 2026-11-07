"""Builds the D-019 promotion's shell rain from CC0 recordings of real brass casings hitting concrete.

Cuts every impact out of the source recordings, keeps the bright ringing hits (brass) over dull thuds, writes the best
as one-shots, then mixes a 3-second rain: a dense burst that thins out, each casing with its own gain, slight pitch and
pan. Output: Assets/Resources/Audio/ShellRain.ogg (approved by the owner 2026-11-06).

Sources, all CC0 (public domain: commercial use, no attribution required), from Freesound's public previews:
  "Brass bullet shell casing drop onto concrete, multiple takes" by GrayJoy, https://freesound.org/people/GrayJoy/sounds/210102/
  ".30 Shell Bouncing" by Danwardvs, https://freesound.org/people/Danwardvs/sounds/205126/
  "Bouncing Shell Casings (Various Sizes)" by GryffDavid, https://freesound.org/people/GryffDavid/sounds/318964/

Rebuild: convert each preview to 44.1 kHz mono WAV with ffmpeg (each download in its own folder), then
  python3 -E scripts/make-shell-rain.py <out_dir> <source.wav>...
  ffmpeg -i <out_dir>/shell-rain.wav -c:a libvorbis -q:a 7 Assets/Resources/Audio/ShellRain.ogg
The mix is seeded, so the same sources give the same file.
"""
import sys
import wave
from pathlib import Path

import numpy as np

RATE = 44100
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

    # The rain: 34 casings over ~2.2 s, densest at the start, then a few late stragglers and the ring-out.
    length = int(3.2 * RATE)
    mix = np.zeros((length, 2), dtype=np.float32)
    onsets = np.sort(np.concatenate([rng.gamma(1.6, 0.28, 28), rng.uniform(1.2, 2.3, 6)]))
    onsets = onsets[onsets < 2.4]
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


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2:])
