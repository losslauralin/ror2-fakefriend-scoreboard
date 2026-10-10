#!/usr/bin/env bash
# 编译插件。加 --install 就顺便丢进 r2modman profile 的 plugins 目录。
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROFILE="${R2MODMAN_PROFILE:-$HOME/.config/r2modmanPlus-local/RiskOfRain2/profiles/Default}"
# r2modman 导入本地 mod 时没有 author，用的目录名是 Unknown-<manifest.name>。
PLUGIN_DIR="$PROFILE/BepInEx/plugins/Unknown-FakeFriendScoreboard"

args=(-c Release)
if [ "${1:-}" = "--install" ]; then
  if [ ! -d "$PLUGIN_DIR" ]; then
    echo "FakeFriendScoreboard: $PLUGIN_DIR 不存在 —— 先在 r2modman 里 Settings -> Import local mod 导入 dist/*.zip" >&2
    exit 1
  fi
  args+=(-p:InstallDir="$PLUGIN_DIR")
fi

dotnet build "$ROOT/src/FakeFriendScoreboard.csproj" "${args[@]}"
