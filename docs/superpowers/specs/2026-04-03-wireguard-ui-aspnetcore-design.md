# WireGuard UI — ASP.NET Core Redesign
**Date:** 2026-04-03  
**Reference:** [ngoduykhanh/wireguard-ui v0.6.2](https://github.com/ngoduykhanh/wireguard-ui/tree/v0.6.2)

---

## Overview

A web-based WireGuard VPN management interface rebuilt in ASP.NET Core (.NET 10) using Blazor Server. Feature scope: core client management, server configuration, global settings, email delivery of client configs, user profile/password management, and live status page.

---

## Architecture

### Solution Structure — 3 Projects

```
WireGuardUI.sln
├── WireGuardUI.Core           # Domain models + interfaces (no external dependencies)
├── WireGuardUI.Infrastructure # EF Core, WireGuard shell, email, crypto
└── WireGuardUI.Web            # Blazor Server — pages, components, DI wiring
```

**Dependency rule:** `Web` → `Infrastructure` → `Core`. Web references Infrastructure only via injected interfaces defined in Core. This keeps business logic testable without the UI or database.

### Technology Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 |
| UI Framework | Blazor Server |
| ORM | Entity Framework Core 10 + SQLite |
| UI Component Library | MudBlazor |
| Authentication | ASP.NET Core Cookie Auth (no ASP.NET Identity) |
| Password Hashing | BCrypt.Net-Next |
| Email | MailKit |
| QR Codes | QRCoder |
| Crypto (WG keys) | BouncyCastle.Cryptography (Curve25519) |

---

## Data Models

All entities stored in SQLite via EF Core. `List<string>` fields use EF Core JSON column serialization (no junction tables).

### WireGuardClient
| Field | Type | Notes |
|---|---|---|
| Id | string | GUID, PK |
| Name | string | Display name |
| Email | string? | For config delivery (optional) |
| PrivateKey | string | WireGuard private key |
| PublicKey | string | WireGuard public key |
| PresharedKey | string | WireGuard preshared key |
| AllocatedIPs | List\<string\> | JSON column, e.g. `["10.252.1.2/32"]` |
| AllowedIPs | List\<string\> | JSON column, default `["0.0.0.0/0"]` |
| ExtraAllowedIPs | List\<string\> | JSON column, optional |
| Endpoint | string? | Override endpoint for this client |
| UseServerDns | bool | Whether client uses server DNS |
| Enabled | bool | Included in wg0.conf when true |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime | |

### ServerConfig *(single row, Id = 1)*
| Field | Type | Notes |
|---|---|---|
| PrivateKey | string | Server WireGuard private key |
| PublicKey | string | Server WireGuard public key |
| Addresses | List\<string\> | JSON column, default `["10.252.1.0/24"]` |
| ListenPort | int | Default 51820 |
| PostUp | string? | iptables/nft rules |
| PreDown | string? | |
| PostDown | string? | |
| UpdatedAt | DateTime | |

### GlobalSetting *(single row, Id = 1)*
| Field | Type | Notes |
|---|---|---|
| EndpointAddress | string | Public IP/hostname for clients |
| DnsServers | List\<string\> | JSON column, default `["1.1.1.1"]` |
| Mtu | int | Default 1450 |
| PersistentKeepalive | int | Default 15 seconds |
| FirewallMark | string | Default `"0xca6c"` |
| Table | string | Routing table |
| ConfigFilePath | string | Default `"/etc/wireguard/wg0.conf"` |
| UpdatedAt | DateTime | |

### AdminUser *(single row, Id = 1)*
| Field | Type | Notes |
|---|---|---|
| Username | string | Default `"admin"` |
| PasswordHash | string | BCrypt hash |
| UpdatedAt | DateTime | |

### EmailSetting *(single row, Id = 1)*
| Field | Type | Notes |
|---|---|---|
| SmtpHost | string | |
| SmtpPort | int | |
| SmtpUsername | string | |
| SmtpPassword | string | |
| FromAddress | string | |
| Encryption | string | `None` \| `SSL` \| `TLS` \| `STARTTLS` |
| UpdatedAt | DateTime | |

---

## Pages & Navigation

All pages except `/login` are protected by `AuthorizeRouteView`. Unauthenticated requests redirect to `/login`.

### Routes

| Page | Route | Purpose |
|---|---|---|
| Login | `/login` | Username/password entry |
| Clients | `/` | Main dashboard (default after login) |
| Server | `/server` | Server interface settings + keypair |
| Global Settings | `/global-settings` | Endpoint, DNS, MTU, keepalive, firewall mark |
| Email Settings | `/email-settings` | SMTP configuration + test send |
| Status | `/status` | Live `wg show` output — peers, handshakes, rx/tx |
| Profile | `/profile` | Change username and password |

### Page Details

**Clients (`/`)**
- Table of all clients with name, email, allocated IP, enabled toggle
- Search/filter by name or email
- Add/Edit client via `ClientDialog` (MudBlazor dialog)
- Per-client actions: download `.conf`, show QR code (`QrCodeDialog`), send via email (`EmailDialog`), delete
- "Apply Config" button triggers the full apply flow
- Bulk enable/disable

**Server (`/server`)**
- Edit interface addresses, listen port, PostUp/PreDown/PostDown
- Regenerate keypair button → `ConfirmDialog` warning (all clients will need reconfiguring)
- "Apply Config" button

**Global Settings (`/global-settings`)**
- Edit endpoint address, DNS servers (comma-separated), MTU, persistent keepalive, firewall mark, table, config file path
- Explicit "Apply Config" button (same as Clients and Server pages — consistent behavior across all pages)

**Email Settings (`/email-settings`)**
- SMTP host, port, username, password, from address, encryption dropdown
- "Test Connection" → sends test email to admin, shows result snackbar

**Status (`/status`)**
- Calls `wg show wg0 dump` via `IWireGuardService.GetStatusAsync()`
- Displays interface info + peer table (public key, endpoint, allowed IPs, last handshake, rx/tx)
- Auto-refreshes every 10 seconds via `System.Threading.Timer`

**Profile (`/profile`)**
- Change username field
- Change password: current password + new password + confirm
- Validates current password via BCrypt before saving new hash

### Shared Components

| Component | Purpose |
|---|---|
| `MainLayout.razor` | Sidebar nav + top bar with user info/logout |
| `NavMenu.razor` | Navigation links with active state |
| `ConfirmDialog.razor` | Generic yes/no confirmation modal |
| `NotificationSnackbar.razor` | Wraps MudBlazor Snackbar for app-wide notifications |
| `LoadingOverlay.razor` | Spinner overlay during async operations |
| `QrCodeDisplay.razor` | Renders QR code PNG from QRCoder as base64 img |

---

## Services & Infrastructure

### Core Interfaces (`WireGuardUI.Core/Interfaces/`)

```csharp
IClientRepository
  GetAllAsync() / GetByIdAsync(id) / AddAsync(client) / UpdateAsync(client) / DeleteAsync(id)

IServerConfigRepository
  GetAsync() / SaveAsync(config)

IGlobalSettingRepository
  GetAsync() / SaveAsync(setting)

IEmailSettingRepository
  GetAsync() / SaveAsync(setting)

IWireGuardService
  GenerateConfigAsync()  → string        // builds wg0.conf content
  WriteConfigAsync()     → Task          // writes file atomically
  SyncConfAsync()        → ApplyResult   // wg syncconf + fallback
  GetStatusAsync()       → WireGuardStatus

// ApplyResult record (in Core/Models/):
//   bool Success, string? ErrorMessage, string? RawOutput

// WireGuardStatus record (in Core/Models/):
//   string InterfaceName, string PublicKey, string ListenPort
//   List<WireGuardPeer> Peers
//     WireGuardPeer: PublicKey, Endpoint, AllowedIPs, LastHandshake, RxBytes, TxBytes

IEmailService
  SendClientConfigAsync(client, toEmail) → Task
  TestConnectionAsync()  → bool

IKeyPairService
  GenerateKeyPair()      → (PrivateKey, PublicKey)
  GeneratePresharedKey() → string

IIpAllocationService
  AllocateNextIp(serverSubnet, existingIps) → string   // pure, no DB dependency
```

### WireGuard Config Apply Flow

1. **Generate** — build `wg0.conf` string from `ServerConfig` + all enabled `WireGuardClient` records
2. **Write** — write atomically to `ConfigFilePath` (write to `.tmp` file, then `File.Move` with overwrite)
3. **Sync** — platform-specific (see below); return `ApplyResult` with success/failure and stderr output

Two concrete implementations of `IWireGuardService` in `WireGuardUI.Infrastructure/WireGuard/`:

| | `LinuxWireGuardService` | `WindowsWireGuardService` |
|---|---|---|
| Primary sync | `wg syncconf wg0 <path>` | `wg syncconf wg0 <path>` |
| Fallback | `wg-quick down wg0 && wg-quick up wg0` | `net stop WireGuardTunnel$wg0` → `net start` |
| `wg` binary | `/usr/bin/wg` (in PATH) | `C:\Program Files\WireGuard\wg.exe` (configurable) |
| Default config path | `/etc/wireguard/wg0.conf` | `C:\Program Files\WireGuard\wg0.conf` |
| Default DB path | `/var/lib/wireguard-ui/wireguard-ui.db` | `C:\ProgramData\WireGuardUI\wireguard-ui.db` |

Registration in `Program.cs`:
```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    services.AddScoped<IWireGuardService, WindowsWireGuardService>();
else
    services.AddScoped<IWireGuardService, LinuxWireGuardService>();
```

All other services (repositories, email, key generation, IP allocation) are fully cross-platform.

### Key Generation

Uses **BouncyCastle.Cryptography** (Curve25519/X25519) for key generation — no dependency on the `wg` binary for key operations. The `IKeyPairService` implementation shells out to `wg genkey`/`wg pubkey` only as a fallback if BouncyCastle is unavailable.

### Authentication

- `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)` in `Program.cs`
- Login: load `AdminUser` from DB → verify BCrypt → `HttpContext.SignInAsync()` → redirect to `/`
- Logout: `HttpContext.SignOutAsync()` → redirect to `/login`
- Password change: verify current password → BCrypt new password → save → re-sign-in cookie
- Session: sliding expiry (default 8 hours), configurable via `appsettings.json`

### Error Handling

- All service methods return `Result<T>` (success/failure + message) — no exceptions leak to Blazor UI
- WireGuard shell failures → error shown via MudBlazor Snackbar; config file still downloadable
- Email failures → error snackbar; download fallback always available
- Each page wrapped in Blazor `<ErrorBoundary>` component for unhandled exceptions

### IP Allocation

`IpAllocationService` (Core layer): given a server subnet CIDR and a list of already-allocated IPs, returns the next available host IP. Pure method — no DB or I/O dependency, trivially unit-testable.

---

## Configuration (`appsettings.json`)

Platform-specific defaults are applied automatically at startup if not overridden. Only the fields that differ from defaults need to be set.

```json
{
  "WireGuardUI": {
    "DbPath": "",                  // default: platform-specific (see table above)
    "SessionExpiryHours": 8,
    "DefaultUsername": "admin",
    "DefaultPassword": "admin",
    "WireGuardTunnelName": "wg0",
    "WireGuardConfigPath": "",     // default: platform-specific
    "WireGuardExePath": ""         // default: platform-specific
  }
}
```

All fields overridable via environment variables (`WireGuardUI__DbPath`, etc.).

`DefaultUsername` and `DefaultPassword` are used only on first run to seed the `AdminUser` record. The password is BCrypt-hashed before being stored — plaintext is never persisted.

---

## Deployment

Supports **Windows** and **Linux**. The correct `IWireGuardService` implementation is selected automatically at runtime.

### Windows

**Option A — Kestrel (direct)**
- `dotnet publish` → self-contained executable, run as a Windows Service (`sc create`) or directly
- Kestrel listens on a configured port (e.g. `http://localhost:5000`)

**Option B — IIS**
- Publish to a folder, configure an IIS site pointing to it
- ASP.NET Core Module v2 (ANCM) in-process hosting; `web.config` auto-generated by `dotnet publish`
- .NET 10 Hosting Bundle must be installed on the server

**Windows prerequisites:**
- WireGuard for Windows installed
- WireGuard tunnel (`wg0`) already installed as a Windows Service
- Process identity must have Administrator rights

### Linux

- `dotnet publish` → self-contained executable, run as a systemd service
- Or behind a reverse proxy (nginx, Caddy) with Kestrel

**Linux prerequisites:**
- WireGuard installed (`wg`, `wg-quick` available in PATH)
- Run as root or with `CAP_NET_ADMIN`
- SQLite DB directory must be writable

---

## Out of Scope

- Wake-on-LAN host management
- Telegram bot integration
- Multi-user / role-based access
- OAuth / external identity providers
