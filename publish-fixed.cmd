@echo off
setlocal
cd /d "%~dp0"

echo === Closing any running KeepItDigital instance ===
taskkill /F /IM KeepItDigital.exe >nul 2>&1
if errorlevel 1 echo No running KeepItDigital.exe found.

timeout /t 1 /nobreak >nul

echo === Cleaning previous build ===
dotnet clean "DiscordRamLimiter\DiscordRamLimiter.csproj" -c Release
if errorlevel 1 goto :fail

if exist "DiscordRamLimiter\bin" rmdir /s /q "DiscordRamLimiter\bin"
if exist "DiscordRamLimiter\bin" (
    echo ERROR: Could not remove the bin folder. A file is still in use.
    echo Close KeepItDigital, Visual Studio, VS Code, or File Explorer windows using the publish folder, then run this script again.
    goto :fail
)
if exist "DiscordRamLimiter\obj" rmdir /s /q "DiscordRamLimiter\obj"
if exist "DiscordRamLimiter\obj" (
    echo ERROR: Could not remove the obj folder.
    goto :fail
)

echo.
echo === Publishing KeepItDigital (Windows x64, self-contained) ===
dotnet publish "DiscordRamLimiter\DiscordRamLimiter.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 goto :fail

echo.
echo === Checking publish output ===
set "PUBLISH=DiscordRamLimiter\bin\Release\net9.0-windows\win-x64\publish"

rem Ensure runtime configuration is present even if MSBuild does not copy linked content.
if not exist "supabase.json" (
    echo ERROR: root supabase.json is missing.
    goto :fail
)
copy /Y "supabase.json" "%PUBLISH%\supabase.json" >nul
if errorlevel 1 (
    echo ERROR: Could not copy supabase.json to publish output.
    goto :fail
)

if not exist "%PUBLISH%\KeepItDigital.exe" (
    echo ERROR: KeepItDigital.exe was not created.
    goto :fail
)

if not exist "%PUBLISH%\supabase.json" (
    echo ERROR: supabase.json was not copied to publish output.
    echo Check DiscordRamLimiter.csproj CopyToPublishDirectory setting.
    goto :fail
)

echo OK: KeepItDigital.exe found.
echo OK: supabase.json found.
echo.
echo Publish completed successfully.
echo Output:
echo %PUBLISH%
echo.
dir /b "%PUBLISH%"
echo.
echo Launching KeepItDigital.exe for testing...
start "" "%PUBLISH%\KeepItDigital.exe"
exit /b 0

:fail
echo.
echo ========================================
echo PUBLISH FAILED
echo ========================================
pause
exit /b 1
