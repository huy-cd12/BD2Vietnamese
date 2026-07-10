param(
    [string]$GameDir = $env:BD2_GAME_DIR
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $CommonPath = "C:\Neowiz\Browndust2\Browndust2_10000001"

    if (Test-Path $CommonPath) {
        $GameDir = $CommonPath
    }
    else {
        $GameDir = Read-Host "Nhập thư mục chứa Brown Dust II.exe"
    }
}

$GameExe = Join-Path $GameDir "Brown Dust II.exe"

if (-not (Test-Path $GameExe)) {
    throw "Không thấy Brown Dust II.exe tại: $GameDir"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "Không tìm thấy .NET SDK trong PATH."
}

$RequiredFiles = @(
        (Join-Path $GameDir "BepInEx\core\BepInEx.dll"),
        (Join-Path $GameDir "Brown Dust II_Data\Managed\UnityEngine.dll"),
        (Join-Path $GameDir "Brown Dust II_Data\Managed\UnityEngine.CoreModule.dll"),
        (Join-Path $GameDir "BepInEx\core\0Harmony.dll"),
        (Join-Path $GameDir "Brown Dust II_Data\Managed\UnityEngine.InputLegacyModule.dll")
)

foreach ($File in $RequiredFiles) {
    if (-not (Test-Path $File)) {
        throw "Thiếu file tham chiếu: $File"
    }
}

Push-Location $PSScriptRoot

try {
    dotnet build `
        "BD2VietnameseSkillText.csproj" `
        -c Release `
        "-p:GameDir=$GameDir"

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build thất bại."
    }

    $Dll = Join-Path `
        $PSScriptRoot `
        "bin\Release\netstandard2.1\BD2VietnameseSkillText.dll"

    if (-not (Test-Path $Dll)) {
        throw "Build xong nhưng không thấy DLL: $Dll"
    }

    $PluginDir = Join-Path `
        $GameDir `
        "BepInEx\plugins\BD2VietnameseSkillText"

    New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null

    Copy-Item `
        $Dll `
        (Join-Path $PluginDir "BD2VietnameseSkillText.dll") `
        -Force

    Write-Host ""
    Write-Host "Installed plugin:"
    Write-Host (Join-Path $PluginDir "BD2VietnameseSkillText.dll")

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$TranslationSource = Join-Path $RepoRoot "translations\SkillTextTable_EN.json"
$TranslationDir = Join-Path $GameDir "BepInEx\config\BD2Vietnamese"

if (Test-Path $TranslationSource) {
    New-Item -ItemType Directory -Force -Path $TranslationDir | Out-Null
    Copy-Item `
        $TranslationSource `
        (Join-Path $TranslationDir "SkillTextTable_EN.json") `
        -Force

    Write-Host "Installed translation:"
    Write-Host (Join-Path $TranslationDir "SkillTextTable_EN.json")
}

}
finally {
    Pop-Location
}
