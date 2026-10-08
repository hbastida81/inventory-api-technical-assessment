# Inventory Management API

RESTful API for product inventory management built with .NET 8, SQL Server, and OAuth2 authentication via Keycloak.

The API manages Products, Categories, and Inventory Movements (entry/exit). It uses EF Core for read operations, Dapper for write operations, and a lightweight CQRS pattern with manual command/query handlers.

## Technical Decisions

- **Architecture:** Clean Architecture with four projects (Domain, Application, Infrastructure, Api).
- **CQRS:** Lightweight manual command/query handlers without MediatR.
- **Reads:** Entity Framework Core with `AsNoTracking` projections.
- **Writes:** Dapper with parameterized inline SQL.
- **Schema:** EF Core Migrations.
- **Auth:** Keycloak as OAuth2/OIDC Authorization Server. Authorization Code + PKCE. API acts as Resource Server only.
- **Database:** SQL Server 2022.
- **Containerization:** Docker Compose (API + SQL Server + Keycloak).

For detailed architecture and decision records, see [docs/architecture.md](docs/architecture.md) and [docs/architecture-decisions.md](docs/architecture-decisions.md).

## Prerequisites

- **Docker** and **Docker Compose** (primary requirement)
- **.NET 8 SDK** (only needed for local build/test/debug outside Docker)

## Quick Start

```bash
docker compose up --build
```

This starts three services:

| Service | Host URL | Purpose |
|---|---|---|
| Inventory API | http://localhost:8081 | RESTful API + Swagger |
| Keycloak | http://localhost:8080 | OAuth2 Authorization Server |
| SQL Server | localhost:1434 | Database |

Startup may take 30-60 seconds while SQL Server initializes, Keycloak imports the realm, and the API applies database migrations.

**Swagger UI:** http://localhost:8081/swagger/index.html

## Ports

| Service | Host Port | Container Port |
|---|---|---|
| API | 8081 | 8080 |
| Keycloak | 8080 | 8080 |
| SQL Server | 1434 (default) | 1433 |

SQL Server is exposed on host port 1434 by default. Host port 1434 is used by default because port 1433 was already occupied by another local SQL Server instance during development.

Override the host port if needed:

```bash
SQLSERVER_PORT=1450 docker compose up --build
```

This changes only the host-published port. The API communicates internally with `inventory-sqlserver:1433` regardless of the host port.

## Database Initialization

The database starts empty. EF Core applies the approved migration automatically when the API starts in the local Docker evaluation environment.

Required tables (Products, Categories, InventoryMovements) are created automatically. No manual `dotnet ef` commands are needed for Docker evaluation.

Data is created through the API. There is no pre-loaded seed data.

Runtime application writes use Dapper. EF Core Migrations manage only the schema.

## How to Use the API

1. Create a Category (POST /api/categories).
2. Create a Product using the Category (POST /api/products).
3. Query or update the Product (GET/PUT /api/products/{id}).
4. Register Inventory Entry or Exit movements (POST /api/products/{productId}/movements).
5. Query the Product to see the updated stock.

All endpoints require authentication. See the next section.

## OAuth2 Authentication

Keycloak is the OAuth2/OIDC Authorization Server. The Inventory API is a Resource Server — it validates access tokens but does not issue them.

Swagger uses Authorization Code + PKCE. The OAuth2 token endpoint belongs to Keycloak, not to the Inventory API. Swagger handles the Authorization Code + PKCE exchange automatically.

### How to Authenticate in Swagger

1. Open http://localhost:8081/swagger/index.html
2. Click **Authorize** (lock icon).
3. Confirm the OAuth client: `inventory-api-swagger`. Click **Authorize**.
4. Keycloak login page appears.
5. Sign in with the local evaluation user:
   - **Username:** `testuser`
   - **Password:** `testpassword`
6. Keycloak redirects back to Swagger. Swagger shows **Authorized**.
7. Execute any endpoint, for example GET /api/products.
8. Expected result: **200 OK**.

Without authentication, all endpoints return **401 Unauthorized**.

## Local Evaluation Credentials

**These credentials are for LOCAL EVALUATION ONLY.**

| Credential | Value | Purpose |
|---|---|---|
| Keycloak test user | testuser / testpassword | API authentication via Swagger |
| Keycloak admin | admin / admin | Keycloak administration console |
| SQL Server SA | sa / YourStrong!Passw0rd | Database access |

## API Endpoints

### Products

| Method | Route | Description |
|---|---|---|
| POST | /api/products | Create a product |
| GET | /api/products | List all products |
| GET | /api/products/{id} | Get a product by ID |
| PUT | /api/products/{id} | Update a product |
| DELETE | /api/products/{id} | Delete a product |

### Categories

| Method | Route | Description |
|---|---|---|
| POST | /api/categories | Create a category |
| GET | /api/categories | List all categories |
| GET | /api/categories/{id} | Get a category by ID |
| PUT | /api/categories/{id} | Update a category |
| DELETE | /api/categories/{id} | Delete a category |

### Inventory Movements

| Method | Route | Description |
|---|---|---|
| POST | /api/products/{productId}/movements | Register an entry or exit movement |

Inventory Movements are append-only. There are no update or delete endpoints for movements.

## Business Rules

- Product price must be >= 0.
- Movement quantity must be a positive integer.
- Inventory Exit cannot produce negative stock (returns 409 Conflict).
- Movement timestamp is assigned by the server in UTC.
- Inventory Movements are immutable historical records.
- A Product with movement history cannot be deleted (returns 409 Conflict).
- A Category referenced by Products cannot be deleted (returns 409 Conflict).
- Allowed deletions are permanent (hard delete).

## HTTP Status Codes

| Status | Meaning |
|---|---|
| 200 OK | Successful retrieval or update |
| 201 Created | Successful creation |
| 204 No Content | Successful deletion |
| 400 Bad Request | Invalid request / validation failure |
| 401 Unauthorized | Missing or invalid authentication |
| 404 Not Found | Resource does not exist |
| 409 Conflict | Business rule violation (insufficient stock, referenced entity) |
| 500 Internal Server Error | Unexpected error (no internal details exposed) |

## Running Tests

```bash
dotnet test Inventory.sln
```

Current verified baseline: 63 passed, 0 failed, 0 skipped.

Tests cover domain entity behavior, command handler logic, and query handler behavior using xUnit and NSubstitute.

## Build

```bash
dotnet build Inventory.sln
```

Current baseline: 0 errors, 0 warnings.

## Local Development / Debugging

To run the API from your IDE or CLI while using Docker for SQL Server and Keycloak:

1. Start only the infrastructure services:

```bash
docker compose up -d inventory-sqlserver keycloak
```

2. Wait for both services to become healthy.

3. Run the API locally:

```bash
dotnet run --project src/Inventory.Api
```

The API uses `appsettings.Development.json` which is configured for local development:

| Setting | Value |
|---|---|
| SQL Server | localhost,1434 |
| Keycloak Authority | http://localhost:8080/realms/inventory |
| Keycloak Base URL | http://localhost:8080 |
| Audience | inventory-api |
| RequireHttpsMetadata | false |

The API will be available at `http://localhost:5000` (or the port configured in `launchSettings.json`).

Set breakpoints in your IDE and debug as with any ASP.NET Core application.

## Environment Configuration

| Variable | Purpose | Default |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | (required) |
| `Authentication__Authority` | Keycloak realm URL for OIDC discovery | (required) |
| `Authentication__ValidIssuer` | Canonical JWT issuer for validation | Authority value |
| `Authentication__Audience` | JWT audience claim | inventory-api |
| `Authentication__KeycloakBaseUrl` | Browser-facing Keycloak URL for Swagger | Derived from issuer |
| `Authentication__RequireHttpsMetadata` | Require HTTPS for OIDC metadata | true |
| `SQLSERVER_PORT` | Host-published SQL Server port | 1434 |

`RequireHttpsMetadata=false` is used only for the local HTTP evaluation environment. The application defaults to `true` (secure) when the value is not provided.

## Docker Architecture

```
Browser ──► localhost:8080 (Keycloak login)
Browser ──► localhost:8081 (Swagger / API)

inventory-api ──► keycloak:8080 (OIDC discovery / JWKS)
inventory-api ──► inventory-sqlserver:1433 (database)
```

Published host ports and Docker-internal service names serve different purposes. The API communicates with SQL Server and Keycloak through Docker DNS, not through published host ports.

## Persistence Strategy

| Operation | Technology | Reason |
|---|---|---|
| Reads | EF Core | Assessment requirement |
| Writes | Dapper (parameterized inline SQL) | Assessment requirement |
| Schema | EF Core Migrations | Single schema management mechanism |
| Inventory transactions | Dapper transaction with atomic conditional stock update | Concurrency-safe, no negative stock possible |

This split exists because it is an explicit requirement of the technical assessment. It is implemented with specific read/write repository interfaces without unnecessary generic repository or Unit of Work abstractions.

## Security

- OAuth2 Authorization Code + PKCE via Keycloak.
- Direct Access Grants are disabled.
- Strict JWT validation: issuer, audience, lifetime, signing key.
- `RequireHttpsMetadata=false` is used only for local evaluation (HTTP Keycloak). Production deployments should use HTTPS.
- No secrets are hardcoded in application code. All credentials are provided through environment variables or Docker Compose configuration.
- API error responses never expose stack traces, SQL details, or internal implementation information.

## Troubleshooting

### Port 1434 already in use

```bash
SQLSERVER_PORT=1450 docker compose up --build
```

### Swagger returns 401

Click the **Authorize** button in Swagger and authenticate with Keycloak before making API requests.

### Keycloak login page not ready

Keycloak may take 30-40 seconds to start. Wait for the container to become healthy:

```bash
docker compose ps
```

### Clean restart of assessment environment

```bash
docker compose down -v
docker compose up --build
```

This removes only this project's containers and volumes. It does not affect unrelated Docker resources.

## Architecture Documentation

| Document | Description |
|---|---|
| [docs/spec.md](docs/spec.md) | Functional specification (Spec-Driven Development) |
| [docs/architecture.md](docs/architecture.md) | Architecture design |
| [docs/architecture-decisions.md](docs/architecture-decisions.md) | Architecture Decision Records (ADRs) |
| [docs/implementation-plan.md](docs/implementation-plan.md) | Implementation plan and TDD strategy |
| [docs/ai-development-log.md](docs/ai-development-log.md) | Chronological AI-assisted development log |
| [docs/ai-collaboration-summary.md](docs/ai-collaboration-summary.md) | AI collaboration process summary |
| [docs/decision-making-answers.md](docs/decision-making-answers.md) | Decision-making question answers |
