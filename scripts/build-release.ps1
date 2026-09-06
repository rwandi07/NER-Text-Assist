param(
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/NER.TextAssist/NER.TextAssist.csproj"
$publishDir = Join-Path $repoRoot "artifacts/publish/$Runtime"
$installerScript = Join-Path $repoRoot "installer/NER-Text-Assist.iss"
$brandingScript = Join-Path $repoRoot "scripts/prepare-branding.ps1"

Write-Host "Preparing NER Text Assist branding..."
& $brandingScript
if ($LASTEXITCODE -ne 0) { throw "Branding preparation failed with exit code $LASTEXITCODE." }

Write-Host "Publishing NER Text Assist for $Runtime..."
dotnet restore $project
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }

dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

$programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
$programFiles = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
$isccCandidates = @(
    (Join-Path $programFilesX86 "Inno Setup 6\ISCC.exe"),
    (Join-Path $programFiles "Inno Setup 6\ISCC.exe"),
    "C:\ProgramData\chocolatey\bin\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) }

$iscc = $isccCandidates | Select-Object -First 1
if ($iscc) {
    Write-Host "Building installer..."
    & $iscc $installerScript
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE." }
    Write-Host "Installer created under installer/output/."
} else {
    Write-Warning "Inno Setup 6 was not found. Publish output is ready, but installer was not compiled."
}
