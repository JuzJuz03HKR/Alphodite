#!/bin/bash
# film.sh <page> <probe> <from> <to> <step> <outdir> [extra args] : one page captured every <step> frames (1 frame = 1/60 s)
SP=${SP:?set SP to a scratch folder first}
page=$1; probe=$2; from=$3; to=$4; step=$5; out=$6; shift 6
rm -rf $out; mkdir -p $out
list=""; for ((f=$from; f<=$to; f+=$step)); do list="$list,$page@$f"; done; list=${list#,}
cd $SP/probe/Tacetno433/bin/Debug/net8.0
TACET_PROBE=$probe TACET_AUTO=1 timeout 900 xvfb-run -a -s "-screen 0 1280x720x24" env LIBGL_ALWAYS_SOFTWARE=1 SDL_AUDIODRIVER=dummy dotnet Tacetno433.dll --shots $list --out $out --steady "$@" 2>&1 | grep -E "PROBE|rror|xception" | head -20
ls $out | wc -l
