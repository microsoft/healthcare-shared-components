# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.

[CmdletBinding()]
param(
    [string] $DotNetPath = 'dotnet',
    [string[]] $SdkVersion = @(),
    [string[]] $ConsumerTargetFramework = @('net10.0', 'net472'),
    [string] $FullFrameworkMSBuildPath
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

function Invoke-DotNet {
    param([string[]] $Arguments)

    & $DotNetPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Assert-Sql {
    param([string] $OutputFile, [string] $Description)

    $actual = (Get-Content $OutputFile -Raw) -replace '\s+', ' '
    $expected = (Get-Content (Join-Path $PSScriptRoot 'PackageTests.sql') -Raw) -replace '\s+', ' '
    if ($actual.Trim() -ne $expected.Trim()) {
        throw "Generated SQL did not match PackageTests.sql for $Description. See $OutputFile."
    }

    Write-Host "PASS $Description generated the expected SQL from the package."
}

$DotNetPath = (Get-Command $DotNetPath).Source
if ($SdkVersion.Count -eq 0) {
    $SdkVersion = @(& $DotNetPath --version)
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not determine the current SDK version.'
    }
}

$runDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "sql-task-package-tests-$([Guid]::NewGuid().ToString('N'))"
$feed = Join-Path $runDirectory 'feed'
$packages = Join-Path $runDirectory 'packages'
$packageVersion = '0.0.0-package-tests'
New-Item $feed -ItemType Directory -Force | Out-Null

Invoke-DotNet @('pack', (Join-Path $PSScriptRoot '../Microsoft.Health.Tools.Sql.Tasks/Microsoft.Health.Tools.Sql.Tasks.csproj'),
    '--configuration', 'Release', '--output', $feed, "-p:PackageVersion=$packageVersion", '--nologo')

Set-Content (Join-Path $runDirectory 'Directory.Build.props') '<Project />'
Set-Content (Join-Path $runDirectory 'Directory.Packages.props') '<Project />'
Set-Content (Join-Path $runDirectory 'NuGet.Config') @"
<configuration>
  <packageSources>
    <clear />
    <add key="package-tests" value="$([System.Security.SecurityElement]::Escape($feed))" />
  </packageSources>
</configuration>
"@

foreach ($sdk in $SdkVersion) {
    $sdkDirectory = Join-Path $runDirectory $sdk
    New-Item $sdkDirectory -ItemType Directory | Out-Null
    Set-Content (Join-Path $sdkDirectory 'global.json') (@{
        sdk = @{ version = $sdk; rollForward = 'disable'; allowPrerelease = $true }
    } | ConvertTo-Json)

    Push-Location $sdkDirectory
    try {
        $actualSdk = & $DotNetPath --version
        if ($LASTEXITCODE -ne 0 -or $actualSdk -ne $sdk) {
            throw "Expected SDK $sdk, got $actualSdk."
        }

        foreach ($framework in $ConsumerTargetFramework) {
            $consumerDirectory = Join-Path $sdkDirectory $framework
            New-Item $consumerDirectory -ItemType Directory | Out-Null
            Copy-Item (Join-Path $PSScriptRoot 'PackageTests.proj') $consumerDirectory
            $project = Join-Path $consumerDirectory 'PackageTests.proj'
            Invoke-DotNet @('msbuild', $project, '-restore', '-target:GenerateFullSqlScript', '-nologo', '-nodeReuse:false',
                "-p:TargetFramework=$framework", "-p:SqlTasksPackageVersion=$packageVersion",
                "-p:SqlTestDirectory=$PSScriptRoot", "-p:RestorePackagesPath=$packages",
                "-p:RestoreConfigFile=$(Join-Path $runDirectory 'NuGet.Config')")

            Assert-Sql (Join-Path $consumerDirectory 'generated.sql') "SDK $sdk, consumer $framework"
        }

    }
    finally {
        Pop-Location
    }
}

if ($FullFrameworkMSBuildPath) {
    $consumerDirectory = Join-Path $runDirectory 'full-framework'
    New-Item (Join-Path $consumerDirectory 'obj') -ItemType Directory -Force | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'PackageFullFramework.proj') $consumerDirectory
    $project = Join-Path $consumerDirectory 'PackageFullFramework.proj'
    $packageDirectory = Join-Path $packages "microsoft.health.tools.sql.tasks/$packageVersion"
    & $FullFrameworkMSBuildPath $project '-target:GenerateFullSqlScript' '-nologo' '-nodeReuse:false' `
        "-p:SqlTasksPackageDirectory=$packageDirectory" "-p:SqlTestDirectory=$PSScriptRoot"
    if ($LASTEXITCODE -ne 0) {
        throw "Full-framework MSBuild failed with exit code $LASTEXITCODE."
    }
    Assert-Sql (Join-Path $consumerDirectory 'generated.sql') 'Full-framework MSBuild'
}

Remove-Item -LiteralPath $runDirectory -Recurse -Force
