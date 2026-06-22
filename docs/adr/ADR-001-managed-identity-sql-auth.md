# ADR-001: Use Azure Managed Identity + Entra ID Authentication for Azure SQL Instead of Username/Password

**Date:** 2026-06-19
**Author:** BeautyStoreCapstone Engineering
**Reviewers:** Staff Engineering / Security Review

---

## Status

**Accepted** — Implemented and verified in production (Day 26–27).

---

## Context

BeautyStoreCapstone is an ASP.NET Core 10 Minimal API deployed to Azure Container Apps.
It connects to Azure SQL Database for order, inventory, payment, and shipping data.

The default approach to SQL authentication is a username and password stored in a connection
string, typically injected as an environment variable or application secret. This is the
out-of-the-box path in most tutorials and scaffolding tools.

The deployment surface includes:

- **Azure Container Apps** — managed compute, system-assigned Managed Identity available
- **Azure SQL Database** — supports both SQL authentication and Entra ID authentication
- **Bicep IaC** — all infrastructure defined as code; environment variables are visible in
  deployment templates and Azure Portal
- **OutboxRelayWorker** — a BackgroundService that polls SQL every 30 seconds
- **Application Insights + OpenTelemetry** — telemetry pipeline that captures connection
  strings in dependency spans if they contain embedded credentials
- **Azure Container Registry** — image pull also requires a credential; this ADR establishes
  the pattern used for all non-application credentials in the system

The threat model identified credential leakage as the highest-impact risk across the stack.
Connection strings appear in: Bicep parameter files, GitHub Actions secrets, Azure Portal
environment variable panels, Application Insights telemetry, and developer machine profiles.
Each is a potential exfiltration point.

The question is: **what authentication mechanism should the API and background worker use
to connect to Azure SQL in production?**

---

## Decision

**Use Azure Managed Identity (System-Assigned) with Entra ID authentication for all SQL
connections. No SQL username or password is stored anywhere in the system.**

Connection string in Container App environment variable:

```
Server=tcp:<server>.database.windows.net,1433;
Database=BeautyStoreDb;
Authentication=Active Directory Managed Identity;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

The Container App's System-Assigned Managed Identity is mapped to a SQL database user:

```sql
CREATE USER [beautystore-dev-api] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [beautystore-dev-api];
ALTER ROLE db_datawriter ADD MEMBER [beautystore-dev-api];
```

`Microsoft.Data.SqlClient` with `Authentication=Active Directory Managed Identity` acquires
a token from the Azure Instance Metadata Service (IMDS) at connection time. No credential
is read from the connection string. No password exists to rotate, leak, or revoke.

---

## Alternatives Considered

### Option A — SQL Authentication (username + password)

Store a SQL login and password in the connection string. Inject via environment variable or
Azure Key Vault reference.

**Rejected because:**
- Password must be created, rotated, and distributed. Each rotation is an operational event.
- Connection string with embedded password appears in: Bicep parameter files, Azure Portal
  environment variable panel, Application Insights telemetry (if not scrubbed), developer
  `.env` files, CI/CD pipeline secrets.
- A leaked password gives full DB access from any network. The blast radius of a credential
  leak is unbounded.
- Rotation requires a coordinated redeploy. Zero-downtime rotation requires two active
  passwords simultaneously, increasing the window of exposure.

### Option B — SQL Authentication + Azure Key Vault reference

Store the password in Key Vault. Inject it into the Container App via a Key Vault reference
in the secret configuration.

**Rejected because:**
- The password still exists. Key Vault reduces exposure but does not eliminate the secret.
- Adds a Key Vault dependency to the startup path. If Key Vault is unreachable at startup,
  the Container App fails to start.
- Requires a separate Managed Identity or access policy to read from Key Vault — adding a
  second identity management concern to solve the first.
- Does not prevent the password from appearing in Application Insights dependency telemetry
  if `SqlClient` includes it in the connection string used for span attributes.

### Option C — User-Assigned Managed Identity

Create a Managed Identity resource explicitly, assign it to the Container App, and use it
for SQL authentication.

**Not selected (for now) because:**
- System-Assigned identity is sufficient for a single-service deployment. It is created,
  managed, and deleted with the Container App automatically.
- User-Assigned identity is the correct choice when the same identity must be shared across
  multiple services or when identity must survive resource deletion. Neither applies here.
- User-Assigned adds a separate identity resource to Bicep, increasing template complexity
  without current benefit.
- Migration from System-Assigned to User-Assigned is straightforward if the requirement
  emerges.

### Option D — Connection string with `Authentication=Active Directory Default`

Use `DefaultAzureCredential` explicitly in the application code via `AddAzureTokenCredential`
on the SQL connection.

**Not selected because:**
- `Authentication=Active Directory Managed Identity` in the connection string is the
  documented `Microsoft.Data.SqlClient` approach for Container Apps. It does not require
  code changes to the DbContext registration.
- `DefaultAzureCredential` is used for Service Bus (where there is no connection-string
  equivalent). Keeping the SQL path in the connection string avoids mixing authentication
  patterns across the EF Core registration and the Service Bus client registration.

---

## Trade-offs

| Concern | SQL Username/Password | Managed Identity (chosen) |
|---|---|---|
| Secrets in source control | Risk — password in Bicep params | None — no password exists |
| Secrets in Azure Portal | Visible in env var panel | Not applicable |
| Credential rotation | Manual, requires redeploy | Not required — token is short-lived |
| Token acquisition latency | None — password inline | ~1–5 ms IMDS call on first connection, then cached |
| Local development | Works immediately | Requires Azure CLI login or VS developer credential |
| Works in Docker locally | Yes | Requires `DefaultAzureCredential` fallback chain |
| Blast radius of credential leak | Full DB access from any network | Token is non-exportable; valid only from IMDS endpoint inside Azure |
| Operational complexity | Password rotation process required | None beyond initial SQL user creation |
| Bicep complexity | Connection string includes User ID + Password params | Connection string has no secrets; no Bicep secret params needed |

---

## Consequences

### Positive

1. **Zero application secrets in the Container App configuration.** The only secret remaining
   in the deployment is the ACR pull password — an infrastructure credential, not an
   application credential. This is verifiable by inspecting the Container App secrets store.

2. **No rotation process.** Managed Identity tokens are acquired at connection time and expire
   after one hour. There is no password to rotate, no rotation schedule, and no service
   disruption during rotation.

3. **Audit trail.** All SQL connections are authenticated as the named identity
   `beautystore-dev-api`. SQL audit logs and Entra ID sign-in logs attribute every query to
   this identity. With SQL username/password, all connections appear as the same SQL login
   regardless of which service made them.

4. **Principle of least privilege enforced at the identity layer.** The MSI has
   `db_datareader` + `db_datawriter` only. `db_owner` is not granted. Privilege escalation
   requires compromising the Container App runtime, not just acquiring a credential.

5. **OpenTelemetry telemetry is clean.** Application Insights dependency spans for SQL
   connections contain no embedded credentials. The connection string recorded in spans
   shows only the server FQDN and database name.

### Negative / Constraints

1. **Local development requires Azure CLI or Visual Studio login.** `Microsoft.Data.SqlClient`
   with `Authentication=Active Directory Managed Identity` only works inside Azure. Local
   development requires either `Authentication=Active Directory Default` (which falls back to
   `DefaultAzureCredential` and picks up Azure CLI credentials) or a separate local
   connection string without Managed Identity.

2. **SQL Entra Admin must be configured before the first deployment.** The Container App MSI
   cannot be mapped to a SQL user until an Entra Admin is set on the SQL Server. This is a
   manual step after first Bicep deployment. It is not automatable in Bicep without a
   deployment script.

3. **SQL user creation is a manual T-SQL step post-deploy.** `CREATE USER [...] FROM EXTERNAL
   PROVIDER` must be run in the target database. It cannot be expressed in Bicep. This step
   must be documented and executed as part of the deployment runbook.

4. **Token acquisition fails if IMDS is unreachable.** In non-Azure environments (on-premises,
   some container runtimes), the IMDS endpoint (`169.254.169.254`) is not available and token
   acquisition fails at connection time. The application must handle this gracefully.

5. **OutboxRelayWorker failure mode changes.** With SQL username/password, a wrong password
   fails immediately at connection open with `SqlException`. With Managed Identity, a token
   acquisition failure produces a different exception path. The `PollAsync` try/catch handles
   both, but error messages differ and must be monitored in Application Insights.

---

## Verification Evidence

**Connection string contains no User ID or Password — verified in Bicep:**

```bicep
{ name: 'ConnectionStrings__BeautyStoreDb',
  value: 'Server=tcp:${sqlServerFqdn},1433;Database=${databaseName};
          Authentication=Active Directory Managed Identity;Encrypt=True;
          TrustServerCertificate=False;Connection Timeout=30;' }
```

**OutboxRelayWorker confirmed DB reachable via MSI — production log:**

```
info: OutboxRelayWorker[0]
      Outbox relay tick complete. DB reachable: True
```

**Application Insights distributed trace — EF Core SQL span visible:**

```
SQL: BeautyStoreDb
Type: SQL
Call status: True
Duration: 1.8 ms
Statement: SELECT 1
```

No credentials appear in the span attributes.

**Zero secrets in Container App configuration — only infrastructure credential remains:**

```
Secrets: [ { name: 'registry-password' } ]   ← ACR pull only
Env vars: no secretRef entries
```
