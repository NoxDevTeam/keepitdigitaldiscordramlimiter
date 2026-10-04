#define AppName "KeepItDigital"
#define AppPublisher "Keep It Digital"
#ifndef AppVersion
  #define AppVersion "2.0.0"
#endif
#define AppExeName "KeepItDigital.exe"

#ifndef SourceDir
  #define SourceDir "..\publish\KeepItDigital-win-x64"
#endif

#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

[Setup]
AppId={{97E41524-D2EF-40FD-A6EF-C563568B4E4B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\KeepItDigital
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=KeepItDigital-Setup
SetupIconFile=..\DiscordRamLimiter\Assets\keepitdigital.ico
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

[InstallDelete]
Type: files; Name: "{app}\KeepItDigitalDiscordRamLimiter.exe"
Type: files; Name: "{app}\KeepItDigitalRamLimiter.exe"
Type: files; Name: "{app}\DiscordRamLimiter.exe"
Type: files; Name: "{autoprograms}\Keep It Digital Discord RAM Limiter.lnk"
Type: files; Name: "{autodesktop}\Keep It Digital Discord RAM Limiter.lnk"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "KeepItDigital"; ValueData: """{app}\{#AppExeName}"" --minimized"; Flags: uninsdeletevalue; Check: ShouldMigrateStartup
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "KeepItDigital"; ValueData: """{app}\{#AppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: startup; Check: not IsUpdateMode
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "DiscordRamLimiter"; Flags: deletevalue
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "KeepItDigitalDiscordRamLimiter"; Flags: deletevalue
Root: HKCU; Subkey: "Software\Classes\keepitdigital"; ValueType: string; ValueName: ""; ValueData: "URL:KeepItDigital Protocol"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\keepitdigital"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\keepitdigital\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent; Check: not IsUpdateMode
Filename: "{app}\{#AppExeName}"; Parameters: "--updated"; Flags: nowait; Check: IsUpdateMode

[Code]
function IsUpdateMode: Boolean;
begin
  Result := CompareText(ExpandConstant('{param:UPDATE|0}'), '1') = 0;
end;

function ShouldMigrateStartup: Boolean;
begin
  Result := IsUpdateMode and
    (RegValueExists(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'DiscordRamLimiter') or
     RegValueExists(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'KeepItDigitalDiscordRamLimiter'));
end;
