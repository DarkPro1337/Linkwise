param(
    [ValidateSet("win-x64", "win-arm64")]
    [string] $RuntimeIdentifier = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$output = Join-Path $root "artifacts\windows\$RuntimeIdentifier\Linkwise"

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

dotnet publish `
    (Join-Path $root "src\Linkwise.Desktop\Linkwise.Desktop.csproj") `
    --configuration Release `
    --runtime $RuntimeIdentifier `
    --self-contained true `
    --output $output

Write-Host $output
