$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($env:WIKI_TOKEN)) {
    throw 'Configure the WIKI_TOKEN repository secret before publishing documentation.'
}
if ([string]::IsNullOrWhiteSpace($env:GITHUB_REPOSITORY)) {
    throw 'GITHUB_REPOSITORY must identify the source repository.'
}

$repositoryRoot = (Resolve-Path $env:GITHUB_WORKSPACE).Path
$wikiDirectory = Join-Path $env:RUNNER_TEMP "wiki-$env:GITHUB_RUN_ID-$env:GITHUB_RUN_ATTEMPT"
if (Test-Path $wikiDirectory) {
    throw "Wiki checkout path already exists: $wikiDirectory"
}

$credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("x-access-token:$env:WIKI_TOKEN"))
$wikiUrl = "https://github.com/$($env:GITHUB_REPOSITORY).wiki.git"
git -c "http.extraheader=AUTHORIZATION: basic $credentials" clone $wikiUrl $wikiDirectory
if ($LASTEXITCODE -ne 0) {
    throw 'Could not clone the repository Wiki. Confirm that Wiki is enabled and WIKI_TOKEN has write access.'
}
git -C $wikiDirectory remote set-url origin $wikiUrl

$projects = Get-ChildItem -Path $repositoryRoot -Filter '*.csproj' -File -Recurse |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|artifacts|\.git|session-state)[\\/]' }
if (-not $projects) {
    throw 'No library projects were found.'
}

$pageNames = @()
foreach ($project in $projects) {
    $readmePath = Join-Path $project.DirectoryName 'README.md'
    if (-not (Test-Path $readmePath)) {
        $readmePath = Join-Path $project.DirectoryName 'PackageReadme.md'
    }
    if (-not (Test-Path $readmePath)) {
        Write-Warning "No README.md or PackageReadme.md found for $($project.Name); skipping Wiki page."
        continue
    }

    $pageName = $project.Directory.Name
    Copy-Item -Path $readmePath -Destination (Join-Path $wikiDirectory "$pageName.md") -Force
    $pageNames += $pageName
}
if (-not $pageNames) {
    throw 'No project README files were available to publish.'
}

$home = @('# Библиотеки Wastegate.Framework.Peripheral', '', 'Страницы документации проектов:')
foreach ($pageName in ($pageNames | Sort-Object)) {
    $home += "- [$pageName]($pageName)"
}
[IO.File]::WriteAllText((Join-Path $wikiDirectory 'Home.md'), ($home -join "`n") + "`n", [Text.UTF8Encoding]::new($false))

git -C $wikiDirectory config user.name 'github-actions[bot]'
git -C $wikiDirectory config user.email '41898282+github-actions[bot]@users.noreply.github.com'
git -C $wikiDirectory add -- Home.md
if ($LASTEXITCODE -ne 0) {
    throw 'Could not stage the Wiki home page.'
}
foreach ($pageName in $pageNames) {
    git -C $wikiDirectory add -- "$pageName.md"
    if ($LASTEXITCODE -ne 0) {
        throw "Could not stage Wiki page $pageName."
    }
}
git -C $wikiDirectory diff --cached --quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host 'Wiki is already up to date.'
    exit 0
}
if ($LASTEXITCODE -ne 1) {
    throw 'Could not inspect staged Wiki changes.'
}

git -C $wikiDirectory commit -m 'Update project documentation from develop'
if ($LASTEXITCODE -ne 0) {
    throw 'Could not commit Wiki documentation.'
}
git -C $wikiDirectory -c "http.extraheader=AUTHORIZATION: basic $credentials" push origin HEAD
if ($LASTEXITCODE -ne 0) {
    throw 'Could not publish documentation to the repository Wiki.'
}