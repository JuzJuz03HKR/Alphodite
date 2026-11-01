ART FOLDER  (Beta1, updated 1 Nov 2026)
=======================================

The full list of every picture the game uses is docs/ASSET_LIST.md : the file names, the canvas
sizes, where each picture shows, which are still missing, and the order to draw them in.
The team ticks its work in docs/TACET_Asset_List.xlsx. How to draw them : docs/BATTLE_ART_PLAN.md
(the battle page) and docs/UI_ART_PLAN.md (every other page).

What the team draws (the user, 1 Nov) : the 3 conductors, the 5 legends, the 6 bosses, 3 or 4 enemy
looks shared by every enemy, and the logo. Everything else is not drawn : the pages are made to need
no picture (docs/ASSET_LIST.md, the plan column).

Nothing in this folder is needed for the game to run. A missing picture is drawn as an EMPTY FRAME
(a faint box with four corner marks) at the exact place and size the picture will have.


WHERE THINGS GO
---------------
    Battle/Musicians   <name>          the five legends from behind, 160 x 256, animated     no Content.mgcb entry
    Battle/Enemies     <name>          112 x 128, elites 144 x 160, bosses 224 x 224, animated  no entry
    Battle/Stages      <movement>_far  _mid  _stage  _front, 704 x 360                       no entry
    Battle/Bosses      <boss>_cutin    180 x 200                                              no entry
    Ui/Busts           bust_<name>     160 x 256                                              no entry
    Ui/Faces           face_<name>     112 x 112                                              no entry
    Ui/Icons           skill_  status_  intent_  keyword_ icons, 22 to 26 pixels             no entry
    Conductors         gallery_ 302 x 402, face_ 224 x 224, cutin_ 180 x 190                  needs a Content.mgcb entry
    Places             shop 672 x 470, rest 960 x 236, luthier 520 x 470                      needs an entry
    Events             event_<title>   520 x 430                                              needs an entry
    Route              route_<room>    230 x 142                                              needs an entry
    Baton              baton           118 pixels tall                                        needs an entry

Battle and Ui pictures are read straight from their folders (Core/FrameArt.cs, Core/UiArt.cs) :
drop the file in and Rebuild. The others are built through Content.mgcb (Core/ArtBank.cs) : open
Content.mgcb in the MGCB Editor, Add Existing Item, then Rebuild.

The battle art is pixel art shown at x2 (think of a 640 x 360 screen). Every other page shows its
pixel art at its real size on the 1280 x 720 screen. Names are lower case, and spaces and every sign
become _   ("THE DEVIL'S STRING" -> the_devil_s_string).

A battle character comes either as an Aseprite sheet (<name>.png + <name>.json, one Tag per
animation) or as one file per frame (<name>_<animation>_<number>.png, numbers from 0).

SHARED LOOKS (1 Nov) : an enemy with no files of its own wears its look : hush, static, the_damper or
the_mute_choir (docs/ASSET_LIST.md 1.2, the table in the code is Data/Enemy.cs). The code tells the
wearers apart with brightness and a small mark of its own, so draw each look once.
A move with no picture yet is stood in for : skill and parry use attack, hurt shivers, a knock out
sinks and fades, an enemy's attack rushes at the band, and a set with no idle stands on the first
frame of its attack. Draw idle and attack first, the rest when there is time.

F3 while playing counts the pictures built through Content.mgcb (ART). The Battle and Ui folders
are not counted there yet.


NOT USED ANY MORE  (the old duel of Alpha6)
-------------------------------------------
Musicians/<name>_idle_0.png (the nine chibi files still in this folder), stand_ portrait_ face_ in
Musicians, Enemies/enemy_<name>, Stage/, Hand/, Eras/, route_crossing, the conductors' portrait_.
