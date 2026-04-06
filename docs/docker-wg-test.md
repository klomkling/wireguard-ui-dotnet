# Docker Test for `wg` Apply Path

Use this setup to test WireGuardUI with Linux `wg` / `wg-quick` tools available when your local machine (for example macOS DMG install) does not provide CLI binaries in PATH.

## 1. Prerequisites

- Docker Desktop (or Docker Engine) with Compose
- Ability to run containers with `NET_ADMIN` capability

## 2. Start Test Stack

From repo root:

```bash
mkdir -p docker-data/db docker-data/conf
docker compose -f docker-compose.wg-test.yml up --build -d
```

Open:

- `http://localhost:5080`

Default host port is `5080` to avoid common macOS/AirPlay conflicts on `5000`.
You can override it with:

```bash
WG_UI_HOST_PORT=5001 docker compose -f docker-compose.wg-test.yml up --build -d
```

## 3. Default Login (Container Test Only)

Configured in `docker-compose.wg-test.yml`:

- Username: `admin`
- Password: `ChangeThisBeforeExposure123!`

Change it after first login (`Profile` page), especially if exposed beyond local machine.

## 4. Verify Tooling Inside Container

```bash
docker exec -it wireguard-ui-wg-test wg --version
docker exec -it wireguard-ui-wg-test wg-quick --version
```

## 5. Test Apply Config Path

1. Configure `Server` and `Global Settings` in the UI.
2. Click `Save & Apply Config` (or `Clients` -> `Apply Config`).
3. Inspect runtime output/logs:

```bash
docker logs -f wireguard-ui-wg-test
```

## 6. Notes for macOS Docker Desktop

- The container has `wg` CLI, but creating/controlling real WireGuard interfaces depends on the Linux kernel in Docker Desktop VM.
- If kernel WireGuard support is unavailable, apply may fail even though CLI exists.
- In that case, use a Linux VM/host with WireGuard kernel support for full end-to-end runtime validation.

## 7. Stop / Cleanup

```bash
docker compose -f docker-compose.wg-test.yml down
```

To remove test data:

```bash
rm -rf docker-data
```
