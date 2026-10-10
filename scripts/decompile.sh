#!/usr/bin/env bash
# 把游戏程序集里和计分板有关的类型 dump 成 C#，供人/agent 读。
# 注意：RoR2 的游戏代码拆在 Assembly-CSharp.dll 和 RoR2.dll 两个程序集里，
# 所以每个类型都要两个都试一遍（ilspycmd 找不到类型时返回非 0）。
set -euo pipefail

GAME="${ROR2_GAME:-$HOME/.local/share/Steam/steamapps/common/Risk of Rain 2}"
MANAGED="$GAME/Risk of Rain 2_Data/Managed"
OUT="$(cd "$(dirname "$0")/.." && pwd)/decompile"
mkdir -p "$OUT"

ASMS=(Assembly-CSharp.dll RoR2.dll)
TYPES=(
  RoR2.UI.ScoreboardStrip
  RoR2.UI.ScoreboardController
  RoR2.UI.AllyCardController
  RoR2.UI.AllyCardManager
  RoR2.Util
  RoR2.PlayerCharacterMasterController
)

for t in "${TYPES[@]}"; do
  name="${t##*.}"
  found=""
  for a in "${ASMS[@]}"; do
    [ -f "$MANAGED/$a" ] || continue
    if ilspycmd --type "$t" "$MANAGED/$a" > "$OUT/$name.cs" 2>"$OUT/$name.err"; then
      found="$a"
      rm -f "$OUT/$name.err"
      break
    fi
  done
  if [ -n "$found" ]; then
    echo "==> $t   <- $found"
  else
    echo "!! $t   未找到（见 $name.err）" >&2
  fi
done

echo
wc -l "$OUT"/*.cs
