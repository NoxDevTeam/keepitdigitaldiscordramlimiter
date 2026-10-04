# KeepItDigital release build

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-release.ps1
```

Output in `dist`:

- `KeepItDigital-Setup.exe` — installer for GitHub Releases
- `KeepItDigital-Portable.exe` — single-file portable EXE for GitHub Releases
- `update.json` — copy this file to the repository root (`main/update.json`) after updating the release version and URL. It is used by the app's automatic updater and is **not** required as a user download.

No `supabase.json` is included. Supabase URL + publishable/anon key are embedded in the client.
Do not embed service-role keys, SMTP passwords, Gmail App Passwords, or other secrets.
