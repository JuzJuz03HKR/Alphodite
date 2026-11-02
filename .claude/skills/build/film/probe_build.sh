#!/bin/bash
# a patched copy for filming (probe_patch.py), built apart from the plain build
SP=${SP:?set SP to a scratch folder first}
export MGFXC_WINE_PATH=$SP/shader/winemonogame
cd /home/user/Alphodite
mkdir -p $SP/probe
tar --exclude='Tacetno433/bin' --exclude='Tacetno433/obj' -cf - Tacetno433.sln Tacetno433 | tar -xf - -C $SP/probe
sed -i 's/net9.0/net8.0/' $SP/probe/Tacetno433/Tacetno433.csproj
python3 $SP/probe_patch.py $SP/probe || exit 1
cd $SP/probe && dotnet build -nologo -v q 2>&1 | grep -E " error |rror\(s\)|arning\(s\)" | sort -u | head -20
