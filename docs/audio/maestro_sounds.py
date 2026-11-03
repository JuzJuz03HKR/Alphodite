#!/usr/bin/env python3
# MAESTRO SOUNDS (3 Nov, the user : a maestro's FINALE sounds like what it is : lightning, glass, light)
# Built only from the recordings already in the game (sources and licences in MANIFEST.md), layered and shaped :
#   finale_storm : a sharp crack of lightning, a long low thunder roll after it, a hit under both
#   finale_prism : glass breaking, then more pieces falling at other pitches, a cold echo, weight under it
#   finale_holy  : a blinding light : a rising shine, the bright impact, a long glow, and a deep hum under it
# Usage (repo root) : python3 docs/audio/maestro_sounds.py <folder with the original wavs>
#   the folder holds finale_storm finale_prism finale_holy enemy_broken boss_strike skill_light finale_ready
#   as they were before the DARK PASS (git show 5e56b81~1:Tacetno433/Content/Audio/Sfx/<name>.wav). Needs ffmpeg.
import os, subprocess, sys

SRC = sys.argv[1]
OUT = "Tacetno433/Content/Audio/Sfx"

def src(n): return os.path.join(SRC, n + ".wav")

def mix(out, inputs, chains, tail=""):
    args = ["ffmpeg", "-y", "-v", "error"]
    for p in inputs: args += ["-i", p]
    labels = "".join("[s%d]" % i for i in range(len(chains)))
    fc = ";".join("[%d:a]aresample=44100,%s[s%d]" % (i, c, k) for k, (i, c) in enumerate(chains))
    fc += ";%samix=inputs=%d:normalize=0%s,alimiter=limit=0.92" % (labels, len(chains), tail)
    args += ["-filter_complex", fc, "-ac", "2", os.path.join(OUT, out + ".wav")]
    r = subprocess.run(args, capture_output=True, text=True)
    if r.returncode: sys.exit(r.stderr)

# LIGHTNING : crack (the storm's first moment, only its highs, driven hard) + thunder (the storm an octave down, slow) + hit
mix("finale_storm", [src("finale_storm"), src("finale_storm"), src("boss_strike"), src("finale_storm")], [
    (0, "atrim=0:0.5,highpass=f=1800,volume=3.2,asoftclip=type=tanh,afade=t=out:st=0.25:d=0.25"),
    (1, "asetrate=44100*0.5,aresample=44100,lowpass=f=700,volume=2.4,adelay=120|120,afade=t=out:st=5.0:d=2.0"),
    (2, "lowpass=f=500,volume=1.1"),
    (3, "asetrate=44100*0.72,aresample=44100,lowpass=f=1600,volume=1.0,adelay=40|40"),
], ",atrim=0:7")

# GLASS : the big break, the prism's ice and glass, then four falls of pieces at other pitches, echo, weight
mix("finale_prism", [src("enemy_broken"), src("finale_prism"), src("enemy_broken"), src("enemy_broken"), src("enemy_broken"), src("enemy_broken"), src("boss_strike")], [
    (0, "volume=1.6"),
    (1, "volume=1.0,adelay=30|30"),
    (2, "asetrate=44100*1.3,aresample=44100,volume=0.9,adelay=180|180"),
    (3, "asetrate=44100*0.8,aresample=44100,volume=1.0,adelay=330|330"),
    (4, "asetrate=44100*1.6,aresample=44100,volume=0.7,adelay=520|520"),
    (5, "asetrate=44100*1.1,aresample=44100,volume=0.6,adelay=760|760"),
    (6, "lowpass=f=380,volume=0.8"),
], ",aecho=0.8:0.55:70|160:0.3|0.2,atrim=0:4")

# LIGHT : a rising shine (the glockenspiel climb, high and soft, ends on the strike), the holy impact, the shine's long
# glow (skill_light stretched in echo), and the strike's body far down so the light still feels heavy
mix("finale_holy", [src("finale_ready"), src("finale_holy"), src("skill_light"), src("boss_strike")], [
    (0, "asetrate=44100*1.25,aresample=44100,highpass=f=900,volume=0.55,afade=t=out:st=0.5:d=0.5"),
    (1, "volume=1.3"),
    (2, "aecho=0.8:0.8:120|260|480:0.45|0.35|0.25,volume=0.9,adelay=60|60"),
    (3, "lowpass=f=260,volume=1.0"),
], ",atrim=0:5")
print("MAESTRO SOUNDS done : finale_storm finale_prism finale_holy")
