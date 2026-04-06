# Email OAuth Setup

This document explains how to configure OAuth for email sending in WireGuardUI.

Current implementation:

- Outlook / Hotmail / Microsoft 365 OAuth2: supported
- Gmail App Password: supported
- Gmail OAuth2: supported
- Yahoo App Password: supported
- Yahoo OAuth2: not enabled in current app flow

## 1. Azure App Registration (Microsoft)

Create (or update) an app registration in Azure Portal for Mail OAuth.

### 1.1 Choose Supported Account Types

Pick one based on your use case:

- Hotmail/Outlook.com only: personal Microsoft accounts only
- Microsoft 365 only: accounts in this org or multi-tenant org accounts
- Both Hotmail + Microsoft 365: accounts in any org directory and personal Microsoft accounts

Important:

- If app is personal-only, requests to `common`/`organizations` will fail (`AADSTS9002331`).

### 1.2 Authentication (Redirect URI)

For local/manual auth-code flow:

- Add redirect URI: `http://localhost`

### 1.3 Public vs Confidential Client

WireGuardUI supports both, but local testing is usually public-client flow.

- Public client flow:
  - Enable "Allow public client flows"
  - Do not use `client_secret` in WireGuardUI Email Settings
- Confidential client flow:
  - Create a client secret in Azure
  - Put secret in WireGuardUI Email Settings (`OAuth2 Client Secret`)
  - Consider disabling public client flow if you require strict confidential-only behavior

### 1.4 Required Scope

Use Microsoft SMTP delegated scope:

- `https://outlook.office.com/SMTP.Send`
- plus `offline_access` to receive refresh token

## 2. Get Refresh Token (Authorization Code Flow)

Use endpoint based on account type:

- Hotmail/Outlook.com: `consumers`
- Microsoft 365: `organizations` (or tenant GUID)

Example authorize URL (Hotmail):

```text
https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id=YOUR_CLIENT_ID&response_type=code&redirect_uri=http%3A%2F%2Flocalhost&scope=https%3A%2F%2Foutlook.office.com%2FSMTP.Send%20offline_access&response_mode=query&prompt=select_account
```

Exchange code for token (same tenant path as authorize):

```bash
curl -sS -X POST "https://login.microsoftonline.com/consumers/oauth2/v2.0/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "client_id=YOUR_CLIENT_ID" \
  --data-urlencode "grant_type=authorization_code" \
  --data-urlencode "code=PASTE_CODE_HERE" \
  --data-urlencode "redirect_uri=http://localhost" \
  --data-urlencode "scope=https://outlook.office.com/SMTP.Send offline_access"
```

Confidential client only:

```bash
--data-urlencode "client_secret=YOUR_CLIENT_SECRET"
```

Notes:

- Authorization code is one-time use and expires quickly.
- Do not double-encode code values (for example `%24%24` when already using `--data-urlencode`).
- Keep tenant endpoint consistent between authorize and token requests.

## 3. WireGuardUI Email Settings Mapping (Microsoft)

Set:

- Provider: `Outlook / Hotmail / Office 365`
- Auth Type: `OAuth2`
- SMTP Host: `smtp-mail.outlook.com`
- SMTP Port: `587`
- Encryption: `STARTTLS`
- From Address: mailbox you authenticated
- Email Account: same mailbox
- OAuth2 Client ID: Azure app client ID
- OAuth2 Client Secret:
  - leave empty for public client
  - set secret only for confidential client
- OAuth2 Refresh Token: token obtained in section 2
- Microsoft Account Type:
  - `consumers` for Hotmail/Outlook.com
  - `organizations` (or tenant GUID) for Microsoft 365

## 4. Microsoft 365 SMTP AUTH Requirement

For Microsoft 365 mailboxes, SMTP AUTH must be enabled at tenant and mailbox level.

Exchange Online PowerShell:

```powershell
Get-TransportConfig | fl SmtpClientAuthenticationDisabled
Set-TransportConfig -SmtpClientAuthenticationDisabled $false

Get-CASMailbox user@domain.com | fl SmtpClientAuthenticationDisabled
Set-CASMailbox user@domain.com -SmtpClientAuthenticationDisabled $false
```

## 5. Common Errors

- `AADSTS7000012`: code/token tenant mismatch
- `AADSTS70000`: invalid/expired/reused auth code
- `AADSTS90023` or `AADSTS700025`: public client must not send client secret
- `AADSTS9002331`: app is personal-only; must use `/consumers`
- `AADSTS40008`: federation/external identity provider issue in tenant
- `535 5.7.139`: SMTP AUTH disabled at tenant/mailbox
- `535 5.7.3`: SMTP auth unsuccessful (often account/token/host mismatch)

## 6. Security Notes

- Do not commit refresh tokens or client secrets.
- Prefer separate app registrations between environments.
- Rotate secrets/tokens immediately if exposed.

## 7. Gmail App Password

This is the simplest Gmail setup (no OAuth app required).

Prerequisites:

- Google account with 2-Step Verification enabled

Steps:

1. Open Google account security page: `https://myaccount.google.com/security`
2. Enable `2-Step Verification` if not already enabled
3. Open `App passwords`: `https://myaccount.google.com/apppasswords`
4. Generate an app password and copy the 16-character value
5. In WireGuardUI Email Settings set:
   - Provider: `Gmail`
   - Auth Type: `Password`
   - SMTP Host: `smtp.gmail.com`
   - SMTP Port: `587`
   - Encryption: `STARTTLS`
   - SMTP Username: your Gmail address
   - SMTP Password / App Password: generated app password

## 8. Gmail OAuth2 (Google Cloud)

### 9.1 Create Google Cloud project

1. Open `https://console.cloud.google.com/`
2. Use project selector -> `New Project` -> create/select project

### 9.2 Enable Gmail API

1. `APIs & Services` -> `Library`
2. Enable `Gmail API`

### 9.3 Configure OAuth consent screen

1. `APIs & Services` -> `OAuth consent screen`
2. Configure app details
3. If app is in Testing, add your Gmail account to `Test users`

### 9.4 Create OAuth client ID

1. `APIs & Services` -> `Credentials`
2. `Create Credentials` -> `OAuth client ID`
3. Application type: `Desktop app`
4. Save:
   - Client ID
   - Client Secret

### 9.5 Get refresh token

Open authorize URL:

```bash
open "https://accounts.google.com/o/oauth2/v2/auth?client_id=YOUR_GOOGLE_CLIENT_ID&redirect_uri=http%3A%2F%2Flocalhost&response_type=code&scope=https%3A%2F%2Fmail.google.com%2F&access_type=offline&prompt=consent"
```

After consent, copy `code` from redirected `http://localhost/?code=...` and exchange token:

```bash
curl -sS -X POST "https://oauth2.googleapis.com/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "client_id=YOUR_GOOGLE_CLIENT_ID" \
  --data-urlencode "client_secret=YOUR_GOOGLE_CLIENT_SECRET" \
  --data-urlencode "code=PASTE_CODE_HERE" \
  --data-urlencode "grant_type=authorization_code" \
  --data-urlencode "redirect_uri=http://localhost"
```

Use `refresh_token` from response in WireGuardUI Email Settings:

- Provider: `Gmail`
- Auth Type: `OAuth2`
- SMTP Host: `smtp.gmail.com`
- SMTP Port: `587`
- Encryption: `STARTTLS`
- Email Account / From Address: your Gmail address
- OAuth2 Client ID: Google client ID
- OAuth2 Client Secret: Google client secret
- OAuth2 Refresh Token: refresh token from token response

## 9. Yahoo App Password

Use App Password for Yahoo SMTP in WireGuardUI.

Steps:

1. Open Yahoo account security: `https://login.yahoo.com/account/security`
2. Enable 2-step verification if required
3. Generate `App password`
4. In WireGuardUI Email Settings set:
   - Provider: `Yahoo`
   - Auth Type: `Password`
   - SMTP Host: `smtp.mail.yahoo.com`
   - SMTP Port: `587`
   - Encryption: `STARTTLS`
   - SMTP Username: your Yahoo email
   - SMTP Password / App Password: generated app password

Troubleshooting:

- `#AUTH005 Too many bad auth attempts`: wait before retrying, then use a fresh app password.

## 10. Yahoo OAuth2 Status

Yahoo OAuth2 exists on Yahoo Developer, but WireGuardUI currently does not implement a Yahoo-specific SMTP OAuth token flow.

Current blockers/constraints:

- Yahoo developer app permissions may not expose mail scopes for all app profiles.
- WireGuardUI backend currently supports Microsoft/Google OAuth token endpoints only.

If Yahoo mail scopes are available in your developer app and OAuth support is required, implement Yahoo token endpoints:

- Authorization: `https://api.login.yahoo.com/oauth2/request_auth`
- Token/refresh: `https://api.login.yahoo.com/oauth2/get_token`
