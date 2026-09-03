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
    # ApplicationDisplayVersion is $(Version) in the csproj; read Version directly.
    return $projectXml.Project.PropertyGroup.Version | Select-Object -First 1
}

function Publish-Windows {
    $output = Join-Path $BuildRoot "windows"
    $binDir = Join-Path $Root "src\QRTwin\bin\$Configuration\net10.0-windows10.0.19041.0\win-x64"
    $version = Get-VersionName

    Write-Host "Building Windows to $output ..."

    Clear-BuildCache -Framework "net10.0-windows10.0.19041.0"

    # dotnet publish with PublishSingleFile omits Platforms\Windows\App.xbf and crashes at startup.
    dotnet build $Project `
        -f net10.0-windows10.0.19041.0 `
        -c $Configuration `
        -p:RuntimeIdentifierOverride=win-x64 `
        -p:WindowsPackageType=None `
        -p:WindowsAppSDKSelfContained=true `
        -p:SelfContained=true

    if ($LASTEXITCODE -ne 0) {
        throw "Windows build failed with exit code $LASTEXITCODE"
    }

    if (-not (Test-Path $binDir)) {
        throw "Windows build output not found: $binDir"
    }

    if (Test-Path $output) {
        Remove-Item -Path $output -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $output | Out-Null
    Copy-Item -Path (Join-Path $binDir "*") -Destination $output -Recurse -Force

    $exe = Join-Path $output "qrtwin.exe"
    if (-not (Test-Path $exe)) {
        throw "Windows executable not found: $exe"
    }

    # WinApp SDK / MAUI resolve the app host by AssemblyName; renaming the .exe breaks startup.
    Write-Host "Windows executable: $exe (version $version)"
    Write-Host "Windows build complete: $output"
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
