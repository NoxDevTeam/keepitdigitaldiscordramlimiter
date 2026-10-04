# KeepItDigital 2.0.3 build fix

This source release adds the missing explicit `System.IO` import to `SupabaseAuthService.cs`.

The previous build-fix source already corrected the WPF/WinForms terminal control namespace collisions and added required IO imports to the SSH/session storage services.

Build on Windows:

```cmd
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
