#!/usr/bin/env bash
# 编译插件。加 --install 就顺便丢进 r2modman profile 的 plugins 目录。
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROFILE="${R2MODMAN_PROFILE:-$HOME/.config/r2modmanPlus-local/RiskOfRain2/profiles/Default}"
PLUGIN_DIR="$PROFILE/BepInEx/plugins/FakeFriendScoreboard"

args=(-c Release)
if [ "${1:-}" = "--install" ]; then
  if [ ! -d "$PLUGIN_DIR" ]; then
    cat >&2 <<MSG
!! $PLUGIN_DIR 不存在。
   插件平时应由 r2modman 管理：先 ./scripts/package.sh，再在 r2modman 里
   Settings -> Import local mod 选 dist/FakeFriendScoreboard-<ver>.zip。
   导入之后这个目录就是 r2modman 装出来的那份，--install 只是覆盖它的 dll。
   想强行装成一份"野"副本（r2modman 不认），先 mkdir -p "$PLUGIN_DIR"。
MSG
    exit 1
  fi
  args+=(-p:InstallDir="$PLUGIN_DIR")
fi

dotnet build "$ROOT/src/FakeFriendScoreboard.csproj" "${args[@]}"
