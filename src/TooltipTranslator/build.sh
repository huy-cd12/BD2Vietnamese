#!/usr/bin/env bash
set -euo pipefail

GAME="${GAME:-$HOME/.steam/debian-installation/steamapps/compatdata/3667336694/pfx/drive_c/Neowiz/Browndust2/Browndust2_10000001}"
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet SDK is not installed or not in PATH."
    exit 1
fi

if [[ ! -d "$GAME" ]]; then
    echo "ERROR: game folder does not exist:"
    echo "$GAME"
    exit 1
fi

find_one() {
    local filename="$1"
    local result=""

    result="$(
        find "$GAME" \
            -type f \
            -iname "$filename" \
            -print -quit 2>/dev/null ||
        true
    )"

    if [[ -z "$result" ]]; then
        echo "ERROR: could not find $filename under:"
        echo "$GAME"
        exit 1
    fi

    printf '%s' "$result"
}

BEPINEX_DLL="$(find_one "BepInEx.dll")"
UNITY_ENGINE_DLL="$(find_one "UnityEngine.dll")"
CORE_MODULE_DLL="$(find_one "UnityEngine.CoreModule.dll")"
INPUT_LEGACY_DLL="$(find_one "UnityEngine.InputLegacyModule.dll")"
UI_DLL="$(find_one "UnityEngine.UI.dll")"
TMP_DLL="$(find_one "Unity.TextMeshPro.dll")"
NEWTONSOFT_DLL="$(find_one "Newtonsoft.Json.dll")"

dotnet build \
    "$PROJECT_DIR/BD2VietnameseTooltip.csproj" \
    -c Release \
    -p:BepInExPath="$BEPINEX_DLL" \
    -p:UnityEnginePath="$UNITY_ENGINE_DLL" \
    -p:CoreModulePath="$CORE_MODULE_DLL" \
    -p:InputLegacyPath="$INPUT_LEGACY_DLL" \
    -p:UIPath="$UI_DLL" \
    -p:TMPPath="$TMP_DLL" \
    -p:NewtonsoftJsonPath="$NEWTONSOFT_DLL"

DLL="$PROJECT_DIR/bin/Release/netstandard2.1/BD2VietnameseTooltip.dll"
PLUGIN_DIR="$GAME/BepInEx/plugins/BD2VietnameseTooltip"

[[ -f "$DLL" ]] || {
    echo "ERROR: build completed but DLL was not found:"
    echo "$DLL"
    exit 1
}

mkdir -p "$PLUGIN_DIR"
install -m 0644 \
    "$DLL" \
    "$PLUGIN_DIR/BD2VietnameseTooltip.dll"

echo "Installed tooltip plugin:"
echo "$PLUGIN_DIR/BD2VietnameseTooltip.dll"
