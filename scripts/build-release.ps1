param(
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/NER.TextAssist/NER.TextAssist.csproj"
$publishDir = Join-Path $repoRoot "artifacts/publish/$Runtime"
$installerScript = Join-Path $repoRoot "installer/NER-Text-Assist.iss"

Write-Host "Publishing NER Text Assist for $Runtime..."
dotnet restore $project
dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

$programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
$programFiles = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
$commandIscc = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue

$isccCandidates = @()
if ($commandIscc) {
    $isccCandidates += $commandIscc.Source
}
$isccCandidates += @(
    (Join-Path $programFilesX86 "Inno Setup 6\ISCC.exe"),
    (Join-Path $programFiles "Inno Setup 6\ISCC.exe")
)

$iscc = $isccCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if ($iscc) {
    Write-Host "Building installer with $iscc..."
    & $iscc $installerScript
    Write-Host "Installer created under installer/output/."
} else {
    throw "Inno Setup 6 compiler (ISCC.exe) was not found. Install Inno Setup 6 before building the installer."
}
