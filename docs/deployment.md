# WireGuardUI Deployment Guide

This document covers installation and runtime configuration for Linux and Windows (IIS), including required permissions for applying WireGuard config.

## 1. Core Configuration

Configure `WireGuardUI` via `appsettings.json` or environment variables:

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

Environment variable format (double underscore):

- `WireGuardUI__DbPath`
- `WireGuardUI__DefaultUsername`
- `WireGuardUI__DefaultPassword`
- `WireGuardUI__WireGuardConfigPath`
- `WireGuardUI__WireGuardExePath`

Production bootstrap note:

- If DB has no admin user, app requires `DefaultUsername` and `DefaultPassword` on startup.

## 2. Linux Deployment

## 2.1 Prerequisites

- .NET runtime (matching target framework)
- `wg` and `wg-quick` available in PATH
- Permissions to write WireGuard config path (default `/etc/wireguard/wg0.conf`)
- Permissions/capabilities to manage interface (`wg syncconf` / `wg-quick`)

## 2.2 Recommended paths

- DB: `/var/lib/wireguard-ui/wireguard-ui.db`
- Config: `/etc/wireguard/wg0.conf`

## 2.3 Permission checks

```bash
which wg
which wg-quick
ls -l /etc/wireguard
```

If apply fails, capture exact output from UI and server logs.

## 3. Windows Deployment (IIS)

## 3.1 Prerequisites

- ASP.NET Core Hosting Bundle installed
- WireGuard for Windows installed
- IIS site + app pool configured

## 3.2 Recommended paths (writable)

- DB: `C:\ProgramData\WireGuardUI\wireguard-ui.db`
- Config: `C:\ProgramData\WireGuardUI\wireguard\<TunnelName>.conf`

Do not use `C:\Program Files\...` for writable app files under IIS.

## 3.3 IIS environment variables

Set on the IIS site (`system.webServer/aspNetCore`):

- `WireGuardUI__DbPath=C:\ProgramData\WireGuardUI\wireguard-ui.db`
- `WireGuardUI__WireGuardConfigPath=C:\ProgramData\WireGuardUI\wireguard\Csith-server.conf` (example)
- `WireGuardUI__WireGuardExePath=C:\Program Files\WireGuard\wg.exe`
- `WireGuardUI__DefaultUsername=<bootstrap-admin-user>` (first startup only when no admin exists)
- `WireGuardUI__DefaultPassword=<strong-bootstrap-password>` (first startup only when no admin exists)

## 3.4 App Pool filesystem permissions

Grant Modify to app pool identity for writable folders:

```powershell
$appPool = "WireGuardUI"
$base = "C:\ProgramData\WireGuardUI"
New-Item -ItemType Directory -Force -Path "$base\wireguard" | Out-Null
icacls $base /grant "IIS AppPool\$appPool:(OI)(CI)M" /T
```

## 3.5 Service control permissions (Apply Config)

`Save & Apply Config` on Windows may need rights to:

- run `wg.exe`
- control `WireGuardTunnel$<TunnelName>` service
- install tunnel service (first run), via `wireguard.exe /installtunnelservice`

If you get `System error 5` (Access denied), app pool identity is not privileged enough for service operations.

Options:

1. Quick validation: run app pool with a privileged identity (for test only).
2. Production hardening: keep least privilege and delegate service-control rights explicitly.
3. Operational fallback: write config from app, then restart tunnel service manually as admin.

## 3.6 Tunnel name mapping (important)

On Windows, tunnel service name is derived from config filename:

- Config file `Csith-server.conf` -> service `WireGuardTunnel$Csith-server`
- Config file `wg0.conf` -> service `WireGuardTunnel$wg0`

If service exists with a different name, align `WireGuardConfigPath` filename to that tunnel.

## 3.7 Manual tunnel service install (admin)

```powershell
"C:\Program Files\WireGuard\wireguard.exe" /installtunnelservice "C:\ProgramData\WireGuardUI\wireguard\Csith-server.conf"
```

## 4. Startup Failure (`HTTP 500.30`)

If IIS shows `HTTP Error 500.30`:

1. Enable stdout logs in `web.config` (`stdoutLogEnabled="true"`).
2. Create log folder and grant write permission to app pool identity.
3. Recycle app pool and check latest `stdout` log file.

Common causes:

- missing bootstrap admin credentials when DB has no admin
- no write permission to DB/config directories
- missing .NET Hosting Bundle/runtime

