# KeepItDigital 2.0.1 build fix

Fixed a compile error in `SupabaseAuthService.cs` by explicitly importing `System.Net.Http` for `HttpClient`.

Build commands:

```powershell
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
