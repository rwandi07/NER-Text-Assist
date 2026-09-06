param(
    [string]$ProjectDir = (Join-Path (Split-Path -Parent $PSScriptRoot) "src/NER.TextAssist")
)

$ErrorActionPreference = "Stop"
$assetsDir = Join-Path $ProjectDir "Assets"
$pngPath = Join-Path $assetsDir "NER-Text-Assist-Icon.png"
$icoPath = Join-Path $assetsDir "NER-Text-Assist.ico"

if (-not (Test-Path $pngPath)) {
    throw "Branding icon PNG not found: $pngPath"
}

$png = [System.IO.File]::ReadAllBytes($pngPath)
$stream = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($stream)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]1)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]32)
$writer.Write([UInt32]$png.Length)
$writer.Write([UInt32]22)
$writer.Write($png)
$writer.Flush()
[System.IO.File]::WriteAllBytes($icoPath, $stream.ToArray())
$writer.Dispose()
$stream.Dispose()
Write-Host "Branding icon prepared: $icoPath"
