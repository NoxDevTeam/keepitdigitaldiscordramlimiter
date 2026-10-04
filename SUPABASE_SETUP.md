# KeepItDigital — Supabase Auth setup

The desktop app now uses the `keepitdigital://` deep-link after email verification.

## 1. Add the redirect URL

In Supabase Dashboard:

**Authentication → URL Configuration → Redirect URLs**

Add this exact URL:

```text
keepitdigital://auth/callback
```

Do not use `http://localhost:3000` for the desktop release.

Supabase must allow the redirect URL used by the signup request. The app sends this redirect automatically when it creates a new account.

## 2. Confirm signup email template

Open:

**Authentication → Email Templates → Confirm signup**

The file `supabase-confirm-email-template.html` in this project is a polished replacement template.

Paste its HTML into the Confirm signup template and save it.

The important link must remain:

```html
<a href="{{ .ConfirmationURL }}">Verify my email</a>
```

Do not replace `{{ .ConfirmationURL }}` with a hard-coded URL.

## 3. What the new flow does

```text
Create account
      ↓
Supabase sends verification email
      ↓
User clicks "Verify my email"
      ↓
Supabase confirms the account
      ↓
keepitdigital://auth/callback
      ↓
KeepItDigital receives the callback
      ↓
"Email verified"
      ↓
Sign in
```

The installer registers the `keepitdigital://` Windows protocol automatically.

The application also registers it at runtime for portable/publish-folder launches.

## 4. The proxy username/password popup

If the browser asks for:

`The proxy ... requires a username and password`

that is the Windows/browser proxy, not Supabase authentication. Never enter the KeepItDigital/Supabase account password into that proxy dialog.

If you don't intentionally use a proxy, check:

**Windows Settings → Network & Internet → Proxy**

and disable an unexpected manual proxy.

## 5. `supabase.json`

The desktop app needs `supabase.json` next to `KeepItDigital.exe`.

The installer now includes it automatically.
