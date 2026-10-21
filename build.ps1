param(
    [switch]$SelfContained,
    [string]$OutDir = "publish"
)

$ErrorActionPreference = "Stop"
$dotnet = "dotnet"

Push-Location $PSScriptRoot
try {
    & $dotnet publish .\PinNote\PinNote.csproj `
        -c Release -r win-x64 `
        --self-contained:$SelfContained `
        -p:PublishSingleFile=true `
        -p:DebugType=none `
        -o $OutDir

    $exe = Join-Path $OutDir "PinNote.exe"
    if (Test-Path $exe) {
        $size = [math]::Round((Get-Item $exe).Length / 1KB, 0)
        Write-Host "OK -> $exe ($size KB)" -ForegroundColor Green
    }
}
finally {
    Pop-Location
}
