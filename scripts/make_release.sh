#!/usr/bin/env bash
set -euo pipefail

GAME="${GAME:-$HOME/.steam/debian-installation/steamapps/compatdata/3667336694/pfx/drive_c/Neowiz/Browndust2/Browndust2_10000001}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${1:-dev}"

OUT="$ROOT/release/BD2VietnameseSkillText-$VERSION"
ZIP="$ROOT/release/BD2VietnameseSkillText-$VERSION.zip"

DLL="$GAME/BepInEx/plugins/BD2VietnameseSkillText/BD2VietnameseSkillText.dll"
JSON="$ROOT/translations/SkillTextTable_EN.json"

rm -rf "$OUT"
mkdir -p \
  "$OUT/BepInEx/plugins/BD2VietnameseSkillText" \
  "$OUT/BepInEx/config/BD2Vietnamese"

[[ -f "$DLL" ]] || {
    echo "Không thấy DLL đã build: $DLL"
    exit 1
}

python3 -m json.tool "$JSON" >/dev/null

cp -a "$DLL" \
  "$OUT/BepInEx/plugins/BD2VietnameseSkillText/"

cp -a "$JSON" \
  "$OUT/BepInEx/config/BD2Vietnamese/SkillTextTable_EN.json"

cp -a "$ROOT/README.md" "$OUT/"

mkdir -p "$ROOT/release"
rm -f "$ZIP"
cd "$ROOT/release"
zip -r "$(basename "$ZIP")" "$(basename "$OUT")"
sha256sum "$(basename "$ZIP")" > "$(basename "$ZIP").sha256"

echo "Đã tạo:"
echo "$ZIP"
echo "$ZIP.sha256"
