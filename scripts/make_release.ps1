param(
    [string]$Version = "dev",
    [string]$GameDir = $env:BD2_GAME_DIR
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ReleaseRoot = Join-Path $RepoRoot "release"
$PackageName = "BD2VietnameseSkillText-$Version-Windows"
$PackageDir = Join-Path $ReleaseRoot $PackageName
$ZipPath = Join-Path $ReleaseRoot "$PackageName.zip"

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $CommonPath = "C:\Neowiz\Browndust2\Browndust2_10000001"

    if (Test-Path $CommonPath) {
        $GameDir = $CommonPath
    }
    else {
        $GameDir = Read-Host "Nhập thư mục chứa Brown Dust II.exe"
    }
}

$Dll = Join-Path `
    $GameDir `
    "BepInEx\plugins\BD2VietnameseSkillText\BD2VietnameseSkillText.dll"

$Json = Join-Path `
    $RepoRoot `
    "translations\SkillTextTable_EN.json"

if (-not (Test-Path $Dll)) {
    throw "Không thấy DLL đã build: $Dll"
}

if (-not (Test-Path $Json)) {
    throw "Không thấy file dịch: $Json"
}

Get-Content $Json -Raw | ConvertFrom-Json | Out-Null

Remove-Item $PackageDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $ZipPath -Force -ErrorAction SilentlyContinue

$PluginDir = Join-Path `
    $PackageDir `
    "BepInEx\plugins\BD2VietnameseSkillText"

$ConfigDir = Join-Path `
    $PackageDir `
    "BepInEx\config\BD2Vietnamese"

New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
New-Item -ItemType Directory -Force -Path $ConfigDir | Out-Null

Copy-Item $Dll (Join-Path $PluginDir "BD2VietnameseSkillText.dll")
Copy-Item $Json (Join-Path $ConfigDir "SkillTextTable_EN.json")
Copy-Item (Join-Path $RepoRoot "README_WINDOWS.md") $PackageDir

Compress-Archive `
    -Path $PackageDir `
    -DestinationPath $ZipPath `
    -Force

$Hash = Get-FileHash $ZipPath -Algorithm SHA256
$HashLine = "$($Hash.Hash.ToLower())  $([System.IO.Path]::GetFileName($ZipPath))"
Set-Content `
    -Path "$ZipPath.sha256" `
    -Value $HashLine `
    -Encoding ascii

Write-Host "Đã tạo:"
Write-Host $ZipPath
Write-Host "$ZipPath.sha256"
