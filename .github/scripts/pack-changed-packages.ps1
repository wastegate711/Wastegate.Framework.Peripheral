$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path $env:GITHUB_WORKSPACE).Path
$outputDirectory = Join-Path $repositoryRoot 'artifacts'
New-Item -ItemType Directory -Force $outputDirectory | Out-Null
Get-ChildItem $outputDirectory -Filter '*.nupkg' -File | Remove-Item -Force

$isRelease = $env:RELEASE_MODE -eq 'true'
$changedPackagesOnly = $env:CHANGED_PACKAGES_ONLY -eq 'true'
$changedFiles = @()
$allPackagesChanged = -not $changedPackagesOnly

if ($changedPackagesOnly) {
    $before = $env:BASE_SHA
    $compareSha = $env:COMPARE_SHA
    if ([string]::IsNullOrWhiteSpace($before) -or [string]::IsNullOrWhiteSpace($compareSha)) {
        throw 'Both BASE_SHA and COMPARE_SHA are required to select changed packages.'
    }
    if ($before -match '^0+$') {
        $allPackagesChanged = $true
    }
    else {
        git -C $repositoryRoot diff --name-only $before $compareSha -- | ForEach-Object {
            $changedFiles += $_.Replace('\', '/')
        }
        if ($LASTEXITCODE -ne 0) {
            throw "Could not compare commits $before and $compareSha."
        }
    }
}

if (@($changedFiles | Where-Object {
    (Split-Path $_ -Leaf) -match '^(Directory\.Build\.(props|targets)|Directory\.Packages\.props|global\.json|NuGet\.Config)$'
}).Count -gt 0) {
    $allPackagesChanged = $true
}

$projects = Get-ChildItem -Path $repositoryRoot -Filter '*.csproj' -File -Recurse |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|artifacts|\.git|session-state)[\\/]' }
if (-not $projects) {
    throw 'No library projects were found.'
}

$selectedPackages = @()
foreach ($project in $projects) {
    $packageId = (dotnet msbuild $project.FullName -getProperty:PackageId | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "Could not read PackageId from $($project.FullName)."
    }
    if ([string]::IsNullOrWhiteSpace($packageId)) {
        continue
    }

    $projectRelativePath = [IO.Path]::GetRelativePath($repositoryRoot, $project.FullName).Replace('\', '/')
    $projectRelativeDirectory = [IO.Path]::GetRelativePath($repositoryRoot, $project.DirectoryName).Replace('\', '/').TrimEnd('/')
    $directoryPrefix = "$projectRelativeDirectory/"
    $isChanged = $allPackagesChanged -or @($changedFiles | Where-Object {
        $_ -eq $projectRelativePath -or $_.StartsWith($directoryPrefix, [StringComparison]::OrdinalIgnoreCase)
    }).Count -gt 0

    if (-not $isChanged) {
        [xml]$projectXml = Get-Content -Path $project.FullName -Raw
        $externalPackageFiles = @()
        foreach ($item in $projectXml.SelectNodes("//*[local-name()='Compile' or local-name()='None' or local-name()='Content' or local-name()='EmbeddedResource']")) {
            foreach ($attributeName in @('Include', 'Update')) {
                $include = $item.GetAttribute($attributeName)
                if ([string]::IsNullOrWhiteSpace($include) -or $include -match '[*?@$()]') {
                    continue
                }

                $fullPath = [IO.Path]::GetFullPath([IO.Path]::Combine($project.DirectoryName, $include))
                $relativePath = [IO.Path]::GetRelativePath($repositoryRoot, $fullPath).Replace('\', '/')
                if (-not $relativePath.StartsWith('../', [StringComparison]::Ordinal) -and
                    -not [IO.Path]::IsPathRooted($relativePath)) {
                    $externalPackageFiles += $relativePath
                }
            }
        }

        $isChanged = @($changedFiles | Where-Object {
            $externalPackageFiles -contains $_
        }).Count -gt 0
    }

    if ($isChanged) {
        $version = (dotnet msbuild $project.FullName -getProperty:Version | Out-String).Trim()
        if ($LASTEXITCODE -ne 0) {
            throw "Could not read Version from $($project.FullName)."
        }
        if ([string]::IsNullOrWhiteSpace($version)) {
            throw "Project $($project.FullName) must define a package Version."
        }

        if ($isRelease) {
            if ($version -notmatch '^\d+\.\d+\.\d+$') {
                throw "Stable package version '$version' in $($project.Name) must be a three-part version such as 1.2.3."
            }

            $versionIndexUrl = "https://api.nuget.org/v3-flatcontainer/$($packageId.ToLowerInvariant())/index.json"
            try {
                $publishedVersions = (Invoke-RestMethod -Uri $versionIndexUrl -TimeoutSec 30).versions
            }
            catch {
                $response = $_.Exception.Response
                $statusCode = $null
                if ($response -and $response.PSObject.Properties['StatusCode']) {
                    $statusCode = [int]$response.StatusCode
                }

                if ($statusCode -ne 404) {
                    throw "Could not query published versions for package '$packageId': $($_.Exception.Message)"
                }

                $publishedVersions = @()
            }

            if ($publishedVersions -contains $version) {
                throw "Package '$packageId' version $version is already published. Increase the <Version> in $($project.Name) before releasing this package."
            }
        }

        $selectedPackages += [pscustomobject]@{
            Project = $project
            Id = $packageId
            Version = $version
        }
    }
}

if ($selectedPackages.Count -eq 0) {
    Write-Host 'No package projects changed; nothing will be packed or published.'
    'package_count=0' >> $env:GITHUB_OUTPUT
    exit 0
}

foreach ($package in $selectedPackages) {
    if ($isRelease) {
        $packageVersion = $package.Version
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:PR_NUMBER)) {
        $packageVersion = "0.0.0-pr.$($env:PR_NUMBER).$($env:GITHUB_RUN_NUMBER)"
    }
    else {
        $packageVersion = "0.0.0-develop.$env:GITHUB_RUN_NUMBER"
    }

    Write-Host "Packing $($package.Id) version $packageVersion from $($package.Project.FullName)"
    dotnet pack $package.Project.FullName --configuration Release --no-build --no-restore --output $outputDirectory "-p:PackageVersion=$packageVersion"
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed for package '$($package.Id)' with exit code $LASTEXITCODE."
    }
}

"package_count=$($selectedPackages.Count)" >> $env:GITHUB_OUTPUT
"package_ids=$((@($selectedPackages | ForEach-Object Id) -join ', '))" >> $env:GITHUB_OUTPUT
