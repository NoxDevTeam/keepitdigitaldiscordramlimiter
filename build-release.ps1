[CmdletBinding()]
param(
    [string]$DownloadBaseUrl = ''
)

$ErrorActionPreference = 'Stop'

$projectRoot = $PSScriptRoot
$projectFile = Join-Path $projectRoot 'DiscordRamLimiter\DiscordRamLimiter.csproj'
$publishDirectory = Join-Path $projectRoot 'publish\KeepItDigitalDiscordRamLimiter-win-x64'
$distributionDirectory = Join-Path $projectRoot 'dist'
$installerScript = Join-Path $projectRoot 'installer\KeepItDigitalDiscordRamLimiter.iss'
$standaloneExecutable = 'KeepItDigitalDiscordRamLimiter.exe'
$setupExecutable = 'Keep-It-Digital-Discord-RAM-Limiter-Setup.exe'
$releaseNotesFile = Join-Path $projectRoot 'release-notes.txt'

foreach ($outputDirectory in @($publishDirectory, $distributionDirectory)) {
    $resolvedOutput = [IO.Path]::GetFullPath($outputDirectory)
    $resolvedRoot = [IO.Path]::GetFullPath($projectRoot) + [IO.Path]::DirectorySeparatorChar

    if (-not $resolvedOutput.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean an output directory outside the project: $resolvedOutput"
    }

    if (Test-Path -LiteralPath $resolvedOutput) {
        Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
    }

    New-Item -ItemType Directory -Path $resolvedOutput | Out-Null
}

dotnet publish $projectFile `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDirectory `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "The application publish step failed with exit code $LASTEXITCODE."
}

$publishedExecutablePath = Join-Path $publishDirectory $standaloneExecutable
$publishedVersion = [version](Get-Item -LiteralPath $publishedExecutablePath).VersionInfo.FileVersion.Trim()
$releaseVersion = $publishedVersion.ToString(3)

if ([string]::IsNullOrWhiteSpace($DownloadBaseUrl)) {
    $DownloadBaseUrl = "https://github.com/NoxDevTeam/keepitdigitaldiscordramlimiter/releases/download/v$releaseVersion"
}

Copy-Item -LiteralPath $publishedExecutablePath -Destination $distributionDirectory

$isccCommand = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
$isccCandidates = @(
    if ($isccCommand) { $isccCommand.Source }
    Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\Inno Setup 6\ISCC.exe'
    Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'
    Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

$isccPath = $isccCandidates | Select-Object -First 1
if (-not $isccPath) {
    throw 'Inno Setup 6 is required to build the installer. Install it with: winget install --id JRSoftware.InnoSetup --exact'
}

& $isccPath "/DSourceDir=$publishDirectory" "/DOutputDir=$distributionDirectory" "/DAppVersion=$releaseVersion" $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "The installer build failed with exit code $LASTEXITCODE."
}

$setupPath = Join-Path $distributionDirectory $setupExecutable
$setupHash = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash
$releaseNotes = (Get-Content -LiteralPath $releaseNotesFile -Raw).Trim()
$manifest = [ordered]@{
    version = $releaseVersion
    changelog = $releaseNotes
    downloadUrl = "$($DownloadBaseUrl.TrimEnd('/'))/$setupExecutable"
    sha256 = $setupHash
}

$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $distributionDirectory 'update.json') -Encoding utf8NoBOM

Get-ChildItem -LiteralPath $distributionDirectory -File | Select-Object Name, Length, FullName
