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
    --self-contained false `
    -p:DebugSymbols=false `
    -p:DebugType=None `
    --output $output

# Native NuGet assets can ship their own large PDB files even when project
# debug symbols are disabled. They are not required to run the application.
Get-ChildItem $output -Filter "*.pdb" -File | Remove-Item -Force

Write-Host $output
