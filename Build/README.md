# Build

`TACET4'33.zip` : the game for Windows 64-bit. Unzip, open the folder `TACET4'33`, run `TACET4'33.exe`.
No .NET install is needed (self-contained).

Built on 29 Sep 2026 in the cloud chat (Linux) from branch `Alpha6` with
`dotnet publish -c Release -r win-x64 --self-contained true` on .NET 8, with stand-in fonts
(Liberation Serif for Palatino, DejaVu Sans for Bahnschrift). Not yet run on Windows.
A build made on Windows in Visual Studio uses the real fonts and .NET 9.
