[CmdletBinding()]
param([string]$DownloadBaseUrl = '')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$projectFile = Join-Path $projectRoot 'DiscordRamLimiter\DiscordRamLimiter.csproj'
$publishDirectory = Join-Path $projectRoot 'publish\KeepItDigital-win-x64'
$distributionDirectory = Join-Path $projectRoot 'dist'
$installerScript = Join-Path $projectRoot 'installer\KeepItDigitalDiscordRamLimiter.iss'
$releaseNotesFile = Join-Path $projectRoot 'release-notes.txt'
$exeName = 'KeepItDigital.exe'
$portableName = 'KeepItDigital-Portable.exe'
$setupName = 'KeepItDigital-Setup.exe'
foreach ($d in @($publishDirectory,$distributionDirectory)) { if(Test-Path $d){Remove-Item $d -Recurse -Force}; New-Item -ItemType Directory -Path $d | Out-Null }
Get-Process KeepItDigital -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

dotnet publish $projectFile --configuration Release --runtime win-x64 --self-contained true --output $publishDirectory -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -p:EnableCompressionInSingleFile=true
if($LASTEXITCODE -ne 0){throw 'The application publish step failed.'}
$publishedExe=Join-Path $publishDirectory $exeName
if(-not(Test-Path $publishedExe)){throw 'KeepItDigital.exe was not produced.'}
$version=[version](Get-Item $publishedExe).VersionInfo.FileVersion.Trim(); $releaseVersion=$version.ToString(3)
if([string]::IsNullOrWhiteSpace($DownloadBaseUrl)){ $DownloadBaseUrl="https://github.com/NoxDevTeam/keepitdigitaldiscordramlimiter/releases/download/v$releaseVersion" }
Copy-Item $publishedExe (Join-Path $distributionDirectory $portableName)
$iscc=(Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
$candidates=@($iscc, (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\Inno Setup 6\ISCC.exe'), (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'), (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')) | Where-Object {$_ -and (Test-Path $_)}
$isccPath=$candidates|Select-Object -First 1
if(-not$isccPath){throw 'Inno Setup 6 is required. Install it with: winget install --id JRSoftware.InnoSetup --exact'}
& $isccPath "/DSourceDir=$publishDirectory" "/DOutputDir=$distributionDirectory" "/DAppVersion=$releaseVersion" $installerScript
if($LASTEXITCODE -ne 0){throw 'Installer build failed.'}
$setupPath=Join-Path $distributionDirectory $setupName
$hash=(Get-FileHash $setupPath -Algorithm SHA256).Hash
$notes=(Get-Content $releaseNotesFile -Raw).Trim()
[ordered]@{version=$releaseVersion; changelog=$notes; downloadUrl="$($DownloadBaseUrl.TrimEnd('/'))/$setupName"; sha256=$hash} | ConvertTo-Json | Set-Content (Join-Path $distributionDirectory 'update.json') -Encoding UTF8
Write-Host "`nRelease artifacts:" -ForegroundColor Green
Get-ChildItem $distributionDirectory -File | Select-Object Name,Length
