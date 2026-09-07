#define AppName "Keep It Digital Discord RAM Limiter"
#define AppPublisher "Keep It Digital"
#ifndef AppVersion
  #define AppVersion "1.1.0"
#endif
#define AppExeName "KeepItDigitalDiscordRamLimiter.exe"

#ifndef SourceDir
  #define SourceDir "..\publish\KeepItDigitalDiscordRamLimiter-win-x64"
#endif

#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

[Setup]
AppId={{97E41524-D2EF-40FD-A6EF-C563568B4E4B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\Keep It Digital\Discord RAM Limiter
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=Keep-It-Digital-Discord-RAM-Limiter-Setup
SetupIconFile=..\DiscordRamLimiter\Assets\discord_no_logo.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
VersionInfoProductName={#AppName}
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "startup"; Description: "Start automatically when I sign in"; GroupDescription: "Additional options:"; Flags: checkedonce
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional options:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "DiscordRamLimiter"; Flags: deletevalue; Check: not IsUpdateMode
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "KeepItDigitalDiscordRamLimiter"; ValueData: """{app}\{#AppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: startup; Check: not IsUpdateMode

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent; Check: not IsUpdateMode
Filename: "{app}\{#AppExeName}"; Parameters: "--updated"; Flags: nowait; Check: IsUpdateMode

[Code]
function IsUpdateMode: Boolean;
begin
  Result := CompareText(ExpandConstant('{param:UPDATE|0}'), '1') = 0;
end;
