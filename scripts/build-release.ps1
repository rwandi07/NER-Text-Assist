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

$isccCandidates = @(
    "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
)

$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($iscc) {
    Write-Host "Building installer..."
    & $iscc $installerScript
    Write-Host "Installer created under installer/output/."
} else {
    Write-Warning "Inno Setup 6 was not found. Publish output is ready, but installer was not compiled."
}
