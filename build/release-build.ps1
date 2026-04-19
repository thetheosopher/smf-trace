param(
    [string]$Configuration = 'Release',
    [string]$RuntimeIdentifier = 'win-x64',
    [string]$InnoCompilerPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'release-common.ps1')

$repoRoot = Get-SMFTraceRepositoryRoot -ScriptRoot $PSScriptRoot
$projectPath = Get-SMFTraceProjectPath -RepositoryRoot $repoRoot
$versionInfo = Get-SMFTraceVersionInfo -RepositoryRoot $repoRoot
$layout = Get-SMFTraceReleaseLayout -RepositoryRoot $repoRoot -Configuration $Configuration -RuntimeIdentifier $RuntimeIdentifier
$installerScriptPath = Join-Path $repoRoot 'src\SMFTrace.Installer\SMFTrace.iss'
$resolvedInnoCompilerPath = Resolve-InnoSetupCompilerPath -RequestedPath $InnoCompilerPath

if (-not (Test-Path $installerScriptPath -PathType Leaf)) {
    throw "Inno Setup script was not found at '$installerScriptPath'."
}

Remove-PathIfPresent -Path $layout.InstallerPath

try {
    Invoke-SMFTracePublish -ProjectPath $projectPath -Configuration $Configuration -RuntimeIdentifier $RuntimeIdentifier -PublishRoot $layout.PublishRoot | Out-Null
    New-SMFTracePortablePackage -PublishRoot $layout.PublishRoot -ZipPath $layout.PortableZipPath

    $innoArguments = @(
        $installerScriptPath
        "/DAppVersion=$($versionInfo.AppVersion)"
        "/DFileVersion=$($versionInfo.FileVersion)"
        "/DPublishDir=$($layout.PublishRoot)"
        "/DOutputDir=$($layout.ArtifactRoot)"
        "/DOutputBaseFilename=$([System.IO.Path]::GetFileNameWithoutExtension($layout.InstallerPath))"
    )

    & $resolvedInnoCompilerPath @innoArguments

    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compilation failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-PathIfPresent -Path $layout.PublishRoot
}

Write-Host 'Release artifacts created:'
Write-Host " - Portable zip: $($layout.PortableZipPath)"
Write-Host " - Installer: $($layout.InstallerPath)"
