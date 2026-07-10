#!/usr/bin/env bash
set -euo pipefail

GAME="${GAME:-$HOME/.steam/debian-installation/steamapps/compatdata/3667336694/pfx/drive_c/Neowiz/Browndust2/Browndust2_10000001}"
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PLUGIN_DIR="$GAME/BepInEx/plugins/BD2SkillTextExporter"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet SDK is not installed or not in PATH."
    echo "Run: dotnet --version"
    exit 1
fi

for required in \
    "$GAME/BrownDust II.exe" \
    "$GAME/BepInEx/core/BepInEx.dll" \
    "$GAME/BepInEx/core/0Harmony.dll" \
    "$GAME/BrownDust II_Data/Managed/UnityEngine.dll" \
    "$GAME/BrownDust II_Data/Managed/UnityEngine.CoreModule.dll"
do
    if [[ ! -e "$required" ]]; then
        echo "ERROR: missing required file:"
        echo "$required"
        exit 1
    fi
done

dotnet build "$PROJECT_DIR/BD2SkillTextExporter.csproj" \
    -c Release \
    -p:GameDir="$GAME"

DLL="$PROJECT_DIR/bin/Release/netstandard2.1/BD2SkillTextExporter.dll"

if [[ ! -f "$DLL" ]]; then
    echo "ERROR: build completed but DLL was not found:"
    echo "$DLL"
    exit 1
fi

mkdir -p "$PLUGIN_DIR"
cp -av "$DLL" "$PLUGIN_DIR/BD2SkillTextExporter.dll"

echo
echo "Installed:"
echo "$PLUGIN_DIR/BD2SkillTextExporter.dll"
echo
echo "Usage in game:"
echo "  F9  = start/stop capture"
echo "  F10 = export"
echo "  F11 = clear current capture"
echo
echo "Exports:"
echo "$GAME/BepInEx/exports/BD2SkillText"
