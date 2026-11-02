# TACET 4'33

A short roguelike where you are the conductor. Your mouse is the baton.
Lead a band of three legendary musicians down into the silence and break the TACET.

Team **Alphodite** · Final Project · MonoGame DesktopGL 3.8 · .NET 9 · Windows

## Play (Windows)

1. Download `TACET433_v1.0.0_Windows.zip` from **Releases** (tag `v1.0.0`).
2. Unzip anywhere.
3. Double click `Tacetno433.exe`. No install needed.

## Controls

| Action | Input |
|---|---|
| Pick, point, press buttons | Mouse |
| Swing the baton (skills, parry, FINALE) | Hold the left button and flick toward the arrow in the ring |
| Hold note | Keep the button held until the ring closes |
| Play the turn | `ENTER` |
| Choose quickly | Number keys `1` `2` `3` |
| Pause / back | `ESC` |
| Full screen | SETTINGS page |

## How the game goes

1. Pick a conductor, then pick 3 of 5 musicians.
2. Walk the map, one room at a time: fights, elites, shops, rests, events, workshops.
3. In a fight, give each musician one move, then press ENTER. Skills ask for a baton swing. Good swings hit harder.
4. When TACET attacks, flick on time to parry.
5. Win fights for stars and gifts. Three floors, a boss at the end of each.

The first run starts with a short practice fight that teaches all of this.

## Build from source

1. Install Visual Studio 2026 with **.NET desktop development** and the .NET 9 SDK.
2. Open `Tacetno433.sln`.
3. Press `F5` (Debug) or pick **Release** and build. Expect 0 errors, 0 warnings.
4. Fonts are open fonts inside `Tacetno433/Content/Fonts/`, so it builds the same on every machine.

To make the zip yourself: right click the project, **Publish**, folder, `win-x64`, self contained.

## Credits

Every sound, font and outside file is listed in [`docs/CREDITS.md`](docs/CREDITS.md) and on the CREDITS page in the game.
Characters, stages and effects are drawn by code (math art). Conductor art by the team.
