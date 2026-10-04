# KeepItDigital release artifacts

Each GitHub Release should contain exactly:

- `KeepItDigital-Setup.exe` — recommended installer.
- `KeepItDigital-Portable.exe` — single-file portable executable; no installation required.

`supabase.json` is NOT shipped. The Supabase project URL and publishable/anon key are embedded in the client application. Never embed service-role keys, database passwords, SMTP passwords, or other secrets.
