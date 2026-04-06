# WireGuardUI

[![CI](https://img.shields.io/github/actions/workflow/status/klomkling/wireguard-ui-dotnet/ci.yml?branch=main&label=CI)](https://github.com/klomkling/wireguard-ui-dotnet/actions/workflows/ci.yml)
![Latest](https://img.shields.io/github/v/release/klomkling/wireguard-ui-dotnet?label=latest)
![.NET](https://img.shields.io/badge/.NET-10-blueviolet)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4)
![SQLite](https://img.shields.io/badge/SQLite-003B57)
![License](https://img.shields.io/github/license/klomkling/wireguard-ui-dotnet)
[![Sponsor](https://img.shields.io/badge/Sponsor-Buy%20me%20a%20coffee-ff813f)](https://buymeacoffee.com/klomkling)

WireGuardUI is a Blazor Server web app for managing WireGuard server settings, clients, and config distribution.

## Features

- Manage WireGuard clients (create, edit, enable/disable, delete)
- Generate and apply server configuration
- Import server/client `.conf` files
- Download client config files
- Send client config by email (SMTP password or OAuth2)
- Live status view from `wg show all dump`
- Cookie-based admin authentication with profile/password update
- SQLite persistence (Entity Framework Core)

## Tech Stack

- .NET 10 (`net10.0`)
- ASP.NET Core Blazor Server
- MudBlazor UI
- Entity Framework Core + SQLite
- MailKit (SMTP / OAuth2)

## Repository Layout

- `src/WireGuardUI.Core` - domain models and interfaces
- `src/WireGuardUI.Infrastructure` - data access, email, WireGuard integrations
- `src/WireGuardUI.Web` - Blazor web app
- `tests/WireGuardUI.Tests` - unit tests

## Prerequisites

- .NET 10 SDK
- (Linux/Windows production) WireGuard tools installed and accessible

## Quick Start

```bash
dotnet restore
dotnet test
dotnet run --project src/WireGuardUI.Web/WireGuardUI.Web.csproj
```

Default local URLs are configured in `src/WireGuardUI.Web/Properties/launchSettings.json`:

- `http://localhost:5000`
- `https://localhost:7000`

## Docker Test (WireGuard CLI Path)

If you want to test direct `wg` apply flow from the app on a machine without local WireGuard CLI in PATH (for example macOS DMG install), use:

- [docs/docker-wg-test.md](docs/docker-wg-test.md)

Docker test default URL: `http://localhost:5080`

## Configuration

Configure `WireGuardUI` in `appsettings.json` or environment-specific settings:

```json
"WireGuardUI": {
  "DbPath": "",
  "SessionExpiryHours": 8,
  "DefaultUsername": "",
  "DefaultPassword": "",
  "WireGuardConfigPath": "",
  "WireGuardExePath": ""
}
```

Notes:

- In production, set strong `DefaultUsername` and `DefaultPassword` before first run.
- In development, local defaults are provided by `appsettings.Development.json`.

## How To Use

See step-by-step usage guide: [docs/usage.md](docs/usage.md)

Includes:

- first login and bootstrap behavior
- initial server/global setup
- client management workflow
- apply config flow
- email sending workflow
- status and basic troubleshooting

## Installation and Deployment

See platform deployment details: [docs/deployment.md](docs/deployment.md)

Includes:

- Linux installation prerequisites and runtime paths
- Windows IIS deployment setup
- required writable folders and app pool filesystem permissions
- Windows tunnel/service permission notes for `Save & Apply Config`
- `HTTP 500.30` startup troubleshooting

## Email (OAuth2) Notes

See full provider setup guide: [docs/email-oauth.md](docs/email-oauth.md)

Includes:

- Azure Portal setup for Hotmail (personal) and Microsoft 365 accounts
- Gmail App Password setup
- Gmail OAuth2 setup (Google Cloud + refresh token flow)
- Public client vs confidential client (`OAuth2 Client Secret`) guidance
- Refresh token acquisition flow
- SMTP AUTH requirements for Exchange Online
- Troubleshooting common Microsoft OAuth/SMTP errors

## Support

If you like the project and want to support it:

[![Buy Me a Coffee](https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png)](https://buymeacoffee.com/klomkling)

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).

## Credits

- Inspired by and references: [ngoduykhanh/wireguard-ui](https://github.com/ngoduykhanh/wireguard-ui)
