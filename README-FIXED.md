# KeepItDigital 2.0.3 — fixed build

This package contains the source project with two fixes:

1. The Login / Create Account window now uses a transparent WPF window background, so the area outside the rounded 28px panel is transparent instead of a rectangular background.
2. `supabase.json` is explicitly copied to both the normal output and the publish output. The existing Supabase loader already reads it from `AppContext.BaseDirectory`, so the published application can find the configuration.

## Build / publish

Requirements:
- Windows
- .NET 9 SDK

You can simply double-click:

    publish-fixed.cmd

Or run:

    cd /d "C:\path\to\KeepItDigital-2.0.3\DiscordRamLimiter"
    dotnet clean -c Release
    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

The script also verifies that both of these exist in the publish folder:

    KeepItDigitalRamLimiter.exe
    supabase.json

## Supabase

The project expects:

    supabase.json

with:

    {
      "url": "https://YOUR-PROJECT.supabase.co",
      "anonKey": "YOUR-PUBLISHABLE-ANON-KEY"
    }

The current project already contains your supplied `supabase.json`.

Do NOT put a Supabase `service_role` / secret key in this file. A publishable/anon key is the appropriate client-side key.

## After publishing

Open:

    DiscordRamLimiter\bin\Release\net9.0-windows\win-x64\publish

and launch:

    KeepItDigitalRamLimiter.exe

Keep `supabase.json` beside the executable for this configuration.

The source `bin` and `obj` folders were intentionally removed from this ZIP so you get a clean source package and don't accidentally run an old build.
