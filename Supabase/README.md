# KeepItDigital Supabase email setup

## 1. Redirect URL
In Supabase Dashboard → Authentication → URL Configuration:

- Add `keepitdigital://auth/callback` to Additional Redirect URLs.
- Do not rely on `http://localhost:3000` for the desktop app.

## 2. Confirm signup email
In Authentication → Email Templates → Confirm signup:

- Subject: `Verify your KeepItDigital account`
- Replace the HTML with `keepitdigital-confirm-signup.html`.
- Keep `{{ .ConfirmationURL }}` in the Verify button exactly as shown.

Supabase's `ConfirmationURL` contains the secure verification endpoint and the redirect supplied by the desktop app.

## 3. SMTP
For production, configure custom SMTP (for example Resend, Postmark, SES, SendGrid or Brevo). The built-in Supabase mailer is intended for testing and has delivery/rate limitations.
