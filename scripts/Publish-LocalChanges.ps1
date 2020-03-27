<#
.SYNOPSIS 
Packs the local component changes, publishes to the local NuGet feed, and updates all of the project references under $RootPath.

.PARAMETER RootPath
The root path where to look for projects to be replaced. If not specified, then the current path will be used.

.PARAMETER DoNotUpdateProjectReferences
A flag indicating to not update the project reference.

.PARAMETER SkipBuild
A flag indicating to skip building.
#>

param(
    [string]$RootPath,

    [switch]$DoNotUpdateProjectReferences,

    [switch]$SkipBuild
)

Set-StrictMode -Version Latest 
$ErrorActionPreference = "stop"
$Global:InformationPreference = "continue"

Set-Variable RepoRootPath -Option Constant -Value (Resolve-Path "$PSScriptRoot\..")
Set-Variable LocalNuGetFeedPath -Option Constant -Value "$RepoRootPath\.nuget-local"

function DownloadNuGetIfNeeded() {
    Set-Variable NuGetDownloadUrl -Option Constant -Value "https://dist.nuget.org/win-x86-commandline/latest/NuGet.exe"

    $toolsPath = Join-Path $RepoRootPath ".tools"
    $nugetPath = Join-Path $toolsPath "nuget.exe"
    
    if (Test-Path $nugetPath) {
        return
    }

    if (!(Test-Path $toolsPath)) {
        New-Item -ItemType Directory $toolsPath | Out-Null
    }

    $tempPath = Join-Path $toolsPath ([Guid]::NewGuid())

    try {
        $response = Invoke-WebRequest -Uri $NuGetDownloadUrl -PassThru -OutFile $nugetPath -UseBasicParsing
    }
    catch [System.Net.WebException] {
        $response = $_.Exception.Response
    }

    if ($response.StatusCode -ne 200) {
        Write-Warning "Failed to download NuGet tool from $NuGetDownloadUrl"
    }
}

function ConfigureLocalNuGetFeed() {
    # Add the local NuGet feed to nuget.config if needed.
    $nugetConfigFileName = "$RootPath\nuget.config"
    $nugetConfig = [xml](Get-Content $nugetConfigFileName)

    $localFeed = $nugetConfig.configuration.packageSources.add | Where-Object { $_.value -eq $LocalNuGetFeedPath }

    if ($localFeed -eq $null) {
        # Add the local NuGet feed
        $element = $nugetConfig.CreateElement("add")

        $keyAttribute = $nugetConfig.CreateAttribute("key")
        $keyAttribute.Value = "Local NuGet Feed"

        $valueAttribute = $nugetConfig.CreateAttribute("value")
        $valueAttribute.Value = $LocalNuGetFeedPath

        $element.Attributes.Append($keyAttribute)  | Out-Null
        $element.Attributes.Append($valueAttribute)  | Out-Null

        $nugetConfig.configuration.packageSources.AppendChild($element)

        $nugetConfig.Save($nugetConfigFileName)
    }
}

function CheckUnstagedProjects($ProjectsWithDependencies) {
    # Check to see if any of the projects already has pending changes.
    $listOfUnstagedFiles = &git diff --name-only | ForEach-Object { Resolve-Path $_ -ErrorAction SilentlyContinue }

    $projectsWithPendingChange = $listOfUnstagedFiles | Where-Object { $_ -in $ProjectsWithDependencies }

    if ($projectsWithPendingChange) {
        $projects = [string]::Join("`n", $projectsWithPendingChange);

        $title = "Confirm"
        $prompt = "The following projects has pending change:`n`n$projects`n`nSelect Yes to continue with update or No to abort."
        $continue = New-Object System.Management.Automation.Host.ChoiceDescription '&Yes', 'Continue the operation'
        $abort = New-Object System.Management.Automation.Host.ChoiceDescription '&No', "Abort the operation"
        
        $choice = $host.UI.PromptForChoice($title, $prompt, @($continue, $abort), 0)

        if ($choice -eq 1) {
            exit
        }
    }
}

if ([string]::IsNullOrEmpty($RootPath)) {
    $RootPath = Get-Location
}

if (!(Test-Path $RootPath)) {
    Write-Error "The root path could not be found. Please make sure the specified path is correct."
    exit
}

# Generate the version suffix using the branch name of the current time.
Push-Location $RepoRootPath

try {
    DownloadNuGetIfNeeded

    $branchName = &git rev-parse --abbrev-ref HEAD
    $branchNameParts = $branchName.Split("/")
    $versionSuffix = "$($branchNameParts[$branchNameParts.Length - 1])-$(Get-Date -Format yyyyMMdd-HHmmss)-preview"

    # Find all projects in the components.
    $projects = Get-ChildItem -Recurse -Include *.csproj -Exclude *Tests.csproj $RepoRootPath

    # Create NuGet package for each of the projects.
    $tempPath = [IO.Path]::Combine([IO.Path]::GetTempPath(), [Guid]::NewGuid())

    $projects | ForEach-Object {
        if ($SkipBuild) {
            &dotnet pack -o $tempPath --no-build --version-suffix $versionSuffix --include-symbols $_.FullName
        }
        else {
            &dotnet pack -o $tempPath --version-suffix $versionSuffix --include-symbols $_.FullName
        }
    }

    # Publish to local NuGet feed.
    if (!(Test-Path $LocalNuGetFeedPath)) {
        New-Item $LocalNuGetFeedPath -ItemType Directory | Out-Null
    }

    Get-ChildItem -Recurse -Include *.symbols.nupkg $tempPath | ForEach-Object {
        &.\.tools\nuget add $_.FullName -Source $LocalNuGetFeedPath
    }

    # Delete the temp
    Remove-Item -Path $tempPath -Recurse
} finally {
    Pop-Location
}

# Update the project references
Push-Location $RootPath

try {
    ConfigureLocalNuGetFeed

    if (!$DoNotUpdateProjectReferences) {
        # Find all projects that has dependencies on FHIR projects.
        $projectsWithDependencies = (Get-ChildItem -Recurse -Include *.csproj $RootPath).FullName

        CheckUnstagedProjects($projectsWithDependencies)

        $utf8WithBom = New-Object System.Text.UTF8Encoding($true)

        $projectsWithDependencies | ForEach-Object {
            Write-Information "Updating $_"

            $project = [xml](Get-Content $_)

            $project.SelectNodes("Project/ItemGroup/PackageReference") |
                Where-Object { $_.Include -match "Microsoft.Health.*" -and $_.Include -ne "Microsoft.Health.Extensions.BuildTimeCodeGenerator" } |
                ForEach-Object { $_.Version = "1.0.0-$versionSuffix" }

            $writer = New-Object System.IO.StreamWriter($_, $false, $utf8WithBom)

            $project.Save($writer)

            $writer.Close()

            # Removed the XML declaration.
            $project = Get-Content $_
            $project | Select-Object -Skip 1 | Set-Content -Encoding UTF8 -Path $_
        }
    }
} finally {
    Pop-Location
}