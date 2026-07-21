"""Procedurally generates all game audio (SFX + looping music) as WAV files
into Assets/Resources/Audio. Run with: uv run python generate.py

Everything is synthesized — no external samples. The music loop writes its
note tails wrapped around the buffer end so the loop is seamless.
"""
import os
import wave

import numpy as np

SR = 44100
OUT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Resources", "Audio"))


def write_wav(name, data, peak=0.85):
    data = np.asarray(data, dtype=np.float64)
    m = np.max(np.abs(data))
    if m > 0:
        data = data / m * peak
    pcm = (np.clip(data, -1, 1) * 32767).astype("<i2")
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(f"{name}.wav  {len(data) / SR:.2f}s")


def sweep(f_start, f_end, dur, curve=1.0):
    """Sine with an exponential-ish frequency sweep."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    p = (t / dur) ** curve
    f = f_start + (f_end - f_start) * p
    phase = 2 * np.pi * np.cumsum(f) / SR
    return t, np.sin(phase)


def pluck(freq, dur, decay=6.0, brightness=0.35):
    """Soft plucked tone: fundamental plus a few decaying harmonics."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    s = (np.sin(2 * np.pi * freq * t)
         + brightness * np.sin(2 * np.pi * 2 * freq * t)
         + brightness * 0.35 * np.sin(2 * np.pi * 3 * freq * t))
    envelope = np.minimum(t / 0.008, 1.0) * np.exp(-t * decay)
    return s * envelope


# ---------- SFX ----------

def gen_pop():
    t, s = sweep(760, 190, 0.18, curve=0.5)
    s *= np.exp(-t * 20)
    click = np.random.default_rng(1).uniform(-1, 1, int(0.004 * SR))
    click *= np.exp(-np.arange(len(click)) / (0.0012 * SR)) * 0.5
    s[:len(click)] += click
    write_wav("pop", s)


def gen_swap():
    n = int(0.14 * SR)
    rng = np.random.default_rng(2)
    noise = np.diff(rng.standard_normal(n + 1))  # high-passed noise
    envelope = np.sin(np.pi * np.arange(n) / n) ** 2
    write_wav("swap", noise * envelope, peak=0.45)


def gen_invalid():
    n = int(0.22 * SR)
    t = np.arange(n) / SR
    f = 150 * np.exp(-t * 2.5)
    phase = 2 * np.pi * np.cumsum(f) / SR
    s = np.tanh(2.5 * np.sin(phase)) * np.exp(-t * 11)
    s += 0.5 * np.sin(2 * np.pi * 82 * t) * np.exp(-t * 9)
    write_wav("invalid", s, peak=0.6)


def gen_button():
    n = int(0.07 * SR)
    t = np.arange(n) / SR
    s = np.sin(2 * np.pi * 1250 * t) * np.exp(-t * 70)
    s += np.sin(2 * np.pi * 2400 * t) * np.exp(-t * 110) * 0.4
    write_wav("button", s, peak=0.5)


def gen_hop():
    t, s = sweep(260, 700, 0.30, curve=0.7)
    s *= np.exp(-t * 8) * np.minimum(t / 0.01, 1.0)
    write_wav("hop", s, peak=0.6)


def gen_win():
    total = int(1.7 * SR)
    buf = np.zeros(total)
    notes = [523.25, 659.25, 783.99, 1046.5]  # C5 E5 G5 C6
    for i, f in enumerate(notes):
        start = int(i * 0.14 * SR)
        tone_sig = pluck(f, 0.5, decay=5)
        buf[start:start + len(tone_sig)] += tone_sig
    # closing chord
    for f in [523.25, 659.25, 783.99, 1046.5]:
        start = int(0.62 * SR)
        tone_sig = pluck(f, 1.0, decay=3) * 0.8
        buf[start:start + len(tone_sig)] += tone_sig
    write_wav("win", buf)


def gen_lose():
    total = int(1.3 * SR)
    buf = np.zeros(total)
    for i, f in enumerate([392.0, 311.13, 261.63]):  # G4 Eb4 C4
        start = int(i * 0.3 * SR)
        n = int(0.7 * SR)
        t = np.arange(n) / SR
        tone_sig = (np.sin(2 * np.pi * f * t) + 0.3 * np.sin(2 * np.pi * f * 1.004 * t))
        tone_sig *= np.minimum(t / 0.02, 1.0) * np.exp(-t * 4)
        buf[start:start + n] += tone_sig
    write_wav("lose", buf, peak=0.55)


def gen_crunch():
    """Cereal crunch: a dense burst of micro-crackles (breaking flakes),
    high-passed for crispness, with a soft low 'bite' thump at the start."""
    rng = np.random.default_rng(11)
    dur = 0.17
    n = int(dur * SR)
    s = np.zeros(n)

    # fewer but longer grains = coarser crackle (cornflakes, not rice crisps)
    grain_times = rng.uniform(0, 1, 55) ** 1.7 * dur  # densest at the bite
    for t0 in grain_times:
        g_len = int(rng.uniform(0.002, 0.008) * SR)
        i0 = int(t0 * SR)
        g_len = min(g_len, n - i0)
        if g_len <= 0:
            continue
        grain = rng.uniform(-1, 1, g_len) * np.exp(-np.arange(g_len) / (0.0016 * SR))
        s[i0:i0 + g_len] += grain * rng.uniform(0.3, 1.0)

    s = np.diff(s, prepend=0)  # high-pass: crispy
    t = np.arange(n) / SR
    s *= np.minimum(t / 0.002, 1.0) * np.exp(-t * 13)  # fast decay = dry
    s += 0.45 * np.sin(2 * np.pi * 150 * t) * np.exp(-t * 45)  # bite thump
    fade = int(0.02 * SR)
    s[-fade:] *= np.linspace(1, 0, fade)
    write_wav("crunch", s, peak=0.7)


# ---------- Music loop ----------

NOTE = {
    "C3": 130.81, "F3": 174.61, "G3": 196.0, "A3": 220.0,
    "C4": 261.63, "E4": 329.63, "F4": 349.23, "G4": 392.0, "A4": 440.0, "B4": 493.88,
    "C5": 523.25, "D5": 587.33, "E5": 659.25, "F5": 698.46, "G5": 783.99,
    "A5": 880.0, "B5": 987.77, "C6": 1046.5, "D6": 1174.66,
}


def gen_music():
    bpm = 112.0
    spb = 60.0 / bpm
    beats = 32  # 8 bars of 4/4
    n_total = int(round(beats * spb * SR))
    buf = np.zeros(n_total)

    def add(start_beat, sig, amp):
        i0 = int(start_beat * spb * SR)
        idx = (i0 + np.arange(len(sig))) % n_total  # wrap tails: seamless loop
        np.add.at(buf, idx, sig * amp)

    minor = [1.0, 1.1892, 1.4983]
    major = [1.0, 1.2599, 1.4983]
    bars = [("C4", major), ("A3", minor), ("F3", major), ("G3", major)] * 2

    # pads: soft sustained triads per bar
    for bar, (root, quality) in enumerate(bars):
        n = int(4 * spb * SR)
        t = np.arange(n) / SR
        envelope = np.minimum(t / 0.4, 1.0) * np.minimum((n - np.arange(n)) / (0.4 * SR), 1.0)
        chord = sum(np.sin(2 * np.pi * NOTE[root] * r * t) for r in quality)
        add(bar * 4, chord * envelope, 0.045)

    # bass: root plucks on beats 1 and 3
    for bar, (root, _) in enumerate(bars):
        for beat in (0, 2):
            add(bar * 4 + beat, pluck(NOTE[root] / 2, 0.45, decay=5), 0.22)

    # melody: quarter notes, one line per bar
    melody = [
        ["E5", "G5", "C6", "G5"],
        ["A5", "E5", "C5", "E5"],
        ["F5", "A5", "C6", "A5"],
        ["G5", "B5", "D6", "B5"],
        ["C6", "G5", "E5", "G5"],
        ["A5", "C6", "A5", "E5"],
        ["F5", "C6", "A5", "F5"],
        ["G5", "D5", "G5", "B5"],
    ]
    for bar, line in enumerate(melody):
        for beat, note in enumerate(line):
            add(bar * 4 + beat, pluck(NOTE[note], 0.55, decay=4.5, brightness=0.25), 0.14)

    # hats: tiny noise ticks on the offbeats
    rng = np.random.default_rng(7)
    tick_n = int(0.03 * SR)
    for beat in range(beats):
        tick = np.diff(rng.standard_normal(tick_n + 1))
        tick *= np.exp(-np.arange(tick_n) / (0.006 * SR))
        add(beat + 0.5, tick, 0.05)

    write_wav("music_loop", buf, peak=0.6)


if __name__ == "__main__":
    gen_pop()
    gen_crunch()
    gen_swap()
    gen_invalid()
    gen_button()
    gen_hop()
    gen_win()
    gen_lose()
    gen_music()
    print("done ->", OUT)
