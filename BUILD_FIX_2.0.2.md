# KeepItDigital 2.0.2 build fixes

This source package fixes the remaining Windows build errors reported when compiling KeepItDigital 2.0.1:

- Explicit `System.IO` imports for local file/profile/session storage services.
- Explicit WPF control types in the SSH session UI so they cannot collide with `System.Windows.Forms` types in the mixed WPF/WinForms project.
- Explicit WPF `TextWrapping` and `ScrollBarVisibility` usage.

Build on Windows with the .NET 9 SDK:

```powershell
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The publisher output is under:

`DiscordRamLimiter\\bin\\Release\\net9.0-windows\\win-x64\\publish\\`
