#!/usr/bin/env bash
set -euo pipefail

GAME="${GAME:-$HOME/.steam/debian-installation/steamapps/compatdata/3667336694/pfx/drive_c/Neowiz/Browndust2/Browndust2_10000001}"
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PLUGIN_DIR="$GAME/BepInEx/plugins/BD2VietnameseFontFix"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet SDK is not installed or not in PATH."
    exit 1
fi

required_files=(
    "$GAME/BepInEx/core/BepInEx.dll"
    "$GAME/BrownDust II_Data/Managed/UnityEngine.dll"
    "$GAME/BrownDust II_Data/Managed/UnityEngine.CoreModule.dll"
    "$GAME/BrownDust II_Data/Managed/UnityEngine.TextRenderingModule.dll"
    "$GAME/BrownDust II_Data/Managed/UnityEngine.InputLegacyModule.dll"
    "$GAME/BrownDust II_Data/Managed/UnityEngine.UI.dll"
    "$GAME/BrownDust II_Data/Managed/Unity.TextMeshPro.dll"
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

DLL="$PROJECT_DIR/bin/Release/netstandard2.1/BD2VietnameseFontFix.dll"

if [[ ! -f "$DLL" ]]; then
    echo "ERROR: Build completed but DLL was not found:"
    echo "$DLL"
    exit 1
fi

mkdir -p "$PLUGIN_DIR"
install -m 0644 "$DLL" "$PLUGIN_DIR/BD2VietnameseFontFix.dll"

echo
echo "Installed plugin:"
echo "$PLUGIN_DIR/BD2VietnameseFontFix.dll"
echo
echo "Open Brown Dust II, open a Vietnamese skill screen, then press F7."
