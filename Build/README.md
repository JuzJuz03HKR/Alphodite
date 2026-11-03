# Build

`TACET4'33.zip` : the game for Windows 64-bit. Unzip, open the folder `TACET4'33`, run `Tacetno433.exe`.
No .NET install is needed (self-contained).

Built on 3 Nov 2026 in the cloud chat (Linux) from branch `Beta1` (balance pass, NORMAL / HARD, FLOOR GLARE, STRIKE, BOSS RIM, SHATTER)
with `dotnet publish -c Release -r win-x64 --self-contained true` on .NET 8 (the game's own OFL fonts, so it looks the
same as a Windows build). Release : no TEST ROOM, no capture tools. The TEAM FREE sounds from the Drive are not inside
(they stay out of the repo) : put them in `TACET4'33/Content/Audio/Borrowed/` to hear them. Not yet run on Windows.
A build made on Windows in Visual Studio uses .NET 9.
The Alpha6 rhythm duel build (29 Sep) is on the `Release` branch.
