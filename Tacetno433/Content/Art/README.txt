ART FOLDER
==========

Pictures go in here. Nothing in this folder is needed for the game to run: every slot shows
an EMPTY BOX (a faint frame with four corner marks) until its picture exists. The box sits
exactly where the picture will go, at the size it will be drawn.

After adding a file, open Content.mgcb with the MGCB editor and add it, so it gets built.
The game picks it up on its own after that - no code has to change.
Press F3 while playing to see how many pictures were found:
    HAND = hand poses    ART = still pictures    PIXEL = pixel musician frames

Draw pictures with a transparent background where the shape is not a full rectangle.
Names are always lower case, and spaces or ' become _   ("THE DEVIL'S STRING" -> the_devil_s_string).
All the slots are listed in code in Core/ArtBank.cs.


1. PIXEL MUSICIANS ON THE DUEL STAGE  (animated, one set per musician)
----------------------------------------------------------------------
Folder:  Content/Art/Musicians
Name:    <name>_<animation>_<number>.png, the number starts at 0

    mali_idle_0.png   mali_idle_1.png   ...     standing and breathing, loops
    mali_attack_0.png mali_attack_1.png ...     playing their beat, plays once
    mali_hurt_0.png   ...                       TACET won the clash on their beat, plays once

Up to 12 frames per animation. Idle alone is enough to start with; a missing attack or hurt
simply keeps showing idle.
Pose: FACING RIGHT (toward TACET), feet on the bottom row of the picture.
Every frame of one musician must be on the same canvas size.
Scale: drawn at x3 (CharacterArt.PixelScale in Core/CharacterArt.cs) with no smoothing, so
the pixels stay sharp. A canvas of about 32 x 56 fills the 100 x 170 stage box at x3.
The same idle frame is also used, fitted and still, on the stage page seats, in the rest of
the band at the curtain call and on the band name page.
Speed: CharacterArt.FrameTime (idle 0.14, attack 0.06, hurt 0.08 seconds per frame).
Code: Core/CharacterArt.cs, positions in Data/StageLayout.cs (DuelColumnX ...)

Instead of animation frames, a single standing picture also works:  stand_<name>.png


2. MUSICIAN PORTRAIT AND FACE  (one each per musician)
------------------------------------------------------
Folder:  Content/Art/Musicians
    portrait_mali.png    full body. Recruit page (160 x 236) and the roster cards (about
                         110 x 128, top of the card). Paint it tall, about 320 x 472.
    face_mali.png        a square head shot. Score rows, the duel's band panel, the stage
                         page details. Paint it square, about 160 x 160.
Code: Core/MusicianArt.cs


3. CONDUCTORS  (one each per conductor)
---------------------------------------
Folder:  Content/Art/Conductors
    portrait_the_inferno.png   the gallery painting (302 x 402), the profile page figure
                               (320 x 530), the bow at the curtain call. Paint about 600 x 900.
    face_the_inferno.png       a square head shot: profile page, duel signature panel.
    cutin_the_inferno.png      the SIGNATURE cut-in, 360 x 380. It breaks out of the black band
                               above and below, like an E.G.O cut-in in Limbus Company.
Code: Core/PortraitBox.cs, Screens/DuelScreen.Hud.cs (DrawCutIn)


4. ENEMIES  (one per shape of TACET)
------------------------------------
Folder:  Content/Art/Enemies
    enemy_hush.png  enemy_dead_air.png  enemy_the_lull.png  enemy_static.png
    enemy_the_mute_choir.png  enemy_white_noise.png
    enemy_requiem.png  enemy_the_nameless_master.png  enemy_the_devil_s_string.png
Size: 200 x 300 in the duel (x1.15 for elites, x1.3 for bosses), standing on y 520, in front
of TACET's black sun. The same picture is squeezed into a small square on the score page.
Draw on a transparent background: the eclipse behind it is an effect and stays.
Code: Screens/DuelScreen.Stage.cs (DrawTacetSide)


5. DUEL BACKGROUND  (one per era)
---------------------------------
Folder:  Content/Art/Stage
    stage_classical.png   stage_siam.png   stage_romantic.png
Size: 1280 x 720, the whole screen. TACET's black covers the right side of it during a
fight, so the important part of the painting should sit on the left. The band stands with
their feet between y 506 and y 554, so paint the floor from about y 452 down.
Code: Core/ArtBank.cs (DrawStage)


6. PANELS : ERAS AND PLACES
---------------------------
Folder:  Content/Art/Eras      era_classical.png   era_siam.png   era_romantic.png
Folder:  Content/Art/Route     route_battle.png  route_elite.png  route_boss.png  route_event.png
                               route_shop.png  route_rest.png  route_crossing.png
Tall panels that stretch to fit the column they are in (around 480 x 720 is a good size).
The bottom 150 pixels sit under the caption, so keep the subject in the upper part.
Code: Core/PanelStrip.cs, Screens/EraChoiceScreen.cs, Screens/RouteScreen.cs


7. PLACES AND EVENTS
--------------------
Folder:  Content/Art/Places    shop.png   672 x 470, the shopkeeper is part of the picture
                               rest.png   960 x 236, a wide strip over the three choices
Folder:  Content/Art/Events    event_<title>.png, 520 x 430, one per event, for example
                               event_a_street_musician.png  (the titles are in Data/GameEvent.cs)


8. CONDUCTOR HAND  (animated, one set per stroke)
-------------------------------------------------
Folder:  Content/Art/Hand
Name:    hand_<pose>_<number>.png, the number starts at 0

    hand_ready_0.png        resting, between beats
    hand_down_0.png         beat 1 of the 4/4 pattern, the baton goes DOWN
    hand_left_0.png         beat 2, the baton goes LEFT
    hand_right_0.png        beat 3, the baton goes RIGHT
    hand_up_0.png           beat 4, the baton goes UP
    hand_signature_0.png    the conductor's signature move

One picture per pose is enough. For an animation, add more numbers in order. They play once,
front to back, then the hand goes back to READY by itself. Up to 8 frames per pose.
Size: the slot is 190 x 190, top left of the duel under the round plate (DuelScreen.handBox).
Draw every pose on the same canvas size so they line up.
Code: Core/HandArt.cs


9. BATON  (the stick the player holds in the duel)
--------------------------------------------------
Folder:  Content/Art/Baton
Name:    baton.png
Draw it STANDING UP, handle at the bottom, tip at the top, on a transparent background.
The game holds it by the bottom middle of the picture (that point sits on the mouse
pointer), stretches it to 118 pixels long and turns it as it swings.
A tall thin canvas such as 24 x 236 works well.
Code: Screens/DuelScreen.Baton.cs (DrawBaton, and UpdateBaton for the swing)


WHAT IS DRAWN IN CODE AND STAYS
-------------------------------
These are effects and interface, not artwork, so they stay when the pictures arrive:
TACET's black field and its glowing rift edge, the black sun (eclipse) behind every enemy, the hollow
rings of TACET's notes, the streaks of light, sparks and floor ripples of a clash, the motif
badges, the instrument family marks, and the diagrams on the HOW TO PLAY pages.
