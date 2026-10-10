#!/usr/bin/env bash
# 编译并打成 Thunderstore 格式的 zip，供 r2modman "Import local mod" 使用。
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
NAME="$(python3 -c 'import json,sys;print(json.load(open(sys.argv[1]))["name"])' "$ROOT/Thunderstore/manifest.json")"
VERSION="$(python3 -c 'import json,sys;print(json.load(open(sys.argv[1]))["version_number"])' "$ROOT/Thunderstore/manifest.json")"
DLL="$ROOT/src/bin/Release/netstandard2.1/FakeFriendScoreboard.dll"
STAGE="$ROOT/dist/$NAME"
ZIP="$ROOT/dist/$NAME-$VERSION.zip"

# 版本号必须两处一致：Plugin.PluginVersion 与 manifest.json(version_number)
PLUGIN_VERSION="$(grep -oP 'PluginVersion = "\K[0-9.]+' "$ROOT/src/Plugin.cs")"
if [ "$PLUGIN_VERSION" != "$VERSION" ]; then
  echo "版本不一致：Plugin.cs=$PLUGIN_VERSION manifest.json=$VERSION（两处都要改）" >&2
  exit 1
fi

dotnet build "$ROOT/src/FakeFriendScoreboard.csproj" -c Release

rm -rf "$STAGE"
mkdir -p "$STAGE"
cp "$ROOT/Thunderstore/manifest.json" "$ROOT/Thunderstore/icon.png" "$STAGE/"
cp "$ROOT/Thunderstore/README.md" "$ROOT/Thunderstore/CHANGELOG.md" "$STAGE/"
cp "$DLL" "$STAGE/"

rm -f "$ZIP"
python3 - "$STAGE" "$ZIP" <<'PY'
import os, sys, zipfile
stage, out = sys.argv[1], sys.argv[2]
with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
    for name in sorted(os.listdir(stage)):
        z.write(os.path.join(stage, name), name)
print("wrote", out)
PY
