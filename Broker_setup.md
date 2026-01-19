# Pact Broker (Self-Hosted) on Azure Container Apps — Step-by-Step

This document explains how to deploy a **self-hosted Pact Broker** + **PostgreSQL** as containers in **Azure Container Apps**, and how to connect your Azure DevOps pipeline to it.

---

## Prerequisites

- Azure CLI installed and logged in:
  - `az login`
- The `containerapp` extension installed/updated:
  - `az extension add --name containerapp --upgrade`
- A resource group name and region (example uses **centralus**)

---

## 1) Create Resource Group

```bash
az group create -n rg-pact -l centralus
```

## 2) Create Container Apps Environment

```bash
az containerapp env create \
  -g rg-pact \
  -n cae-pact \
  -l centralus
```

## 3) Deploy PostgreSQL as a Container App (Internal + TCP 5432)

### 3.1 Create Postgres app

Pick a strong password (example only):

```bash
POSTGRES_PASSWORD="123"
```

Create the Postgres container:

```bash
az containerapp create \
  -g rg-pact \
  -n pact-postgres \
  --environment cae-pact \
  --image postgres:15 \
  --ingress internal \
  --target-port 5432 \
  --min-replicas 1 --max-replicas 1 \
  --cpu 0.5 --memory 1Gi \
  --env-vars \
    POSTGRES_DB=pact_broker \
    POSTGRES_USER=pact_broker \
    POSTGRES_PASSWORD=$POSTGRES_PASSWORD
```

### 3.2 IMPORTANT: ensure Postgres ingress uses TCP and exposes port 5432

Azure Container Apps may default the ingress transport to Auto. Postgres needs TCP.

Enable TCP explicitly:

```bash
az containerapp ingress enable \
  -g rg-pact \
  -n pact-postgres \
  --type internal \
  --transport tcp \
  --target-port 5432 \
  --exposed-port 5432
```

Verify:

```bash
az containerapp show -g rg-pact -n pact-postgres --query "properties.configuration.ingress" -o jsonc
```

You should see something like:

```json
"transport": "Tcp"
"exposedPort": 5432
"targetPort": 5432
"external": false
```

## 4) Deploy Pact Broker as a Container App (External)

### 4.1 Choose broker basic-auth credentials

Example only:

```bash
PACT_BROKER_USERNAME="admin"
PACT_BROKER_PASSWORD="admin123!"
```

### 4.2 Create the Pact Broker container app

Key point: use the Postgres service name as host:

```bash
PACT_BROKER_DATABASE_HOST=pact-postgres
```

Also add DB retries; on cold start Postgres may not be ready immediately:

```bash
PACT_BROKER_DATABASE_CONNECT_MAX_RETRIES=10
```

Create the broker:

```bash
az containerapp create \
  -g rg-pact \
  -n pact-broker \
  --environment cae-pact \
  --image pactfoundation/pact-broker:latest \
  --ingress external \
  --target-port 9292 \
  --min-replicas 1 --max-replicas 1 \
  --cpu 0.5 --memory 1Gi \
  --env-vars \
    PACT_BROKER_DATABASE_ADAPTER=postgres \
    PACT_BROKER_DATABASE_HOST=pact-postgres \
    PACT_BROKER_DATABASE_PORT=5432 \
    PACT_BROKER_DATABASE_NAME=pact_broker \
    PACT_BROKER_DATABASE_USERNAME=pact_broker \
    PACT_BROKER_DATABASE_PASSWORD=$POSTGRES_PASSWORD \
    PACT_BROKER_DATABASE_CONNECT_MAX_RETRIES=10 \
    PACT_BROKER_BASIC_AUTH_USERNAME=$PACT_BROKER_USERNAME \
    PACT_BROKER_BASIC_AUTH_PASSWORD=$PACT_BROKER_PASSWORD \
    PACT_BROKER_PUBLIC_HEARTBEAT=true
```

## 5) Verify Broker Health

### 5.1 Get the public URL (FQDN)

```bash
FQDN=$(az containerapp show -g rg-pact -n pact-broker --query properties.configuration.ingress.fqdn -o tsv)
echo $FQDN
```

Your Broker URL is:

```
https://$FQDN
```

### 5.2 Check logs (confirm DB connection + migrations)

```bash
az containerapp logs show -g rg-pact -n pact-broker --tail 300 --follow
```

A healthy startup includes messages like:

```
Connected to database pact_broker
Migrating database schema
Mounting UI
Mounting PactBroker::API
```

### 5.3 Browser access

Open:

```
https://$FQDN
```

Login:

- Username: `$PACT_BROKER_USERNAME`
- Password: `$PACT_BROKER_PASSWORD`

## 6) Common Failure + Fix

### 6.1 Broker crashes with DB timeout

**Symptoms:**

```
PG::ConnectionBad ... port 5432 ... timeout expired
```

**Fix checklist:**

- Postgres ingress is TCP + exposedPort 5432:
  ```bash
  az containerapp show ... pact-postgres --query properties.configuration.ingress -o jsonc
  ```
- Broker uses `PACT_BROKER_DATABASE_HOST=pact-postgres` (NOT the internal FQDN)
- Postgres min replicas is 1:
  ```bash
  az containerapp show ... pact-postgres --query properties.template.scale -o jsonc
  ```
- Add retries:
  ```bash
  PACT_BROKER_DATABASE_CONNECT_MAX_RETRIES=10
  ```

### 6.2 Password mismatch

Postgres and broker must match exactly:

- Postgres: `POSTGRES_USER` / `POSTGRES_PASSWORD`
- Broker: `PACT_BROKER_DATABASE_USERNAME` / `PACT_BROKER_DATABASE_PASSWORD`

## 7) Azure DevOps Pipeline Configuration

Your repo pipeline expects these variables (Pipeline UI → Variables):

- `PactBrokerUrl` = `https://$FQDN`
- `PactBrokerUsername` = `admin` (secret recommended)
- `PactBrokerPassword` = `admin123!` (secret)
- (Optional if using PactFlow instead) `PactBrokerToken`

These map into environment variables used by scripts/tests:

- `PACT_BROKER_BASE_URL`
- `PACT_BROKER_USERNAME`
- `PACT_BROKER_PASSWORD`

## 8) Useful Commands

**Show ingress mode (public vs private)**

```bash
az containerapp show -g rg-pact -n pact-broker --query "properties.configuration.ingress.external" -o tsv
```

**List revisions/health**

```bash
az containerapp revision list -g rg-pact -n pact-broker -o table
az containerapp revision list -g rg-pact -n pact-postgres -o table
```

**Tail logs**

```bash
az containerapp logs show -g rg-pact -n pact-broker --tail 200
az containerapp logs show -g rg-pact -n pact-postgres --tail 200
```

## Notes

- Keeping the broker external allows Microsoft-hosted Azure DevOps agents to reach it (no VNet required for the pipeline).
- If you make the broker internal, you will need a self-hosted Azure DevOps agent inside the same network/VNet.
- If you want, paste your actual resource group/environment/app names (or your broker FQDN) and I'll tailor the file to your exact values.
