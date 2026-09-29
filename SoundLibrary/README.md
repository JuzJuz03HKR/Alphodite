# SoundLibrary

The team's instrument sample library (added 29 Sep 2026). **Not part of the game build.**
The game uses one short note per musician, converted from these files into
`Tacetno433/Content/Audio/Instruments/` (mono, 16-bit, 44.1 kHz, at most 1.1 s):

| Game file | Source here | Note |
|---|---|---|
| violin.wav | Violin Section/Spic/VlnEns_Spic_C4_v2_rr1.wav | C4 |
| flute.wav | Flute/stac/LDFlute_stac_C5_v2_rr1.wav | C5 |
| timpani.wav | Timpani/Timpani1_Hit_v3_rr1_Sum.wav | no pitch |
| soduang.wav (stand-in) | Viola Section/spic/Violas_spic_C4_v2_rr1.wav | C4 |
| pinai.wav (stand-in) | Oboe/Stacc/Oboe_Stacc_D4_v2_rr1_Main.wav | D4 |
| ranatek.wav (stand-in) | Xylo/Xylo_Medium_C5_ff_01_far.wav | C5 |
| cello.wav | Cello Section/spic/spic_C3_v2_RR1.wav | C3 |
| horn.wav | F Horn/stac/MOHorn_stac_C3_v2_rr1.wav | C3 |
| bassdrum.wav | drums/bass/bdrum_f_1.wav | no pitch |

The instruments are switched off for now (`SoundBank.InstrumentsOn = false`, 29 Sep).
