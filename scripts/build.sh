#!/usr/bin/env bash
# 编译插件。加 --install 就顺便丢进 r2modman profile 的 plugins 目录。
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROFILE="${R2MODMAN_PROFILE:-$HOME/.config/r2modmanPlus-local/RiskOfRain2/profiles/Default}"
PLUGIN_DIR="$PROFILE/BepInEx/plugins/FakeFriendScoreboard"

args=(-c Release)
if [ "${1:-}" = "--install" ]; then
  args+=(-p:InstallDir="$PLUGIN_DIR")
fi

dotnet build "$ROOT/src/FakeFriendScoreboard.csproj" "${args[@]}"
