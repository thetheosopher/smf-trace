param(
    [string]$Configuration = 'Release',
    [string]$RuntimeIdentifier = 'win-x64'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'release-common.ps1')

$repoRoot = Get-SMFTraceRepositoryRoot -ScriptRoot $PSScriptRoot
$projectPath = Get-SMFTraceProjectPath -RepositoryRoot $repoRoot
$layout = Get-SMFTraceReleaseLayout -RepositoryRoot $repoRoot -Configuration $Configuration -RuntimeIdentifier $RuntimeIdentifier

try {
    Invoke-SMFTracePublish -ProjectPath $projectPath -Configuration $Configuration -RuntimeIdentifier $RuntimeIdentifier -PublishRoot $layout.PublishRoot | Out-Null
    New-SMFTracePortablePackage -PublishRoot $layout.PublishRoot -ZipPath $layout.PortableZipPath
}
finally {
    Remove-PathIfPresent -Path $layout.PublishRoot
}
