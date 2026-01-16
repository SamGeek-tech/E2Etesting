# Pact Runbook (Self-hosted Broker + Tests + CI)

This repo uses **Pact** consumer-driven contract testing with a **self-hosted Pact Broker** (Docker + PostgreSQL). This document consolidates the previous Pact docs into a single place.

## What you get

- **Consumer tests** generate pact JSON files into `tests/*/pacts/`
- **Publishing** uploads pact files to the broker (via REST API; no CLI dependency)
- **Provider verification tests** fetch pacts from the broker and verify the real APIs
- **Can-I-Deploy** checks deployment safety using broker Matrix API
- **Works locally and in CI** (Azure Pipelines)

## Key URLs

- **Pact Broker UI**: `http://localhost:9292`
- **Pact Broker health**: `http://localhost:9292/diagnostic/status/heartbeat`
- **PostgreSQL**: `localhost:5432` (used by broker)

## Environment variables

### Broker auth (choose one)

- **Self-hosted (basic auth)**:
  - `PACT_BROKER_BASE_URL` (example: `http://localhost:9292`)
  - `PACT_BROKER_USERNAME` (example: `admin`)
  - `PACT_BROKER_PASSWORD` (example: `admin`)

- **PactFlow (token auth)**:
  - `PACT_BROKER_BASE_URL`
  - `PACT_BROKER_TOKEN`

### Provider verification

- `PROVIDER_VERSION` (recommended: CI build id)
- `PUBLISH_VERIFICATION_RESULTS` (`true` to publish results)

## Local workflow (recommended)

### 1) Start the broker

```powershell
.\start-pact-broker.ps1
```

Open `http://localhost:9292` (default `admin/admin`).

### 2) Configure broker env vars

```powershell
$env:PACT_BROKER_BASE_URL="http://localhost:9292"
$env:PACT_BROKER_USERNAME="admin"
$env:PACT_BROKER_PASSWORD="admin"
```

### 3) Run consumer tests (generate pacts)

```powershell
# OrderService consumer tests
dotnet test tests/OrderService.Contract.Tests/OrderService.Contract.Tests.csproj --filter "OrderClientTests"

# InventoryService consumer tests
dotnet test tests/InventoryService.Contract.Tests/InventoryService.Contract.Tests.csproj --filter "InventoryClientTests"
```

### 4) Publish pacts to the broker

```powershell
.\publish-pacts.ps1 -Version "1.0.0" -Branch "main"
```

Notes:
- Script publishes via broker REST API:
  - `PUT /pacts/provider/{provider}/consumer/{consumer}/version/{version}`
- Script tags the **consumer version** with `-Branch`.

### 5) Run provider verification tests (publish results)

```powershell
$env:PROVIDER_VERSION="1.0.0"
$env:PUBLISH_VERIFICATION_RESULTS="true"

# OrderService provider verification
dotnet test tests/OrderService.Contract.Tests/OrderService.Contract.Tests.csproj --filter "OrderApiProviderTests"

# InventoryService provider verification
dotnet test tests/InventoryService.Contract.Tests/InventoryService.Contract.Tests.csproj --filter "InventoryApiProviderTests"
```

### 6) Can-I-Deploy check (matrix)

```powershell
.\can-i-deploy.ps1 -Pacticipant "OrderClientSdk" -Version "1.0.0"
.\can-i-deploy.ps1 -Pacticipant "InventoryClientSdk" -Version "1.0.0"
```

## Broker management

```powershell
.\start-pact-broker.ps1 -Action start
.\start-pact-broker.ps1 -Action status
.\start-pact-broker.ps1 -Action logs
.\start-pact-broker.ps1 -Action stop
.\start-pact-broker.ps1 -Action clean   # WARNING: deletes broker data
```

## Known “gotchas” (and fixes already applied)

- **No Ruby/CLI dependency**: publishing and can-i-deploy use REST API from PowerShell.
- **PowerShell encoding**: scripts avoid emojis to prevent parsing issues on Windows.
- **Provider state failures** usually mean the provider state endpoint is unreachable:
  - Ensure provider APIs actually run on the port the provider tests use (`--urls`).
  - Avoid hardcoded Kestrel endpoints in `appsettings.json` that override `--urls`.

## CI/CD (Azure DevOps)

The pipeline (`azure-pipelines-ghazdo.yml`) is set up to:
- Run consumer tests
- Publish pacts via `publish-pacts.ps1`
- Run provider verifications
- Run can-i-deploy via `can-i-deploy.ps1`

### Pipeline variables (recommended)

- `PactBrokerUrl`
- For self-hosted: `PactBrokerUsername`, `PactBrokerPassword` (secret)
- For PactFlow: `PactBrokerToken` (secret)

## Troubleshooting quick checklist

- **Broker UI not reachable**: `.\start-pact-broker.ps1 -Action status` and broker health endpoint.
- **Publish returns 409 Conflict**: version already exists in broker (use a new `-Version` or accept that it’s already published).
- **“No contracts found” in can-i-deploy**: consumer pacts not published for that version/participant.
- **Provider verification shows setup state handler failed**: `/provider-states` not reachable or provider isn’t running on expected port.

