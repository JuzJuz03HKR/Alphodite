#!/bin/bash
# Cloud (Linux) setup so MonoGame's content builder can compile .fx shaders.
# MonoGame runs "wine dotnet c:\fxccs.dll ..." inside the Wine prefix named by MGFXC_WINE_PATH and
# needs the real d3dcompiler_47.dll. The official script downloads them from Microsoft and Mozilla,
# which the cloud network blocks, so this one takes:
#   - d3dcompiler_47.dll from the cef.redist.x64 package on NuGet (api.nuget.org is allowed)
#   - a small C stand-in for "dotnet c:\fxccs.dll" (fxcshim.c next to this file) built with MinGW
# Usage:  bash .claude/skills/build/shader_setup.sh <work folder>
# Then:   export MGFXC_WINE_PATH=<work folder>/winemonogame   before dotnet build
set -e
WORK="${1:?usage: shader_setup.sh <work folder>}"
HERE="$(cd "$(dirname "$0")" && pwd)"
mkdir -p "$WORK"
export DEBIAN_FRONTEND=noninteractive
if ! command -v wine >/dev/null || ! command -v x86_64-w64-mingw32-gcc >/dev/null; then
  apt-get install -y wine64 gcc-mingw-w64-x86-64 >/dev/null 2>&1 || { apt-get update >/dev/null 2>&1; apt-get install -y wine64 gcc-mingw-w64-x86-64 >/dev/null 2>&1; }
fi
if [ ! -f "$WORK/d3dcompiler_47.dll" ]; then
  mkdir -p "$WORK/nuget_cef"
  curl -s --max-time 900 -o "$WORK/nuget_cef/cef.nupkg" https://api.nuget.org/v3-flatcontainer/cef.redist.x64/120.2.7/cef.redist.x64.120.2.7.nupkg
  unzip -o -j "$WORK/nuget_cef/cef.nupkg" "CEF/d3dcompiler_47.dll" -d "$WORK" >/dev/null
  rm -rf "$WORK/nuget_cef"
fi
x86_64-w64-mingw32-gcc -O2 -static -o "$WORK/dotnet.exe" "$HERE/fxcshim.c"
export WINEARCH=win64 WINEPREFIX="$WORK/winemonogame" WINEDEBUG=-all WINEDLLOVERRIDES="mscoree,mshtml="
[ -d "$WINEPREFIX/drive_c/windows/system32" ] || timeout 300 wine wineboot -i >/dev/null 2>&1
cp "$WORK/d3dcompiler_47.dll" "$WORK/dotnet.exe" "$WINEPREFIX/drive_c/windows/system32/"
echo "stand-in for MonoGame's fxccs.dll, see fxcshim.c" > "$WINEPREFIX/drive_c/fxccs.dll"
echo "ready: export MGFXC_WINE_PATH=$WINEPREFIX"
