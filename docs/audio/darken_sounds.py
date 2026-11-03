#!/usr/bin/env python3
# DARK PASS (3 Nov, the user : the bright, sweet, cute sounds and the win sounds go dark, gloomy and fierce like the music)
# Run once, after make_sounds.py, from the repo root : python3 docs/audio/darken_sounds.py
# Each file is pitched down, its highs cut, given a long cold echo; the win sounds also get a low hit under them.
# The originals stay in git history (the commit before the DARK PASS). Needs ffmpeg.
import os, subprocess, sys

SFX = "Tacetno433/Content/Audio/Sfx"
MUSIC = "Tacetno433/Content/Audio/Music"

# name : (pitch factor, low pass Hz, echo, low hit under it)
CHIMES = {
    "victory":      (0.66, 2600, True,  True),    # the glockenspiel climb : now a low bell climb over a drum hit
    "run_complete": (0.62, 2400, True,  True),    # the bells : a funeral bell
    "finale_ready": (0.70, 3000, True,  True),
    "qte_perfect":  (0.70, 3500, False, False),
    "rest_recover": (0.72, 2600, True,  False),
    "skill_heal":   (0.75, 2800, True,  False),
    "skill_light":  (0.78, 3200, True,  False),
    "skill_boost":  (0.75, 3000, True,  False),
    "rehearse":     (0.72, 3000, True,  True),
    "motif_get":    (0.70, 2800, True,  False),
    "combo_up":     (0.72, 3000, False, False),
    "check_pass":   (0.75, 3000, False, False),
    "ui_confirm":   (0.80, 3500, False, False),
    "recruit":      (0.75, 3000, False, False),
    "buy":          (0.78, 3200, False, False),
}
MUSIC_DARK = {"victory": (0.89, 3800), "shop": (0.89, 3800)}    # two semitones down, the air cut, the bass up

def run(args):
    r = subprocess.run(["ffmpeg", "-y", "-v", "error"] + args, capture_output=True, text=True)
    if r.returncode: sys.exit(r.stderr)

def dark_sfx(name, pitch, lowpass, echo, hit):
    path = os.path.join(SFX, name + ".wav"); tmp = path + ".tmp.wav"
    chain = "asetrate=44100*%g,aresample=44100,lowpass=f=%d,bass=g=5:f=140" % (pitch, lowpass)
    if echo: chain += ",aecho=0.8:0.6:90|210:0.35|0.22"
    chain += ",apad=pad_dur=0.4,volume=1.3"
    if hit:                                                # THE LOW HIT : the boss's strike, its body only, under the chime
        thud = os.path.join(SFX, "boss_strike.wav")
        run(["-i", path, "-i", thud, "-filter_complex",
             "[0:a]aresample=44100,%s[a];[1:a]aresample=44100,lowpass=f=420,volume=0.9[b];[a][b]amix=inputs=2:normalize=0,alimiter=limit=0.95" % chain,
             "-ac", "2", tmp])
    else:
        run(["-i", path, "-af", "aresample=44100," + chain + ",alimiter=limit=0.95", "-ac", "2", tmp])
    os.replace(tmp, path)

def dark_music(name, pitch, lowpass):
    path = os.path.join(MUSIC, name + ".ogg"); tmp = path + ".tmp.ogg"
    run(["-i", path, "-af", "aresample=44100,asetrate=44100*%g,aresample=44100,atempo=%g,lowpass=f=%d,bass=g=4:f=110,alimiter=limit=0.95" % (pitch, 1 / pitch, lowpass),
         "-c:a", "libvorbis", "-q:a", "5", tmp])
    os.replace(tmp, path)

for n, v in CHIMES.items(): dark_sfx(n, *v)
for n, v in MUSIC_DARK.items(): dark_music(n, *v)
print("DARK PASS done :", len(CHIMES), "sounds,", len(MUSIC_DARK), "tracks")
