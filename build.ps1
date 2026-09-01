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

function Publish-Windows {
    $output = Join-Path $BuildRoot "windows"
    Write-Host "Publishing Windows (single-file) to $output ..."

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

    Write-Host "Building Android APK ..."

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

    $apkFiles = Get-ChildItem -Path $binDir -Filter "*.apk"
    if (-not $apkFiles) {
        throw "No APK files found in $binDir"
    }

    foreach ($apk in $apkFiles) {
        Copy-Item -Path $apk.FullName -Destination (Join-Path $output $apk.Name) -Force
        Write-Host "Android APK: $(Join-Path $output $apk.Name)"
    }

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
