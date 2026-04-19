param(
    [string]$Configuration = 'Release',
    [string]$RuntimeIdentifier = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$projectPath = Join-Path $repoRoot 'src\SMFTrace.Wpf\SMFTrace.Wpf.csproj'
$packageRoot = Join-Path $repoRoot "output\portable\SMFTrace.Wpf\$Configuration\$RuntimeIdentifier"
$publishRoot = Join-Path $packageRoot 'publish'
$zipPath = Join-Path $packageRoot "SMFTrace-portable-$RuntimeIdentifier.zip"

if (Test-Path $publishRoot) {
    Remove-Item $publishRoot -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null

dotnet publish $projectPath -c $Configuration -r $RuntimeIdentifier --self-contained true -o $publishRoot

Compress-Archive -Path (Join-Path $publishRoot '*') -DestinationPath $zipPath -Force
Remove-Item $publishRoot -Recurse -Force

Write-Host "Portable package created at $zipPath"
