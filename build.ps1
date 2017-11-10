[cmdletbinding()]
param(
    [Parameter(Position=0, ValueFromRemainingArguments=$true)]
    $ExtraParameters
)
$ErrorActionPreference="Stop"
$ProgressPreference="SilentlyContinue"

$RepoRoot = "$PSScriptRoot"
$DOTNET_INSTALL_DIR="$REPOROOT/.dotnet"

$env:REPOROOT="$RepoRoot"
$env:XDG_DATA_HOME="$REPOROOT/.nuget/packages"
$env:NUGET_PACKAGES="$REPOROOT/.nuget/packages"
$env:NUGET_HTTP_CACHE_PATH="$REPOROOT/.nuget/packages"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

if (-Not (Test-Path $DOTNET_INSTALL_DIR))
{
    New-Item -Type "directory" -Path $DOTNET_INSTALL_DIR 
}

Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "$DOTNET_INSTALL_DIR/dotnet-install.ps1"
& $DOTNET_INSTALL_DIR/dotnet-install.ps1 -InstallDir "$DOTNET_INSTALL_DIR" -Version 1.0.0-rc4-004911
& $DOTNET_INSTALL_DIR/dotnet-install.ps1 -InstallDir "$DOTNET_INSTALL_DIR" -Version 2.0.2

$env:PATH="$DOTNET_INSTALL_DIR;$env:PATH"

& dotnet msbuild build.proj /t:MakeVersionProps
& dotnet msbuild build.proj /v:n /fl /flp:v=n $ExtraParameters
