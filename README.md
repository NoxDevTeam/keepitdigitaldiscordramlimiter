# KeepItDigital

KeepItDigital is the expanded Windows control center built from the original Keep It Digital RAM Limiter. The original RAM limiter remains in the app and keeps its background/tray behavior.

## Included tools

- **RAM Limiter** — original live RAM monitoring, process trimming, startup option, emergency protection, tray mode and update checker.
- **Fast SSH** — saved server profiles, host/IP, username, port, encrypted local password storage, optional private key, multiple SSH session tabs and a custom command bar.
- **Startup Apps** — view current-user Windows startup entries, enable/disable them, and add custom `.exe` applications.
- **Profile & Settings** — Supabase-backed account profile and sign-out.

## Account setup

The app requires a KeepItDigital account before the main window opens.

Authentication uses the Supabase Auth REST API. Create `supabase.json` beside the executable (or use environment variables):

```json
{
  "url": "https://YOUR-PROJECT.supabase.co",
  "anonKey": "YOUR_SUPABASE_ANON_KEY"
}
```

The repository includes `supabase.example.json`. Do not commit your real `supabase.json`.

The Supabase anon/public key is intended for client applications; do not put a service-role key in this app.

Supabase must have email/password authentication enabled. If email confirmation is enabled, a new user must confirm their email before signing in.

Sessions are stored locally with Windows DPAPI for the current Windows user. SSH profiles are also protected with Windows DPAPI.

## SSH notes

Windows OpenSSH Client is used by default. If a `plink.exe` executable is available on `PATH` and a profile contains a password, KeepItDigital can use Plink for password-based authentication. Otherwise the app uses `ssh.exe`; SSH keys are recommended for unattended/passwordless sessions.

The command bar sends commands to the active SSH process. Multiple servers can remain open simultaneously as tabs.

## Startup apps

The Startup Apps page manages the current Windows user's `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entries. Disabled entries are moved into a KeepItDigital-specific disabled registry key and can be restored. Custom apps are added to the same per-user startup location and do not require administrator access.

## Build

Requirements:

- Windows 10 or newer
- .NET 9 SDK with Windows Desktop support
- Internet access for the first NuGet restore

```powershell
dotnet restore .\DiscordRamLimiter.sln
dotnet build .\DiscordRamLimiter.sln
dotnet run --project .\DiscordRamLimiter\DiscordRamLimiter.csproj
```

The current development environment used to prepare this source package does not include the .NET SDK, so the updated project was not compiled here.

## Security

- Supabase access/refresh tokens are protected with Windows DPAPI.
- SSH profile passwords are protected with Windows DPAPI.
- Never use a Supabase service-role key in a desktop client.
- Password-based SSH is less desirable than key authentication. Prefer a private key where possible.

## Original RAM limiter

The original RAM limiter services and supported-process logic were retained rather than removed. This includes Discord, Spotify, browser and selected utility process tracking, working-set trimming, tray operation, automatic startup and emergency memory protection.

## License

MIT.
