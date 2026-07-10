#!/usr/bin/env bash
set -euo pipefail

GAME="${GAME:-$HOME/.steam/debian-installation/steamapps/compatdata/3667336694/pfx/drive_c/Neowiz/Browndust2/Browndust2_10000001}"
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PLUGIN_DIR="$GAME/BepInEx/plugins/BD2VietnameseSkillText"
TRANSLATION_DIR="$GAME/BepInEx/config/BD2Vietnamese"
TRANSLATION_FILE="$TRANSLATION_DIR/SkillTextTable_EN.json"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet SDK is not installed or not in PATH."
    exit 1
fi

required_files=(
    "$GAME/BepInEx/core/BepInEx.dll"
    "$GAME/BepInEx/core/0Harmony.dll"
    "$GAME/BrownDust II_Data/Managed/UnityEngine.dll"
    "$GAME/BrownDust II_Data/Managed/UnityEngine.InputLegacyModule.dll"
    "$GAME/BrownDust II_Data/Managed/UnityEngine.CoreModule.dll"
)

for file in "${required_files[@]}"; do
    if [[ ! -f "$file" ]]; then
        echo "ERROR: Missing required file:"
        echo "$file"
        exit 1
    fi
done

cd "$PROJECT_DIR"
dotnet build -c Release -p:GameDir="$GAME"

DLL="$PROJECT_DIR/bin/Release/netstandard2.1/BD2VietnameseSkillText.dll"

if [[ ! -f "$DLL" ]]; then
    echo "ERROR: Build completed but DLL was not found:"
    echo "$DLL"
    exit 1
fi

mkdir -p "$PLUGIN_DIR" "$TRANSLATION_DIR"
install -m 0644 "$DLL" "$PLUGIN_DIR/BD2VietnameseSkillText.dll"

if [[ ! -f "$TRANSLATION_FILE" ]]; then
    install -m 0644 \
        "$PROJECT_DIR/SkillTextTable_EN.json" \
        "$TRANSLATION_FILE"
    echo "Installed starter translation:"
    echo "$TRANSLATION_FILE"
else
    echo "Kept existing translation file:"
    echo "$TRANSLATION_FILE"
fi

echo
echo "Installed plugin:"
echo "$PLUGIN_DIR/BD2VietnameseSkillText.dll"
echo
echo "Open Brown Dust II. Press F6 in game to reload the JSON after editing."
