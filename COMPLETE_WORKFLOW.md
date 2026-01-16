# Complete Pact Workflow - Step by Step

## Overview

This guide shows the complete contract testing workflow from consumer tests to deployment checks.

## Prerequisites

```powershell
# Start Pact Broker
.\start-pact-broker.ps1

# Set environment variables
$env:PACT_BROKER_BASE_URL="http://localhost:9292"
$env:PACT_BROKER_USERNAME="admin"
$env:PACT_BROKER_PASSWORD="admin"
```

## Step 1: Consumer Tests (Generate Pacts)

Consumer tests define what the consumer expects from the provider.

```powershell
# Run consumer tests - generates pact files
dotnet test tests/OrderService.Contract.Tests/OrderService.Contract.Tests.csproj --filter "OrderClientTests"

# Output: tests/OrderService.Contract.Tests/pacts/OrderClientSdk-OrderServiceApi.json
```

**What this does:**
- Consumer (OrderClientSdk) defines expectations of Provider (OrderServiceApi)
- Generates pact JSON file with all interactions
- Tests pass if mock provider meets expectations

## Step 2: Publish Pacts to Broker

Publish the generated pacts to the broker so providers can verify against them.

```powershell
.\publish-pacts.ps1 -Version "1.0.0" -Branch "main"
```

**What this does:**
- Uploads pact files to broker via REST API
- Tags version with branch name
- Makes contracts available for provider verification

**Verify in browser:** http://localhost:9292

## Step 3: Check Deployment Readiness (Before Provider Tests)

```powershell
.\can-i-deploy.ps1 -Pacticipant "OrderClientSdk" -Version "1.0.0"
```

**Expected Result:**
```
[BLOCKED] NOT SAFE TO DEPLOY
OrderClientSdk v1.0.0 cannot be deployed to production
Some contracts are not verified or have verification failures
```

This is correct! Provider hasn't verified the contract yet.

## Step 4: Provider Verification Tests

Provider tests verify that the actual API implementation matches the consumer's expectations.

### 4a: Start the Provider API

Make sure OrderService.Api is running:

```powershell
# Check what port OrderService API runs on
# Look in src/OrderService.Api/Program.cs or Properties/launchSettings.json

# Start the API (example)
cd src/OrderService.Api
dotnet run
```

### 4b: Run Provider Verification

```powershell
$env:PROVIDER_VERSION="1.0.0"
$env:PUBLISH_VERIFICATION_RESULTS="true"

dotnet test tests/OrderService.Contract.Tests/OrderService.Contract.Tests.csproj --filter "OrderApiProviderTests"
```

**What this does:**
- Fetches pacts from broker for OrderServiceApi provider
- Starts OrderService.Api (or connects to running instance)
- Replays all consumer interactions against real API
- Publishes verification results back to broker

## Step 5: Check Deployment Readiness (After Provider Tests)

```powershell
.\can-i-deploy.ps1 -Pacticipant "OrderClientSdk" -Version "1.0.0"
```

**Expected Result (if verification passed):**
```
[OK] SAFE TO DEPLOY
OrderClientSdk v1.0.0 can be deployed to production
All contracts have been verified
```

## Understanding the Roles

### Consumer (OrderClientSdk)
- **Creates** the contract (pact)
- Defines expectations
- **Question:** "Can I deploy? Are my providers verified?"

### Provider (OrderServiceApi)
- **Verifies** the contract
- Proves it meets expectations
- **Question:** "Do I satisfy all my consumers?"

## Can-I-Deploy: Who Should Ask?

### ✅ Consumers Ask "Can I Deploy?"
```powershell
# Consumer checking if provider dependencies are verified
.\can-i-deploy.ps1 -Pacticipant "OrderClientSdk" -Version "1.0.0"
```

**Checks:** Has OrderServiceApi verified this version's pact?

### ⚠️ Providers Can Check Too
```powershell
# Provider checking if they break any consumers
.\can-i-deploy.ps1 -Pacticipant "OrderServiceApi" -Version "1.0.0"
```

**Checks:** Have all consumers that depend on me been verified with my changes?

## Complete Example Workflow

```powershell
# 1. Setup
.\start-pact-broker.ps1
$env:PACT_BROKER_BASE_URL="http://localhost:9292"
$env:PACT_BROKER_USERNAME="admin"
$env:PACT_BROKER_PASSWORD="admin"

# 2. Consumer: Create & Publish Pacts
dotnet test tests/OrderService.Contract.Tests/OrderService.Contract.Tests.csproj --filter "OrderClientTests"
.\publish-pacts.ps1 -Version "1.0.0" -Branch "main"

# 3. Check (should be blocked)
.\can-i-deploy.ps1 -Pacticipant "OrderClientSdk" -Version "1.0.0"

# 4. Provider: Verify Pacts
# (Start API first if not running)
$env:PROVIDER_VERSION="1.0.0"
$env:PUBLISH_VERIFICATION_RESULTS="true"
dotnet test tests/OrderService.Contract.Tests/OrderService.Contract.Tests.csproj --filter "OrderApiProviderTests"

# 5. Check again (should be approved)
.\can-i-deploy.ps1 -Pacticipant "OrderClientSdk" -Version "1.0.0"

# 6. View in browser
start http://localhost:9292
```

## Troubleshooting

### Pacts Not Found
```
[WARNING] NO CONTRACTS FOUND
```

**Solutions:**
- Run consumer tests first
- Check pacticipant name spelling
- Verify pacts were published: check http://localhost:9292

### Not Verified
```
Status: NOT VERIFIED
```

**Solutions:**
- Run provider verification tests
- Make sure API is running on correct port
- Check provider test configuration
- Ensure `PUBLISH_VERIFICATION_RESULTS="true"`

### Verification Failed
```
Status: FAILED
```

**Solutions:**
- Check provider test output for specific failures
- API implementation may not match consumer expectations
- Update either provider implementation or consumer expectations
- Re-run tests after fixes

## CI/CD Integration

The Azure pipeline (`azure-pipelines-ghazdo.yml`) already includes:

1. **Consumer Tests Stage**: Runs consumer tests and publishes pacts
2. **Provider Tests Stage**: Runs provider verification
3. **Can-I-Deploy Check**: Validates deployment readiness
4. **Deployment Stage**: Only proceeds if can-i-deploy passes

## Key Benefits

✅ **Catch Breaking Changes Early** - Before deployment
✅ **Independent Deployment** - Consumer and provider can deploy separately
✅ **Contract Versioning** - Track contract changes over time
✅ **Automated Safety Checks** - Can-i-deploy prevents breaking deployments
✅ **Documentation** - Pacts document API usage

## Next Steps

1. ✅ Consumer tests passing
2. ✅ Pacts published to broker
3. ✅ Can-i-deploy script working
4. ⏳ **Next:** Run provider verification tests
5. ⏳ **Then:** Full deployment approval workflow

## Quick Reference

| Command | Purpose |
|---------|---------|
| `.\start-pact-broker.ps1` | Start broker |
| `dotnet test ... --filter "OrderClientTests"` | Generate pacts |
| `.\publish-pacts.ps1 -Version "X"` | Upload to broker |
| `.\can-i-deploy.ps1 -Pacticipant "X" -Version "Y"` | Check deployment |
| `dotnet test ... --filter "OrderApiProviderTests"` | Verify as provider |

## Documentation

- `PACT_RUNBOOK.md` - Pact broker + tests + CI runbook (single source)
- `COMPLETE_WORKFLOW.md` - This file
