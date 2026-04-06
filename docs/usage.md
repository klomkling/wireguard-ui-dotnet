# WireGuardUI Usage Guide

This guide covers day-to-day usage of WireGuardUI after installation.

## 1. Start the App

```bash
dotnet run --project src/WireGuardUI.Web/WireGuardUI.Web.csproj
```

Open:

- `http://localhost:5000`
- or `https://localhost:7000`

## 2. First Login

On first run, if no admin account exists:

- Development environment: bootstrap defaults are available (`admin` / `admin`) unless you override config.
- Production environment: set `WireGuardUI:DefaultUsername` and `WireGuardUI:DefaultPassword` to strong values before first start.

After login, change credentials from `Profile`.

## 3. Initial Configuration

Set these pages before adding many clients:

1. `Server`
- Server key pair
- Interface addresses/subnet
- Listen port
- Optional PostUp/PreDown/PostDown hooks
- Import server config from `.conf` (via file picker on the Server page)

2. `Global Settings`
- Public endpoint address used by clients
- DNS servers
- MTU / keepalive
- WireGuard config file path

Use `Save` first, then `Save & Apply Config` when ready to update the runtime config.

## 4. Manage Clients

From `Clients` page:

- Create new client (`New Client`)
- Edit existing client
- Enable/disable client
- Delete client
- Download client config (`.conf`)
- Import one or many client config files
- Send config via email using the email address saved on each client

For each client, verify:

- Allocated IP(s)
- Allowed IPs
- Optional email address

## 5. Apply WireGuard Config

Two common ways:

1. From `Clients` -> `Apply Config`
2. From `Server` or `Global Settings` -> `Save & Apply Config`

Behavior depends on environment:

- Linux/Windows production: real WireGuard service integration
- Development/macOS: no-op apply service is used (safe for UI testing without changing host WireGuard state)

Server import note:

- Importing on `Server` updates server/interface values (and related global settings like DNS/MTU when present) from the selected config file.
- Import does not automatically reload interface state; use `Save & Apply Config` after import when you want changes applied to WireGuard runtime.

## 6. Email Settings

1. Configure provider in `Email Settings`
2. Click `Test Connection`

Provider setup references:

- [Email OAuth Setup](email-oauth.md)

## 7. Check Runtime Status

Use `Status` page to view:

- Interface details
- Peer list
- Handshake and transfer counters

Status auto-refreshes every 10 seconds.

## 8. Admin Operations

- `Profile` page: change username/password
- Top-right logout button: end current session

## 9. Troubleshooting Basics

- Run tests:
```bash
dotnet test
```
- Verify app config in:
  - `src/WireGuardUI.Web/appsettings.json`
  - `src/WireGuardUI.Web/appsettings.Development.json`
- If email fails, capture exact provider error text from UI and compare with:
  - [Email OAuth Setup](email-oauth.md)
- For installation, IIS/App Pool permissions, and deployment-specific troubleshooting:
  - [Deployment Guide](deployment.md)
