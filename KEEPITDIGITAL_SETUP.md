# KeepItDigital 2.0 setup

1. Open `supabase.json` and replace the placeholder values with:
   - your Supabase project URL
   - your Supabase anon/public key
2. In Supabase Dashboard → Authentication → Providers, enable Email/Password.
3. Build on Windows with .NET 9:
   `dotnet restore .\DiscordRamLimiter.sln`
   `dotnet build .\DiscordRamLimiter.sln -c Release`
4. Run:
   `dotnet run --project .\DiscordRamLimiter\DiscordRamLimiter.csproj`
5. Create an account from the KeepItDigital login screen.

For distribution, keep the anon key only in the client configuration. Never ship a Supabase service-role key.

SSH:
- Windows OpenSSH Client is the default.
- For password automation, install `plink.exe` and make it available on PATH. The app detects it automatically.
- For best security, use a private key instead of a password.

The package intentionally contains source code and project assets rather than a newly compiled EXE because the build environment used to prepare this update does not contain the .NET SDK.
