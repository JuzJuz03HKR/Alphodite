# make_sounds.py : builds every sound and track of TACET 4'33 (block E, 2 Nov 2026, docs/BETA1_DIRECTION.md 47)
#
# READY MADE FIRST (the user 13.134 to 13.136) : almost every sound is a ready made one, picked for TACET's theme (dark,
# orchestral, the silence that breaks like glass, the band's elements) and only cut, levelled and named here. Nothing is
# composed. Iris (the team's AI) cannot hear : she picks by what the packs say a sound is and by measuring it, and the
# user listens and judges every one. A pick is one line below, so swapping a sound is changing that line.
#
# The packs (all CC0, from OpenGameArt.org), unzipped into one folder given with --packs :
#   kenney_interfaceSounds.zip      Kenney, Interface Sounds              opengameart.org/content/interface-sounds
#   RPGsounds_Kenney.zip            Kenney, 50 RPG sound effects          opengameart.org/content/50-rpg-sound-effects
#   sword_-_starninjas_1.zip and sword_clash_-_starninjas_0.zip
#                                   StarNinjas, 20 Sword Sound Effects    opengameart.org/content/20-sword-sound-effects-attacks-and-clashes
#   150gameeffectsounds.zip         untamedpixels, 150 Combat Sound Effects (each file's own maker in its LICENSES.txt)
#                                                                         opengameart.org/content/150-combat-sound-effects
#   80-CC0-RPG-SFX_0.zip            rubberduck, 80 CC0 RPG SFX            opengameart.org/content/80-cc0-rpg-sfx
#   sfx_100_v2.zip                  rubberduck, 100 CC0 SFX #2            opengameart.org/content/100-cc0-sfx-2
#   swishes.zip                     artisticdude, Swishes Sound Pack      opengameart.org/content/swishes-sound-pack
#   magic/magical_2.ogg             JaggedStone, Magic Spell SFX          opengameart.org/content/magic-spell-sfx
# and the VSCO 2 CE orchestra (CC0) for each musician's own note and a few orchestral ones.
#
# Three labels (docs/CREDITS.md) :
#   OURS      made here from CC0 recordings or from noise        -> Tacetno433/Content/Audio/Sfx or Music (in the repo)
#   FREE      someone else's CC0 or CC-BY file, cut and levelled -> Tacetno433/Content/Audio/Sfx or Music (in the repo)
#   BORROWED  other games' or of unknown origin (the playtest only, the user 13.125 13.132)
#             -> the folder given with --borrowed, NEVER the repo. Copy it into Tacetno433/Content/Audio/Borrowed.
#
# Needs : python3 with numpy scipy soundfile, and ffmpeg (for ogg)
#   pip install numpy scipy soundfile
#   git clone https://github.com/sgossner/VSCO-2-CE        (the orchestra, CC0, or use SoundLibrary/ for the files it has)
#   the Drive folder soundgame downloaded somewhere
# Run from the repository root :
#   python3 docs/audio/make_sounds.py --vsco <VSCO-2-CE folder> --packs <packs folder> --drive <soundgame folder> --borrowed <out folder>
# It also writes docs/audio/MANIFEST.md : every file, its label and what it is made of.

import argparse, os, sys, math, json, subprocess, tempfile
from fractions import Fraction
import numpy as np
import soundfile as sf
from scipy.signal import resample_poly, butter, sosfiltfilt, fftconvolve

SR = 44100
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SFX_OUT = os.path.join(ROOT, 'Tacetno433', 'Content', 'Audio', 'Sfx')
MUSIC_OUT = os.path.join(ROOT, 'Tacetno433', 'Content', 'Audio', 'Music')
rng = np.random.default_rng(433)          # the noise is the same every run

# ------------------------------------------------------------------------------------------------------------------
# Finding the sources
# ------------------------------------------------------------------------------------------------------------------
VSCO_DIR = None
DRIVE_DIR = None
PACKS_DIR = None
_index = None


def vsco(rel):
    """A VSCO 2 CE file by its path in that library. Looked for in --vsco first, then anywhere in SoundLibrary/
    (the team's copy keeps some of the same files under shorter folders)."""
    global _index
    if VSCO_DIR:
        p = os.path.join(VSCO_DIR, rel)
        if os.path.exists(p) and os.path.getsize(p) > 1000:
            return p
    if _index is None:
        _index = {}
        for d, _, fs in os.walk(os.path.join(ROOT, 'SoundLibrary')):
            for f in fs:
                _index[f] = os.path.join(d, f)
    name = os.path.basename(rel)
    if name in _index:
        return _index[name]
    raise FileNotFoundError('VSCO file not found : ' + rel + ' (give --vsco a VSCO-2-CE checkout)')


def pack(rel):
    p = os.path.join(PACKS_DIR, rel)
    if not os.path.exists(p):
        raise FileNotFoundError('pack file not found : ' + p)
    return p


def drive(rel):
    p = os.path.join(DRIVE_DIR, rel)
    if not os.path.exists(p):
        raise FileNotFoundError('Drive file not found : ' + p)
    return p


# ------------------------------------------------------------------------------------------------------------------
# Sound as numbers : float32 arrays, shape (samples, 2), at 44.1 kHz
# ------------------------------------------------------------------------------------------------------------------
def load(path, start=0.0, length=None):
    x, sr = sf.read(path, always_2d=True, dtype='float32')
    if x.shape[1] == 6:                       # 5.1 (L R C LFE Ls Rs) down to two
        l = x[:, 0] + 0.707 * x[:, 2] + 0.707 * x[:, 4] + 0.5 * x[:, 3]
        r = x[:, 1] + 0.707 * x[:, 2] + 0.707 * x[:, 5] + 0.5 * x[:, 3]
        x = np.stack([l, r], axis=1) / 2.2
    elif x.shape[1] == 1:
        x = np.repeat(x, 2, axis=1)
    elif x.shape[1] > 2:
        x = x[:, :2]
    a = int(start * sr)
    b = len(x) if length is None else min(len(x), a + int(length * sr))
    x = x[a:b]
    if sr != SR:
        f = Fraction(SR, sr).limit_denominator(1000)
        x = resample_poly(x, f.numerator, f.denominator, axis=0).astype(np.float32)
    return x


def seconds(x):
    return len(x) / SR


def silence(sec):
    return np.zeros((int(sec * SR), 2), np.float32)


def db(v):
    return 10 ** (v / 20.0)


def peak(x):
    return float(np.max(np.abs(x))) if len(x) else 0.0


def gain(x, d):
    return x * db(d)


def norm(x, to=-1.0):
    p = peak(x)
    return x * (db(to) / p) if p > 0 else x


def trim(x, floor=-60.0, keep_tail=True):
    """Leading silence off (relative to the loudest moment), so a hit sounds the frame it is played"""
    p = peak(x)
    if p == 0:
        return x
    m = np.max(np.abs(x), axis=1)
    on = np.where(m > p * db(floor))[0]
    if len(on) == 0:
        return x
    a = max(0, on[0] - int(0.002 * SR))
    b = on[-1] + 1 if not keep_tail else len(x)
    return x[a:b]


def cut(x, sec):
    return x[:int(sec * SR)]


def fade_out(x, sec):
    n = min(len(x), int(sec * SR))
    if n > 0:
        x = x.copy()
        x[-n:] *= np.linspace(1, 0, n, dtype=np.float32)[:, None] ** 2
    return x


def fade_in(x, sec):
    n = min(len(x), int(sec * SR))
    if n > 0:
        x = x.copy()
        x[:n] *= np.linspace(0, 1, n, dtype=np.float32)[:, None] ** 2
    return x


def shift(x, semitones):
    """Tuned up or down by resampling (a short sound gets a little shorter or longer, as a real instrument would)"""
    if abs(semitones) < 1e-6:
        return x
    f = Fraction(2 ** (-semitones / 12.0)).limit_denominator(400)
    return resample_poly(x, f.numerator, f.denominator, axis=0).astype(np.float32)


def lowpass(x, hz):
    sos = butter(4, hz, 'lowpass', fs=SR, output='sos')
    return sosfiltfilt(sos, x, axis=0).astype(np.float32)


def highpass(x, hz):
    sos = butter(4, hz, 'highpass', fs=SR, output='sos')
    return sosfiltfilt(sos, x, axis=0).astype(np.float32)


def reverse(x):
    return x[::-1].copy()


def reverb(x, sec=1.6, wet=0.25, bright=6000):
    """A plain hall : the sound convolved with fading noise (two different noises, so it is wide)"""
    n = int(sec * SR)
    t = np.arange(n) / SR
    env = np.exp(-6.0 * t / sec)
    ir = np.stack([rng.standard_normal(n) * env, rng.standard_normal(n) * env], axis=1).astype(np.float32)
    ir = lowpass(ir, bright)
    ir /= np.sqrt(np.sum(ir ** 2, axis=0, keepdims=True)) + 1e-9
    tail = np.stack([fftconvolve(x[:, c], ir[:, c]) for c in range(2)], axis=1).astype(np.float32)
    dry = np.vstack([x, np.zeros((len(tail) - len(x), 2), np.float32)])
    return dry * (1 - wet) + tail * wet


def mix(parts, length=None):
    """parts : (sound, start in seconds, gain in dB). Everything laid on one track."""
    end = max(int(o * SR) + len(s) for s, o, g in parts)
    if length is not None:
        end = int(length * SR)
    out = np.zeros((end, 2), np.float32)
    for s, o, g in parts:
        a = int(o * SR)
        if a >= end:
            continue
        b = min(end, a + len(s))
        out[a:b] += s[:b - a] * db(g)
    return out


def mono(x):
    m = x.mean(axis=1, keepdims=True)
    return np.repeat(m, 2, axis=1)


def svf_band(sig, freqs, q):
    """ADVANCED PART : a band pass filter whose centre moves sample by sample (a state variable filter). Used to make
    a swish : noise whose pitch rises, like air cut by a blade."""
    low = band = 0.0
    out = np.zeros_like(sig)
    damp = 1.0 / q
    for i in range(len(sig)):
        f = 2.0 * math.sin(math.pi * min(freqs[i], SR / 6.0) / SR)
        high = sig[i] - low - damp * band
        band += f * high
        low += f * band
        out[i] = band
    return out


def noise(sec):
    return rng.standard_normal(int(sec * SR)).astype(np.float32)


def stereo(m):
    return np.stack([m, m], axis=1).astype(np.float32)


# ------------------------------------------------------------------------------------------------------------------
# Writing
# ------------------------------------------------------------------------------------------------------------------
MANIFEST = []


def write_wav(folder, name, x, label, made_of, to_peak=-1.0, mono_out=True):
    x = norm(x, to_peak)
    x = np.clip(x, -1, 1)
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, name + '.wav')
    data = x.mean(axis=1) if mono_out else x
    sf.write(path, data, SR, subtype='PCM_16')
    MANIFEST.append((name + '.wav', label, made_of, round(seconds(x), 2)))
    return path


def loudness(path):
    r = subprocess.run(['ffmpeg', '-hide_banner', '-nostats', '-i', path, '-af', 'ebur128', '-f', 'null', '-'],
                       capture_output=True, text=True).stderr
    vals = [float(v) for v in __import__('re').findall(r'I:\s+(-?[\d.]+) LUFS', r)]
    return vals[-1] if vals else -20.0


def loop_ready(x, cross=3.0, floor=-40.0):
    """A track that loops : the quiet start and end are trimmed, then the end is faded into the start, so when
    MediaPlayer jumps back to the beginning the music carries on with no gap and no click."""
    m = np.max(np.abs(x), axis=1)
    p = m.max()
    hop = int(0.05 * SR)
    loud = np.array([m[i:i + hop].max() for i in range(0, len(m) - hop, hop)])
    on = np.where(loud > p * db(floor))[0]
    a, b = on[0] * hop, (on[-1] + 1) * hop
    x = x[a:b]
    n = int(cross * SR)
    if len(x) < 3 * n:
        return x
    head, body = x[:n], x[n:]
    t = np.linspace(0, 1, n, dtype=np.float32)[:, None]
    body = body.copy()
    body[-n:] = body[-n:] * np.cos(t * math.pi / 2) + head * np.sin(t * math.pi / 2)
    return body


def write_ogg(folder, name, x, label, made_of, lufs=-18.0, loop=True):
    if loop:
        x = loop_ready(x)
    os.makedirs(folder, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        wav = os.path.join(tmp, 'a.wav')
        sf.write(wav, np.clip(x, -1, 1), SR, subtype='FLOAT')
        g = lufs - loudness(wav)
        y = x * db(g)
        if peak(y) > db(-1.0):                # never past -1 dB : quieter than asked rather than clipped
            y = norm(y, -1.0)
        sf.write(wav, np.clip(y, -1, 1), SR, subtype='PCM_16')
        path = os.path.join(folder, name + '.ogg')
        subprocess.run(['ffmpeg', '-y', '-hide_banner', '-loglevel', 'error', '-i', wav, '-c:a', 'libvorbis',
                        '-q:a', '5', path], check=True)
    MANIFEST.append((name + '.ogg', label, made_of, round(seconds(x), 1)))
    return path


# ------------------------------------------------------------------------------------------------------------------
# Picking : a ready made sound, cut so it sounds the moment it is played, then levelled
# ------------------------------------------------------------------------------------------------------------------
def V(rel, start=0.0, length=None):
    return trim(load(vsco(rel), start, length))


def land(x, before=0.012):
    """A hit starts right at its loudest moment (a little before it), so it lands on the frame the game plays it"""
    m = np.max(np.abs(x), axis=1)
    i = int(np.argmax(m))
    return x[max(0, i - int(before * SR)):]


def peak_at(x):
    return int(np.argmax(np.max(np.abs(x), axis=1))) / SR


def P(rel, sec=None, fade=None, at_peak=False, start=0.0):
    """One ready made file from the packs"""
    x = trim(load(pack(rel), start))
    if at_peak:
        x = land(x)
    if sec is not None:
        x = cut(x, sec)
    if fade is not None:
        x = fade_out(x, fade)
    return x


C = '150gameeffectsounds/'
K = 'kenney_interfaceSounds/Audio/'
CREDIT = {
    's01': 'Cosmo Myzrail Gorynych (CoMiGo), Bleeps and Bloops, CC0, in untamedpixels\' 150 Combat Sound Effects',
    's02': 'Kenney, Impact Sounds, CC0, in untamedpixels\' 150 Combat Sound Effects',
    's03': 'Kenney, RPG Audio, CC0, in untamedpixels\' 150 Combat Sound Effects',
    's04': 'Kenney, Sci-fi Sounds, CC0, in untamedpixels\' 150 Combat Sound Effects',
    's06': 'lentikula, Basic Spell Impacts, CC0, in untamedpixels\' 150 Combat Sound Effects',
    's07': 'lentikula, Druid Spell Impacts, CC0, in untamedpixels\' 150 Combat Sound Effects',
    's08': 'lentikula, Healing Spell Impacts, CC0, in untamedpixels\' 150 Combat Sound Effects',
}


def combat(rel):
    """Who made a file of the 150 Combat pack : its name ends in its source's tag (the pack's LICENSES.txt)"""
    tag = rel.rsplit('_', 1)[-1].split('.')[0]
    return '"' + os.path.basename(rel).rsplit('_', 1)[0] + '" by ' + CREDIT.get(tag, 'see the pack\'s LICENSES.txt')


def make_band():
    """BAND NOTES : each legend's own instrument, one VSCO recording each, D minor so two at once never clash
    (D2 D3 F3 A5 D6). Iris measured each recording's pitch : VSCO names its notes one octave lower than they sound."""
    S = SFX_OUT
    write_wav(S, 'band_violin', fade_out(cut(V('Strings/Solo Violin/spic/LLVln_spic_A4_v2_rr1.wav'), 0.7), 0.35), 'FREE',
              'VSCO 2 CE (Versilian Studios, CC0) solo violin spiccato, sounds A5')
    write_wav(S, 'band_flute', fade_out(cut(shift(V('Woodwinds/Flute/stac/LDFlute_stac_C6_v2_rr1.wav'), -10), 0.6), 0.3), 'FREE',
              'VSCO 2 CE (CC0) flute staccato, tuned to D6')
    drum = fade_out(cut(shift(V('Percussion/Timpani/Timpani2_Hit_v3_rr1_Sum.wav'), 3.23), 1.4), 0.9)
    drum = np.tanh(norm(drum, 0.0) * db(9.0))          # a low drum's loudness is all in its first moment : pushed up softly
    write_wav(S, 'band_timpani', drum, 'FREE', 'VSCO 2 CE (CC0) timpani hit, tuned to D2, made fuller')
    write_wav(S, 'band_cello', fade_out(cut(V('Strings/Cello Section/spic/spic_D2_v2_RR1.wav'), 0.8), 0.4), 'FREE',
              'VSCO 2 CE (CC0) cello section spiccato, sounds D3')
    write_wav(S, 'band_horn', fade_out(cut(V('Brass/F Horn/stac/MOHorn_stac_F2_v2_rr1.wav'), 0.8), 0.4), 'FREE',
              'VSCO 2 CE (CC0) French horn staccato, sounds F3')


def make_fight(B):
    """The blows, the notes, the parry, the hurt, TACET, the skills by element, the FINALE, the bosses"""
    S = SFX_OUT
    # A blow's weapon : strings cut, winds strike quick, drums smash (the user's Drive hits win when borrowed)
    write_wav(S, 'weapon_cut', P(C + '01_combat_melee/knife_slice2_48kHz_16bit_0.56s_d1b73212_s03.wav', 0.5, 0.25, True), 'FREE', combat('knife_slice2_s03'))
    write_wav(S, 'weapon_shot', P(C + '01_combat_melee/Impact_01_s01.wav', 0.3, 0.15, True), 'FREE', combat('Impact_01_s01'))
    write_wav(S, 'weapon_smash', P(C + '01_combat_melee/impact_punch_heavy_002_48kHz_16bit_0.46s_565485a1_s02.wav', 0.45, 0.2, True), 'FREE', combat('impact_punch_heavy_002_s02'))
    # A skill's blow : the musician's own element (Musician.Spot), the same colour as its VFX
    write_wav(S, 'skill_fire', P(C + '04_magic_spells/Fire Spell Impact 4_s06.wav', 1.0, 0.6, True), 'FREE', combat('Fire Spell Impact 4_s06'), mono_out=False)
    write_wav(S, 'skill_wind', P(C + '04_magic_spells/Wind Spell Impact 5_s07.wav', 1.1, 0.7, True), 'FREE', combat('Wind Spell Impact 5_s07'), mono_out=False)
    write_wav(S, 'skill_earth', P(C + '04_magic_spells/Earth Spell Impact 4_s07.wav', 1.1, 0.7, True), 'FREE', combat('Earth Spell Impact 4_s07'), mono_out=False)
    write_wav(S, 'skill_water', P(C + '04_magic_spells/Water Spell Impact 4_s06.wav', 1.0, 0.6, True), 'FREE', combat('Water Spell Impact 4_s06'), mono_out=False)
    write_wav(S, 'skill_light', P(C + '04_magic_spells/Healing Spell Impact 10_s08.wav', 0.8, 0.5, True), 'FREE', combat('Healing Spell Impact 10_s08'), mono_out=False)
    write_wav(S, 'skill_heal', P(C + '04_magic_spells/Healing Spell Impact 1_s08.wav', 1.2, 0.7, start=0.35), 'FREE', combat('Healing Spell Impact 1_s08'), mono_out=False)
    write_wav(S, 'skill_guard', P('sfx_100_v2/sfx100v2_metal_hit_01.ogg', 0.7, 0.4, True), 'FREE', '"metal hit 01" by rubberduck, 100 CC0 SFX #2, CC0')
    write_wav(S, 'skill_boost', P('magic/magical_2.ogg', 1.2, 0.6), 'FREE', '"magical_2" by JaggedStone, Magic Spell SFX, CC0', mono_out=False)
    # A note judged
    write_wav(S, 'qte_perfect', fade_out(cut(V('VSCO 1 Percussion/varMetal/triangle/3/triangle3_hit_mf.wav'), 0.8), 0.5), 'FREE', 'VSCO 2 CE (CC0) triangle')
    write_wav(S, 'qte_normal', P(K + 'tick_002.ogg'), 'FREE', '"tick_002" by Kenney, Interface Sounds, CC0', to_peak=-6.0)
    write_wav(S, 'qte_miss', P('swishes/swishes/swish-2.wav'), 'FREE', '"swish-2" by artisticdude, Swishes Sound Pack, CC0 : the baton cuts only air')
    # The parry : TACET's note swings in (loudest right on the ring), then blade on blade
    swing = P('sword_-_starninjas_1/sword - StarNinjas/sword.4.ogg')
    write_wav(S, 'enemy_swing', swing, 'FREE', '"sword.4" by StarNinjas, 20 Sword Sound Effects, CC0 (loudest %.3f s in : BattleRules.SoundSwingLead)' % peak_at(swing))
    print('   enemy_swing is loudest at %.3f s : BattleRules.SoundSwingLead' % peak_at(swing))
    write_wav(S, 'parry_perfect', P('sword_clash_-_starninjas_0/sword_clash.9.ogg', 0.8, 0.5, True), 'FREE', '"sword_clash.9" by StarNinjas, 20 Sword Sound Effects, CC0')
    write_wav(S, 'parry_good', P('sword_clash_-_starninjas_0/sword_clash.2.ogg', 0.4, 0.2, True), 'FREE', '"sword_clash.2" by StarNinjas, 20 Sword Sound Effects, CC0', to_peak=-3.0)
    # A held note, a ROLL, the band's turn
    trem = load(vsco('Strings/Violin Section/Trem/VlnEns_Trem_D5_v1.wav'), 0.6, 1.6)
    n = int(0.3 * SR)
    t = np.linspace(0, 1, n, dtype=np.float32)[:, None]
    body = trem[n:].copy()
    body[-n:] = body[-n:] * np.cos(t * math.pi / 2) + trem[:n] * np.sin(t * math.pi / 2)    # ends into its own start
    write_wav(S, 'hold_loop', body, 'FREE', 'VSCO 2 CE (CC0) violin section tremolo, made to loop', to_peak=-3.0)
    write_wav(S, 'roll_tap', fade_out(cut(V('VSCO 1 Percussion/drums/snare/drum1/snare1_mp_1.wav'), 0.25), 0.15), 'FREE', 'VSCO 2 CE (CC0) snare, soft')
    taps = mix([(cut(V('VSCO 1 Percussion/varWood/claves_mf.wav'), 0.2), 0, -2), (cut(V('VSCO 1 Percussion/varWood/claves_mf_2.wav'), 0.3), 0.16, 0)])
    write_wav(S, 'round_start', fade_out(taps, 0.1), 'FREE', 'VSCO 2 CE (CC0) claves twice : the baton taps the stand')
    # Hurt and falls
    write_wav(S, 'band_hurt', P(C + '01_combat_melee/impact_punch_heavy_000_48kHz_16bit_0.65s_8fb58ba7_s02.wav', 0.6, 0.3, True), 'FREE', combat('impact_punch_heavy_000_s02'))
    write_wav(S, 'band_down', P('sfx_breaking_and_falling/bfh1_metal_falling_01.ogg', 1.1, 0.4), 'FREE', '"bfh1 metal falling 01" by rubberduck, 75 CC0 breaking falling hit SFX, CC0 : the instrument falls')
    # TACET : its shell breaks like glass (the white glass of its VFX)
    glass = P(C + '03_explosions_destruction/BreakGlass_02_s01.wav')
    boom = P(C + '03_explosions_destruction/low_frequency_explosion_001_48kHz_16bit_1s_a63b8c61_s04.wav', 0.9, 0.5, True)
    write_wav(S, 'enemy_silenced', mix([(glass, 0, 0), (boom, 0, -4)]), 'FREE', combat('BreakGlass_02_s01') + ' with ' + combat('low_frequency_explosion_001_s04'), mono_out=False)
    write_wav(S, 'enemy_broken', P(C + '03_explosions_destruction/BreakGlass_01_s01.wav'), 'FREE', combat('BreakGlass_01_s01'))
    write_wav(S, 'enemy_charge', fade_out(fade_in(cut(V('Percussion/Timpani/Rolls/Timpani1_Roll_v3_rr1_Sum.wav'), 1.4), 1.0), 0.25), 'FREE', 'VSCO 2 CE (CC0) timpani roll, swelling')
    write_wav(S, 'enemy_turn', fade_out(cut(V('VSCO 1 Percussion/varMetal/Gong/gong_hit_ppp.wav'), 1.8), 1.0), 'FREE', 'VSCO 2 CE (CC0) gong, very soft')
    write_wav(S, 'stunned', fade_out(cut(V('VSCO 1 Percussion/varMetal/triangle/1/triangle1_roll.wav'), 0.9), 0.5), 'FREE', 'VSCO 2 CE (CC0) triangle roll')
    # FINALE : the gauge, then each conductor's own (STORM, PRISM, HOLY : their block D looks)
    write_wav(S, 'finale_ready', fade_out(cut(V('Miscellania Raw/Misc 2/glock_glisses/glock_fx_up_pentatonic_med_01.wav'), 1.6), 0.8), 'FREE',
              'VSCO 2 CE (CC0) glockenspiel glissando up', mono_out=False)
    bolt = P(C + '04_magic_spells/Lightning Spell Impact 3_s06.wav', at_peak=True)
    thunder = P('sfx_100_v2/sfx100v2_thunder_01.ogg', start=0.8)
    write_wav(S, 'finale_storm', fade_out(cut(mix([(bolt, 0, 0), (thunder, 0.15, -2)]), 3.6), 1.6), 'FREE',
              combat('Lightning Spell Impact 3_s06') + ' with "thunder 01" by rubberduck, 100 CC0 SFX #2, CC0', mono_out=False)
    ice = P(C + '04_magic_spells/Ice Spell Impact 2_s06.wav', at_peak=True)
    write_wav(S, 'finale_prism', fade_out(cut(mix([(ice, 0, 0), (glass, 0.05, -2)]), 1.8), 1.0), 'FREE',
              combat('Ice Spell Impact 2_s06') + ' with ' + combat('BreakGlass_02_s01'), mono_out=False)
    holy = P(C + '04_magic_spells/Healing Spell Impact 5_s08.wav', start=0.25)
    bells = trim(load(vsco('Miscellania Raw/Misc 2/NepaleseBells/fx_med_main.wav')))
    write_wav(S, 'finale_holy', fade_out(cut(mix([(holy, 0, 0), (bells, 0.2, -6)]), 2.8), 1.4), 'FREE',
              combat('Healing Spell Impact 5_s08') + ' with VSCO 2 CE (CC0) Nepalese bells', mono_out=False)
    # The bosses (the user's Drive braams win when borrowed)
    roll = V('Percussion/Timpani/Rolls/Timpani1_Roll_v5_rr1_Sum.wav')
    gong = V('VSCO 1 Percussion/varMetal/Gong/gong_hit_ff_hard.wav')
    write_wav(S, 'boss_entrance', fade_out(cut(mix([(fade_in(cut(roll, 0.95), 0.8), 0, -2), (gong, 0.95, 0)]), 3.6), 1.6), 'FREE',
              'VSCO 2 CE (CC0) timpani roll into a gong, landing as the boss rises (0.95 s)', mono_out=False)
    write_wav(S, 'boss_tell', fade_out(fade_in(cut(V('Brass/F Horn/sus/MOHorn_sus_D2_v2_1.wav'), 1.8), 0.5), 0.6), 'FREE',
              'VSCO 2 CE (CC0) French horn, a long low D swelling')
    write_wav(S, 'boss_strike', fade_out(cut(V('VSCO 1 Percussion/varMetal/Cymbals/clash/crash_hit_fff_loose.wav'), 2.0), 1.0), 'FREE',
              'VSCO 2 CE (CC0) crash cymbals fff', mono_out=False)
    big = P(C + '03_explosions_destruction/low_frequency_explosion_000_48kHz_16bit_2s_84586419_s04.wav', 1.8, 1.0, True)
    write_wav(S, 'boss_turn', mix([(P(C + '03_explosions_destruction/BreakGlass_03_s01.wav'), 0, 0), (big, 0, -2)]), 'FREE',
              combat('BreakGlass_03_s01') + ' with ' + combat('low_frequency_explosion_000_s04') + ' : the white shell breaks', mono_out=False)
    write_wav(S, 'boss_down', fade_out(cut(V('VSCO 1 Percussion/varMetal/Gong/gong_hit_fff.wav'), 4.0), 2.0), 'FREE', 'VSCO 2 CE (CC0) gong fff', mono_out=False)
    write_wav(S, 'fight_start', fade_out(cut(V('VSCO 1 Percussion/varMetal/Gong/gong_hit_mp.wav'), 2.0), 1.0), 'FREE', 'VSCO 2 CE (CC0) gong mp', mono_out=False)
    if B:
        h1 = land(load(drive('hit/Hit_1.wav')))
        write_wav(B, 'weapon_cut', fade_out(cut(h1, 0.6), 0.2), 'BORROWED', 'Drive soundgame/เสียงตี/Hit_1.wav (source not known yet)')
        h2 = land(load(drive('hit/HIt_2.wav')))
        write_wav(B, 'weapon_shot', fade_out(cut(h2, 0.65), 0.2), 'BORROWED', 'Drive soundgame/เสียงตี/HIt_2.wav (source not known yet)')
        h3 = trim(load(drive('hit/Hit_3.wav')))
        write_wav(B, 'weapon_smash', fade_out(cut(h3, 0.5), 0.15), 'BORROWED', 'Drive soundgame/เสียงตี/Hit_3.wav (source not known yet)')
        f = 'fight/braam_'
        src = 'Drive soundgame/song/Fight monter effect : '
        rise = load(drive(f + 'airy_rise_sub_impact_SND58427.wav'), 4.29 - 0.95)
        write_wav(B, 'boss_entrance', fade_out(cut(rise, 3.6), 1.6), 'BORROWED', src + 'Designed, Braam, Airy Rise, Sub Impact, Clean, Classic SND58427, its impact moved to 0.95 s (source not known yet)', mono_out=False)
        write_wav(B, 'boss_strike', fade_out(cut(load(drive(f + 'orchestral_impact_huge_epic_SND58575.wav')), 3.0), 1.2), 'BORROWED', src + 'Designed, Braam, Orchestral Impact, Huge, Epic 01 SND58575 (source not known yet)', mono_out=False)
        write_wav(B, 'boss_tell', fade_out(cut(trim(load(drive(f + 'classic_soft_brass_short_SND56455.wav'))), 3.0), 1.2), 'BORROWED', src + 'Designed, Braam, Classic, Soft, Brass, Short (C#) SND56455 (source not known yet)', mono_out=False)
        write_wav(B, 'boss_turn', fade_out(cut(trim(load(drive(f + 'crunchy_baritone_SND58610.wav'))), 3.0), 1.2), 'BORROWED', src + 'Designed, Braam, Crunchy Baritone SND58610 (source not known yet)', mono_out=False)
        write_wav(B, 'boss_down', fade_out(cut(trim(load(drive(f + 'dark_double_bass_long_SND56476.wav'))), 5.0), 2.0), 'BORROWED', src + 'Designed, Braam, Dark, Double Bass, Long SND56476 (source not known yet)', mono_out=False)
        write_wav(B, 'enemy_charge', fade_out(cut(trim(load(drive(f + 'rumbling_low_soft_SND56543.wav'))), 2.2), 1.0), 'BORROWED', src + 'Designed, Braam, Rumbling, Low, Soft SND56543 (source not known yet)', mono_out=False)
        write_wav(B, 'fight_start', fade_out(cut(trim(load(drive('gui/Enter the Fight.wav'))), 1.8), 0.8), 'BORROWED', 'Drive soundgame/GUI/Enter the Fight.wav (source not known yet)', mono_out=False)


def make_ui(B):
    """The pages : Kenney's clean interface sounds (the user wants clean UI, Honkai: Star Rail and CZN), small orchestral
    moments from VSCO where a page has a moment of its own"""
    S = SFX_OUT
    write_wav(S, 'ui_move', P(K + 'select_002.ogg'), 'FREE', '"select_002" by Kenney, Interface Sounds, CC0', to_peak=-8.0)
    write_wav(S, 'ui_confirm', P(K + 'confirmation_003.ogg'), 'FREE', '"confirmation_003" by Kenney, Interface Sounds, CC0', to_peak=-4.0)
    write_wav(S, 'ui_back', P(K + 'back_002.ogg'), 'FREE', '"back_002" by Kenney, Interface Sounds, CC0', to_peak=-5.0)
    write_wav(S, 'ui_denied', P(K + 'error_004.ogg'), 'FREE', '"error_004" by Kenney, Interface Sounds, CC0', to_peak=-4.0)
    write_wav(S, 'path_chosen', P(K + 'maximize_006.ogg'), 'FREE', '"maximize_006" by Kenney, Interface Sounds, CC0', to_peak=-3.0)
    write_wav(S, 'page_turn', P('RPGsounds_Kenney/OGG/bookFlip2.ogg'), 'FREE', '"bookFlip2" by Kenney, RPG sound effects, CC0', to_peak=-6.0)
    write_wav(S, 'check_pass', P(K + 'confirmation_002.ogg'), 'FREE', '"confirmation_002" by Kenney, Interface Sounds, CC0', to_peak=-3.0)
    write_wav(S, 'check_fail', P(K + 'error_006.ogg'), 'FREE', '"error_006" by Kenney, Interface Sounds, CC0', to_peak=-3.0)
    write_wav(S, 'motif_get', P('80-CC0-RPG-SFX_0/item_gem_01.ogg'), 'FREE', '"item gem 01" by rubberduck, 80 CC0 RPG SFX, CC0', to_peak=-3.0)
    write_wav(S, 'rehearse', P('magic/magical_2.ogg', 1.2, 0.6), 'FREE', '"magical_2" by JaggedStone, Magic Spell SFX, CC0 (the same as skill_boost)', mono_out=False)
    write_wav(S, 'rest_recover', P(C + '04_magic_spells/Healing Spell Impact 12_s08.wav', 1.4, 0.8), 'FREE', combat('Healing Spell Impact 12_s08'), mono_out=False)
    write_wav(S, 'recruit', P(K + 'confirmation_001.ogg'), 'FREE', '"confirmation_001" by Kenney, Interface Sounds, CC0 (under the musician\'s own note)', to_peak=-4.0)
    write_wav(S, 'combo_up', P(K + 'confirmation_004.ogg'), 'FREE', '"confirmation_004" by Kenney, Interface Sounds, CC0', to_peak=-4.0)
    write_wav(S, 'buy', P('80-CC0-RPG-SFX_0/item_coins_01.ogg'), 'FREE', '"item coins 01" by rubberduck, 80 CC0 RPG SFX, CC0', to_peak=-3.0)
    write_wav(S, 'beat_tick', P(K + 'tick_001.ogg'), 'FREE', '"tick_001" by Kenney, Interface Sounds, CC0', to_peak=-3.0)
    write_wav(S, 'dice', P('sfx_100_v2/sfx100v2_stones_01.ogg', 0.9, 0.3), 'FREE', '"stones 01" by rubberduck, 100 CC0 SFX #2, CC0', to_peak=-4.0)
    write_wav(S, 'victory', fade_out(cut(V('Miscellania Raw/Misc 2/glock_glisses/glock_fx_up_chromatic_med_01.wav'), 2.0), 1.0), 'FREE',
              'VSCO 2 CE (CC0) glockenspiel glissando up : the light comes back', mono_out=False)
    write_wav(S, 'defeat', fade_out(lowpass(cut(V('VSCO 1 Percussion/varMetal/Gong/gong_hit_p.wav'), 3.0), 2000), 2.0), 'FREE', 'VSCO 2 CE (CC0) gong p, darkened', mono_out=False)
    write_wav(S, 'run_complete', fade_out(cut(trim(load(vsco('Miscellania Raw/Misc 2/NepaleseBells/fx_long_1_main.wav'))), 4.5), 2.0), 'FREE',
              'VSCO 2 CE (CC0) Nepalese bells', mono_out=False)
    if B:
        g = trim(load(drive('gui/some GUI.wav')))
        write_wav(B, 'ui_confirm', fade_out(cut(g, 1.2), 0.8), 'BORROWED', 'Drive soundgame/GUI/some GUI.wav, its first sound (source not known yet)', to_peak=-4.0, mono_out=False)
        e = trim(load(drive('gui/enter_somthing.wav')))
        write_wav(B, 'path_chosen', fade_out(cut(e, 2.6), 1.6), 'BORROWED', 'Drive soundgame/GUI/enter_somthing.wav (source not known yet)', mono_out=False)
        write_wav(B, 'dice', trim(load(drive('gui/Dice event.wav'))), 'BORROWED', 'Drive soundgame/GUI/Dice event.wav (source not known yet)', mono_out=False)


def make_music(B):
    """The tracks : every one ready made, the user's own picks from the Drive (freesound, CC0 and CC-BY). Plain fights
    are quiet ambient, bosses full, THE TURN the biggest (docs/research/E_music.md). A track used twice starts at
    another place the second time. The borrowed ones (the user's picks of unknown origin, or Worldless and other games'
    music the user adds) go to Content/Audio/Borrowed with the same names and win."""
    M = MUSIC_OUT
    s = 'song/'
    fs = 'freesound '
    sz = ' by szegvari, CC0'
    dark = s + '610532__szegvari__dark-sad-mood-orchestra-strings-piano-choir.wav'
    wait = s + '610996__szegvari__long-time-waiting-strings.wav'
    thrill = s + '609755__szegvari__dark-sad-mood-thriller-piano.wav'
    ritual = s + '595853__szegvari__old-ritual.wav'
    force = s + '611098__szegvari__force-dance-orchestra-strings-brass.wav'
    desert = s + '577391__szegvari__desert-fly-orchestra-atmo.wav'
    alone = s + '609647__szegvari__sad-alone-orchestra-strings-vocal-choir.wav'
    epic = s + '250856__joshuaempyre__epic-orchestra-loop.wav'
    write_ogg(M, 'title', load(drive(dark)), 'FREE', fs + '610532 "Dark Sad Mood Orchestra Strings Piano Choir Cinematic Music Atmo Ambient Surround"' + sz, -18)
    write_ogg(M, 'gallery', load(drive(wait)), 'FREE', fs + '610996 "Long Time Waiting - Strings String Solo Orchestra Orchestral Sad Dark Mood Drama Synth Atmo Music Surround"' + sz, -19)
    write_ogg(M, 'route', load(drive(thrill)), 'FREE', fs + '609755 "Dark Sad Mood Thriller Drama Orchestra Piano Keys Cinematic Atmo Music Surround"' + sz, -19)
    write_ogg(M, 'battle', load(drive(ritual)), 'FREE', fs + '595853 "Old ritual - Fantasy Background Soundscape Haunted Atmo Music Synth Drum Orchestra - Mastered"' + sz, -18)
    write_ogg(M, 'elite', load(drive(force)), 'FREE', fs + '611098 "Force Dance - Orchestra Strings Brass Atmo Atmosphere Ambience Music Surround"' + sz, -17)
    write_ogg(M, 'boss', load(drive(force), 20.0), 'FREE', fs + '611098 "Force Dance"' + sz + ' (from 20 s : the playtest uses the borrowed boss track)', -16)
    write_ogg(M, 'boss_turn', load(drive(epic)), 'FREE', fs + '250856 "Epic Orchestra - LOOP" by Joshua Empyre, CC-BY 4.0 (levelled)', -15, loop=False)
    write_ogg(M, 'victory', load(drive(desert)), 'FREE', fs + '577391 "Desert fly Orchestra Atmo"' + sz, -19)
    write_ogg(M, 'shop', load(drive(desert), 12.0), 'FREE', fs + '577391 "Desert fly Orchestra Atmo"' + sz + ' (from 12 s)', -20)
    write_ogg(M, 'defeat', load(drive(alone)), 'FREE', fs + '609647 "Sad Alone Orchestra Strings Vocal Choir Drama Noise music Mood Surround"' + sz, -19)
    write_ogg(M, 'event', load(drive(thrill), 20.0), 'FREE', fs + '609755 "Dark Sad Mood Thriller..."' + sz + ' (from 20 s)', -20)
    write_ogg(M, 'rest', load(drive(wait), 30.0), 'FREE', fs + '610996 "Long Time Waiting..."' + sz + ' (from 30 s)', -20)
    write_ogg(M, 'ending', load(drive(dark), 25.0), 'FREE', fs + '610532 "Dark Sad Mood Orchestra..."' + sz + ' (from 25 s)', -19)
    if B:
        write_ogg(B, 'title', load(drive(s + 'title_music.wav')), 'BORROWED', 'Drive soundgame/song/title_music.wav (source not known yet)', -18)
        write_ogg(B, 'boss', load(drive(s + 'Boss_music_1.wav')), 'BORROWED', 'Drive soundgame/song/Boss_music_1.wav (source not known yet)', -16)


def write_manifest():
    lines = ['# Sound manifest (made by docs/audio/make_sounds.py, do not edit by hand)', '',
             'Labels : FREE = someone else\'s CC0 or CC-BY file, cut and levelled here / '
             'BORROWED = playtest only, never in the repo (docs/CREDITS.md) / OURS = made here (none yet : ready made first, 13.134)', '',
             '| file | label | made of | seconds |', '|---|---|---|---|']
    for name, label, made, sec in MANIFEST:
        lines.append('| `%s` | %s | %s | %s |' % (name, label, made, sec))
    with open(os.path.join(ROOT, 'docs', 'audio', 'MANIFEST.md'), 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(lines) + '\n')


def main():
    global VSCO_DIR, DRIVE_DIR, PACKS_DIR
    ap = argparse.ArgumentParser()
    ap.add_argument('--vsco', help='a VSCO-2-CE checkout (CC0, github.com/sgossner/VSCO-2-CE)')
    ap.add_argument('--packs', required=True, help='the OpenGameArt packs, unzipped (see the top of this file)')
    ap.add_argument('--drive', required=True, help='the Google Drive folder soundgame, downloaded')
    ap.add_argument('--borrowed', help='where the BORROWED files go (never inside the repo)')
    ap.add_argument('--only', help='comma separated : band fight ui music')
    a = ap.parse_args()
    VSCO_DIR, DRIVE_DIR, PACKS_DIR = a.vsco, a.drive, a.packs
    B = a.borrowed
    if B and os.path.abspath(B).startswith(ROOT):
        sys.exit('--borrowed must be outside the repository : those files may never be committed')
    parts = {'band': make_band, 'fight': lambda: make_fight(B), 'ui': lambda: make_ui(B), 'music': lambda: make_music(B)}
    todo = a.only.split(',') if a.only else list(parts)
    for k in todo:
        print('making', k)
        parts[k]()
    if not a.only:
        write_manifest()


if __name__ == '__main__':
    main()
