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

$publishArguments = @(
    'publish'
    $projectPath
    '-c'
    $Configuration
    '-r'
    $RuntimeIdentifier
    '--self-contained'
    'true'
    '-p:PublishSingleFile=true'
    '-p:IncludeNativeLibrariesForSelfExtract=true'
    '-p:EnableCompressionInSingleFile=true'
    '-p:DebugSymbols=false'
    '-p:DebugType=None'
    '-o'
    $publishRoot
)

& dotnet @publishArguments

$nonWindowsArtifacts = @()

if ($RuntimeIdentifier -like 'win-*') {
    $nonWindowsArtifacts = Get-ChildItem -Path $publishRoot -File | Where-Object {
        $_.Extension -in @('.dylib', '.so')
    }

    foreach ($artifact in $nonWindowsArtifacts) {
        Remove-Item $artifact.FullName -Force
    }
}

$publishedFiles = Get-ChildItem -Path $publishRoot -File | Sort-Object Name
$publishedFileList = $publishedFiles.Name -join ', '

if ($nonWindowsArtifacts.Count -gt 0) {
    $removedArtifacts = $nonWindowsArtifacts.Name -join ', '
    Write-Host "Removed non-Windows publish artifacts: $removedArtifacts"
}

Write-Host "Published files: $publishedFileList"

Compress-Archive -Path (Join-Path $publishRoot '*') -DestinationPath $zipPath -Force
Remove-Item $publishRoot -Recurse -Force

Write-Host "Portable package created at $zipPath"
