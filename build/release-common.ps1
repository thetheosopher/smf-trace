Set-StrictMode -Version Latest

function Get-SMFTraceRepositoryRoot {
    param(
        [Parameter(Mandatory)]
        [string]$ScriptRoot
    )

    return (Resolve-Path (Join-Path $ScriptRoot '..')).Path
}

function Get-SMFTraceProjectPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryRoot
    )

    return Join-Path $RepositoryRoot 'src\SMFTrace.Wpf\SMFTrace.Wpf.csproj'
}

function Get-SMFTraceVersionInfo {
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryRoot
    )

    $propsPath = Join-Path $RepositoryRoot 'Directory.Build.props'
    [xml]$props = Get-Content -Path $propsPath -Raw
    $propertyGroups = @($props.Project.PropertyGroup)
    $displayVersion = ($propertyGroups | Where-Object { $_.Version } | Select-Object -First 1).Version
    $fileVersion = ($propertyGroups | Where-Object { $_.FileVersion } | Select-Object -First 1).FileVersion

    if ([string]::IsNullOrWhiteSpace($displayVersion) -or [string]::IsNullOrWhiteSpace($fileVersion)) {
        throw "Unable to resolve release version information from $propsPath."
    }

    return [pscustomobject]@{
        AppVersion = $displayVersion.Trim()
        FileVersion = $fileVersion.Trim()
    }
}

function Get-SMFTraceReleaseLayout {
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryRoot,

        [Parameter(Mandatory)]
        [string]$Configuration,

        [Parameter(Mandatory)]
        [string]$RuntimeIdentifier
    )

    $artifactRoot = Join-Path $RepositoryRoot "output\release\SMFTrace.Wpf\$Configuration\$RuntimeIdentifier"

    return [pscustomobject]@{
        ArtifactRoot = $artifactRoot
        PublishRoot = Join-Path $artifactRoot 'publish'
        PortableZipPath = Join-Path $artifactRoot "SMFTrace-portable-$RuntimeIdentifier.zip"
        InstallerPath = Join-Path $artifactRoot "SMFTrace-setup-$RuntimeIdentifier.exe"
    }
}

function Remove-PathIfPresent {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (Test-Path $Path) {
        Remove-Item $Path -Recurse -Force
    }
}

function Remove-NonWindowsPublishArtifacts {
    param(
        [Parameter(Mandatory)]
        [string]$PublishRoot
    )

    $nonWindowsArtifacts = @(
        Get-ChildItem -Path $PublishRoot -Recurse -File | Where-Object {
            $_.Extension -in @('.dylib', '.so')
        }
    )

    foreach ($artifact in $nonWindowsArtifacts) {
        Remove-Item $artifact.FullName -Force
    }

    if ($nonWindowsArtifacts.Count -gt 0) {
        Write-Host ("Removed non-Windows publish artifacts: {0}" -f ($nonWindowsArtifacts.Name -join ', '))
    }
}

function Invoke-SMFTracePublish {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectPath,

        [Parameter(Mandatory)]
        [string]$Configuration,

        [Parameter(Mandatory)]
        [string]$RuntimeIdentifier,

        [Parameter(Mandatory)]
        [string]$PublishRoot
    )

    if ($RuntimeIdentifier -notlike 'win-*') {
        throw "SMF Trace publishes are only supported for Windows runtime identifiers. Received '$RuntimeIdentifier'."
    }

    Remove-PathIfPresent -Path $PublishRoot
    New-Item -ItemType Directory -Force -Path $PublishRoot | Out-Null

    $publishArguments = @(
        'publish'
        $ProjectPath
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
        $PublishRoot
    )

    & dotnet @publishArguments

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    Remove-NonWindowsPublishArtifacts -PublishRoot $PublishRoot

    $publishedFiles = @(Get-ChildItem -Path $PublishRoot -Recurse -File | Sort-Object FullName)
    if ($publishedFiles.Count -eq 0) {
        throw "dotnet publish completed without producing any files in $PublishRoot."
    }

    Write-Host ("Published files: {0}" -f ($publishedFiles.Name -join ', '))

    return $publishedFiles
}

function New-SMFTracePortablePackage {
    param(
        [Parameter(Mandatory)]
        [string]$PublishRoot,

        [Parameter(Mandatory)]
        [string]$ZipPath
    )

    Remove-PathIfPresent -Path $ZipPath
    Compress-Archive -Path (Join-Path $PublishRoot '*') -DestinationPath $ZipPath -Force
    Write-Host "Portable package created at $ZipPath"
}

function Resolve-InnoSetupCompilerPath {
    param(
        [string]$RequestedPath
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        if (-not (Test-Path $RequestedPath -PathType Leaf)) {
            throw "Inno Setup compiler was not found at '$RequestedPath'."
        }

        return (Resolve-Path $RequestedPath).Path
    }

    $compilerCommand = Get-Command 'iscc.exe' -ErrorAction SilentlyContinue
    if ($null -ne $compilerCommand) {
        return $compilerCommand.Source
    }

    $candidates = @(
        @(
            (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
            (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
            (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
        ) | Where-Object {
            -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path $_ -PathType Leaf)
        }
    )

    if ($candidates.Count -gt 0) {
        return (Resolve-Path $candidates[0]).Path
    }

    throw 'Inno Setup 6 was not found. Install ISCC.exe or pass -InnoCompilerPath.'
}
