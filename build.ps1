param(
    [Parameter(Position = 0)]
    [ValidateSet("windows", "android", "all")]
    [string]$Target = "all",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Root = $PSScriptRoot
$Project = Join-Path $Root "src\QRTwin\QRTwin.csproj"
$BuildRoot = Join-Path $Root "build"

if (-not (Test-Path $Project)) {
    throw "Project not found: $Project"
}

function Clear-BuildCache {
    param(
        [string]$Framework
    )

    Write-Host "Cleaning previous build outputs ..."
    if ($Framework) {
        dotnet clean $Project -c $Configuration -f $Framework --nologo -v q
    }
    else {
        dotnet clean $Project -c $Configuration --nologo -v q
    }

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet clean failed with exit code $LASTEXITCODE"
    }

    # dotnet clean can leave a stale MAUI resizetizer cache (wrong .NET template appicon.ico).
    $objRoot = Join-Path $Root "src\QRTwin\obj"
    if (Test-Path $objRoot) {
        Get-ChildItem -Path $objRoot -Recurse -Directory -Filter "resizetizer" -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        Get-ChildItem -Path $objRoot -Recurse -File -Include "mauiimage.stamp","mauiimage.outputs","mauiimage.inputs" -ErrorAction SilentlyContinue |
            Remove-Item -Force -ErrorAction SilentlyContinue
    }
}

function Get-VersionName {
    [xml]$projectXml = Get-Content $Project
    return $projectXml.Project.PropertyGroup.ApplicationDisplayVersion | Select-Object -First 1
}

function Publish-Windows {
    $output = Join-Path $BuildRoot "windows"
    $version = Get-VersionName
    Write-Host "Publishing Windows (single-file) to $output ..."

    Clear-BuildCache -Framework "net10.0-windows10.0.19041.0"

    dotnet publish $Project `
        -f net10.0-windows10.0.19041.0 `
        -c $Configuration `
        -o $output `
        -p:RuntimeIdentifierOverride=win-x64 `
        -p:WindowsPackageType=None `
        -p:WindowsAppSDKSelfContained=true `
        -p:SelfContained=true `
        -p:PublishSingleFile=true `
        -p:IncludeAllContentForSelfExtract=true `
        -p:EnableMsixTooling=true

    if ($LASTEXITCODE -ne 0) {
        throw "Windows publish failed with exit code $LASTEXITCODE"
    }

    $exe = Get-ChildItem -Path $output -Filter "*.exe" |
        Where-Object { $_.Name -notlike "RestartAgent.exe" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($exe) {
        Write-Host "Windows executable: $($exe.FullName)"
    }

    Write-Host "Windows publish complete: $output"
}

function Build-Android {
    $output = Join-Path $BuildRoot "android"
    $binDir = Join-Path $Root "src\QRTwin\bin\$Configuration\net10.0-android"
    $version = Get-VersionName
    $artifactName = "qrtwin-$version.apk"

    Write-Host "Building Android APK ..."

    Clear-BuildCache -Framework "net10.0-android"

    dotnet build $Project `
        -f net10.0-android `
        -c $Configuration `
        -p:AndroidPackageFormats=apk

    if ($LASTEXITCODE -ne 0) {
        throw "Android build failed with exit code $LASTEXITCODE"
    }

    if (-not (Test-Path $binDir)) {
        throw "Android build output not found: $binDir"
    }

    New-Item -ItemType Directory -Force -Path $output | Out-Null

    $apk = Get-ChildItem -Path $binDir -Filter "*.apk" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $apk) {
        throw "No APK files found in $binDir"
    }

    $destination = Join-Path $output $artifactName
    Copy-Item -Path $apk.FullName -Destination $destination -Force
    Write-Host "Android APK: $destination"

    Write-Host "Android build complete: $output"
}

switch ($Target) {
    "windows" { Publish-Windows }
    "android" { Build-Android }
    "all" {
        Publish-Windows
        Build-Android
    }
}
