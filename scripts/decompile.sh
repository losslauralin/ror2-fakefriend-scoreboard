#!/usr/bin/env bash
# 把游戏程序集里和计分板有关的类型 dump 成 C#，供人/agent 读。
set -euo pipefail

GAME="${ROR2_GAME:-$HOME/.local/share/Steam/steamapps/common/Risk of Rain 2}"
MANAGED="$GAME/Risk of Rain 2_Data/Managed"
OUT="$(cd "$(dirname "$0")/.." && pwd)/decompile"
mkdir -p "$OUT"

if [ ! -f "$MANAGED/Assembly-CSharp.dll" ]; then
  echo "找不到游戏程序集: $MANAGED" >&2
  exit 1
fi

for t in RoR2.UI.ScoreboardStrip RoR2.UI.ScoreboardController RoR2.UI.AllyCardController RoR2.UI.AllyCardManager RoR2.Util; do
  name="${t##*.}"
  echo "==> $t"
  ilspycmd --type "$t" "$MANAGED/Assembly-CSharp.dll" > "$OUT/$name.cs"
done

wc -l "$OUT"/*.cs
