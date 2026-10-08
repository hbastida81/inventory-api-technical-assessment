# Architecture — Inventory Management API

**Document type:** Architecture Design
**Status:** APPROVED (all decisions consolidated)
**Version:** 2.0
**Date:** 2026-10-07

**Related documents:**
- Specification: [spec.md](spec.md)
- Decision records: [architecture-decisions.md](architecture-decisions.md) (ADR-001 through ADR-011)
- Implementation plan: [implementation-plan.md](implementation-plan.md)

---

# 1. Architecture Overview

The Inventory API uses a **Clean Architecture** approach organized in four projects with explicit dependency direction: Domain at the center (no dependencies), Application depending on Domain, Infrastructure implementing Application interfaces, and API as the composition root.

**Why Clean Architecture:**
- The assessment evaluates SOLID, Clean Code, and design patterns. Clean Architecture enforces dependency inversion (the D in SOLID) at the project level.
- The EF Core/Dapper split naturally maps to an infrastructure layer that implements application-defined interfaces.
- It produces a testable application layer with no infrastructure dependencies.
- It is a well-known pattern that reviewers can follow immediately.
- For 12 endpoints with clear CQRS separation, Clean Architecture provides structure without excessive overhead.

**Why not something simpler (e.g., two-layer API + Data):**
- A simpler structure would work functionally but would not demonstrate the architectural thinking expected at Senior level.
- The EF Core + Dapper split benefits from an explicit infrastructure boundary.
- The assessment specifically calls for design patterns and SOLID — Clean Architecture enforces both.

---

# 2. Solution Structure

```
InventoryApi.sln
├── src/
│   ├── Inventory.Domain/
│   ├── Inventory.Application/
│   ├── Inventory.Infrastructure/
│   └── Inventory.Api/
├── tests/
│   └── Inventory.Tests/
├── docker/
│   ├── keycloak/
│   │   └── realm-export.json
│   └── sql/
│       └── (init scripts if needed)
├── docker-compose.yml
├── Dockerfile
└── README.md
```

### Inventory.Domain

**Responsibility:** Domain entities, enums, value objects, domain rules. Pure business logic with zero external dependencies.

**Allowed dependencies:** None. No NuGet packages except the .NET SDK itself.

**Forbidden dependencies:** Application, Infrastructure, API, EF Core, Dapper, ASP.NET Core, any NuGet package.

**Contents:**
- `Entities/` — Product, Category, InventoryMovement.
- `Enums/` — MovementType (Entry, Exit).
- Domain validation rules embedded in entities (e.g., stock cannot go negative).

### Inventory.Application

**Responsibility:** Use cases expressed as commands and queries. Defines interfaces (ports) that Infrastructure implements. Orchestrates domain operations.

**Allowed dependencies:** Inventory.Domain.

**Forbidden dependencies:** Infrastructure, API, EF Core, Dapper, ASP.NET Core.

**Contents:**
- `Commands/` — command records and their handlers.
- `Queries/` — query records and their handlers.
- `Interfaces/` — ports for persistence (read and write), defined here, implemented in Infrastructure.
- `Exceptions/` — application-level exceptions for not-found, conflict, validation failures.

### Inventory.Infrastructure

**Responsibility:** Implements the interfaces defined in Application. Contains all persistence logic (EF Core for reads, Dapper for writes), database configuration, and external service integration.

**Allowed dependencies:** Inventory.Application, Inventory.Domain, EF Core, Dapper, SQL Server driver.

**Forbidden dependencies:** API, ASP.NET Core (except for service registration extensions).

**Contents:**
- `Persistence/` — DbContext (read-only), Dapper write repositories.
- `Configuration/` — EF Core entity configurations.
- `DependencyInjection.cs` — service registration extension method.

### Inventory.Api

**Responsibility:** Composition root. HTTP layer. Controllers, authentication configuration, Swagger, error handling middleware, dependency injection wiring.

**Allowed dependencies:** Inventory.Application, Inventory.Infrastructure (only for DI registration).

**Forbidden dependencies:** Inventory.Domain entities should not leak into API contracts. API uses DTOs/request-response models.

**Contents:**
- `Controllers/` — REST controllers.
- `Models/` — request/response DTOs.
- `Middleware/` — global error handling.
- `Extensions/` — service configuration helpers.
- `Program.cs` — composition root.

### Inventory.Tests

**Responsibility:** Unit tests for domain logic, command handlers, query handlers, and validation.

**Allowed dependencies:** All source projects (for testing), xUnit, mocking library (NSubstitute or Moq).

**Contents:**
- `Domain/` — entity behavior tests.
- `Commands/` — command handler tests.
- `Queries/` — query handler tests.

---

# 3. Dependency Direction

```
Inventory.Api
    │
    ├──► Inventory.Application
    │         │
    │         └──► Inventory.Domain
    │
    └──► Inventory.Infrastructure (DI registration only)
              │
              ├──► Inventory.Application (implements interfaces)
              └──► Inventory.Domain (maps entities)
```

**Rules:**
- Domain depends on nothing.
- Application depends only on Domain.
- Infrastructure depends on Application (to implement its interfaces) and Domain (to map entities).
- API depends on Application (to call handlers) and Infrastructure (only to register services in DI).
- No circular dependencies.
- No layer may bypass another (e.g., API must not call Infrastructure directly for business operations).

---

# 4. Domain Layer

**Entities:**

- **Product** — Id (int), Name (string), CategoryId (int), Price (decimal), Stock (int), CreatedAt (DateTime, UTC), UpdatedAt (DateTime, UTC). Contains domain behavior: `AddStock(int quantity)`, `RemoveStock(int quantity)` which enforces the no-negative-stock rule by throwing a domain exception.
- **Category** — Id (int), Name (string), CreatedAt (DateTime, UTC), UpdatedAt (DateTime, UTC).
- **InventoryMovement** — Id (int), ProductId (int), Type (MovementType), Quantity (int), CreatedAt (DateTime, UTC — serves as the authoritative movement timestamp). Immutable after creation. No UpdatedAt (movements are never modified).

**Enums:**
- **MovementType** — Entry, Exit.

**Domain rules embedded in entities:**
- Product.RemoveStock throws if quantity exceeds current stock (OD-005).
- InventoryMovement constructor enforces positive quantity (BR-006).
- Product constructor enforces non-null name and valid price.

**What does NOT belong in Domain:**
- Database annotations or EF Core attributes.
- Persistence interfaces.
- HTTP concepts.
- Configuration.

---

# 5. Application Layer

## Commands (Write operations — Dapper)

Each command is a record representing the intent. Each handler implements a simple interface.

```
Interface: ICommandHandler<TCommand, TResult>
  Task<TResult> HandleAsync(TCommand command, CancellationToken ct)
```

**Commands:**
- `CreateProductCommand` → handler creates product via Dapper, returns created product ID.
- `UpdateProductCommand` → handler updates product via Dapper.
- `DeleteProductCommand` → handler deletes product via Dapper.
- `CreateCategoryCommand` → handler creates category via Dapper.
- `UpdateCategoryCommand` → handler updates category via Dapper.
- `DeleteCategoryCommand` → handler deletes category via Dapper.
- `RegisterMovementCommand` → handler validates stock, inserts movement, updates product stock — all via Dapper in a transaction.

## Queries (Read operations — EF Core)

```
Interface: IQueryHandler<TQuery, TResult>
  Task<TResult> HandleAsync(TQuery query, CancellationToken ct)
```

**Queries:**
- `GetProductQuery` → handler queries product by ID via EF Core.
- `ListProductsQuery` → handler queries all products via EF Core.
- `GetCategoryQuery` → handler queries category by ID via EF Core.
- `ListCategoriesQuery` → handler queries all categories via EF Core.

## Interfaces (Ports)

Defined in Application, implemented in Infrastructure:

- `IProductReadRepository` — EF Core read operations for products.
- `IProductWriteRepository` — Dapper write operations for products.
- `ICategoryReadRepository` — EF Core read operations for categories.
- `ICategoryWriteRepository` — Dapper write operations for categories.
- `IMovementWriteRepository` — Dapper write operations for movements.
- `IDbConnectionFactory` — provides `IDbConnection` instances for Dapper.

**Why separate read/write repositories:**
The CQRS split demands it. Read repositories return rich DTOs with navigation data (e.g., product with category name). Write repositories accept commands and execute parameterized SQL. They use different underlying technologies (EF Core vs Dapper) and should not share an interface.

## Application Exceptions

- `NotFoundException` — resource does not exist (maps to 404).
- `ConflictException` — business rule violation like insufficient stock (maps to 409).
- `ValidationException` — input validation failure (maps to 400).

These are thrown by handlers and caught by API middleware.

## How CQRS works without MediatR

Controllers receive an `ICommandHandler<T, R>` or `IQueryHandler<T, R>` via constructor injection. No mediator, no pipeline, no dispatch. The controller calls `handler.HandleAsync(command, ct)` directly. Each handler is registered in DI as its specific interface.

**Example flow (conceptual, not code):**
1. Controller receives HTTP request, maps to command/query record.
2. Controller calls injected handler.
3. Handler uses injected repository to perform the operation.
4. Handler returns result.
5. Controller maps result to HTTP response.

---

# 6. Infrastructure Layer

## EF Core (Read Access)

- **InventoryDbContext** — read-only DbContext with `DbSet<Product>`, `DbSet<Category>`, `DbSet<InventoryMovement>`.
- Entity configurations in `IEntityTypeConfiguration<T>` classes.
- Navigation properties for joins (Product → Category).
- Used by read repository implementations.
- DbContext is registered as `Scoped` in DI.
- EF Core Migrations manage the database schema.

**Important:** The DbContext is used for reads. It is NOT used for writes. This keeps the CQRS separation clean.

## Dapper (Write Access)

- Write repositories receive `IDbConnectionFactory` and create `IDbConnection` instances.
- All writes use parameterized inline SQL.
- Transactions are managed explicitly where needed (e.g., RegisterMovement must insert movement + update stock atomically).

**IDbConnectionFactory implementation:**
- Creates `SqlConnection` from a connection string loaded via environment variable.
- Registered as `Singleton` in DI (the factory is stateless; connections are created per-use).

## EF Core and Dapper Coexistence

Both EF Core and Dapper connect to the same SQL Server database. They share the same schema but serve different purposes:
- EF Core owns the schema (migrations).
- Dapper executes raw SQL against that schema.
- No abstraction layer sits between them — they are independent.
- There is no conflict because reads and writes operate on different code paths.

---

# 7. API Layer

## Controllers

One controller per resource:
- `ProductsController` — CRUD endpoints for products.
- `CategoriesController` — CRUD endpoints for categories.
- `MovementsController` — POST endpoint for inventory movements (nested under products route).

Controllers are thin: validate input shape (model binding), call the appropriate handler, map the result to HTTP response.

## Authentication

- `[Authorize]` attribute on all controllers (or globally via policy).
- JWT Bearer authentication configured in `Program.cs` using Keycloak's OIDC discovery endpoint.
- No custom authentication logic.

## Swagger

- Swashbuckle or NSwag configured with OAuth2 Authorization Code + PKCE flow.
- Security scheme points to Keycloak's authorization and token endpoints.
- Evaluator clicks "Authorize" in Swagger UI, logs in via Keycloak, and tests endpoints.

## Error Handling

A global exception-handling middleware catches application exceptions and maps them:

| Exception | HTTP Status |
|---|---|
| `ValidationException` | 400 Bad Request |
| `NotFoundException` | 404 Not Found |
| `ConflictException` | 409 Conflict |
| Unhandled exceptions | 500 Internal Server Error (generic message) |

Response body follows the spec error contract: `{ status, message, errors? }`.

## Dependency Injection

`Program.cs` is the composition root:
- Registers domain services (if any).
- Registers all command/query handlers.
- Registers read/write repositories.
- Registers DbContext and DbConnectionFactory.
- Configures authentication.
- Configures Swagger.

---

# 8. CQRS Design

## Naming Conventions

- Commands: `{Action}{Entity}Command` — e.g., `CreateProductCommand`.
- Command handlers: `{Action}{Entity}CommandHandler`.
- Queries: `{Action}{Entity}Query` — e.g., `GetProductQuery`, `ListProductsQuery`.
- Query handlers: `{Action}{Entity}QueryHandler`.

## Handler Responsibilities

- **Command handlers:** Validate business rules, call write repository, return result (created ID or void).
- **Query handlers:** Call read repository, return DTO. No business logic in queries.

## Dependency Boundaries

- Command handlers depend on write repository interfaces (Dapper).
- Query handlers depend on read repository interfaces (EF Core).
- No handler depends on both read and write repositories (strict CQRS).

**RegisterMovement concurrency design (ADR-008 — APPROVED, modified by human):**

The `RegisterMovementCommandHandler` calls a single write repository method that executes atomically within a Dapper transaction:

1. For **Entry** movements: insert the movement and update stock (`SET Stock = Stock + @Quantity`) in one transaction.
2. For **Exit** movements: use an atomic conditional update (`UPDATE Products SET Stock = Stock - @Quantity WHERE Id = @ProductId AND Stock >= @Quantity`). Verify the affected row count. If zero rows affected, stock is insufficient — roll back and throw `ConflictException`. Insert the movement only if the stock update succeeded.

This design ensures:
- Stock modification and movement insertion are committed or rolled back together.
- Concurrent requests cannot consume the same available stock (the conditional WHERE clause is atomic at the SQL engine level).
- No separate stock read query is needed for exits — the WHERE clause validates and updates in one operation.
- The "read" is an implementation detail of the write operation, not a CQRS query. Strict CQRS separation is maintained.

## Are Repositories Necessary?

**For writes (Dapper):** Yes. Write repositories encapsulate the SQL and transaction management. Without them, handlers would contain raw SQL — mixing application logic with infrastructure concerns.

**For reads (EF Core):** A lightweight read repository or query service is justified to keep DbContext out of the Application layer. The Application layer should not reference EF Core directly. The read repository interface is defined in Application; the implementation using DbContext lives in Infrastructure.

**Alternative considered:** Injecting DbContext directly into query handlers. Rejected because it would require the Application project to reference EF Core, breaking the dependency rule.

---

# 9. Persistence Design Approach

## EF Core Read Model

- DbContext with navigation properties for efficient joins.
- Read repositories return DTOs or domain entities.
- Queries use `AsNoTracking()` for read performance.
- No SaveChanges calls from read repositories.

## Dapper Command Strategy

- Write repositories receive `IDbConnectionFactory`.
- Open connection, begin transaction when needed, execute parameterized SQL, commit.
- SQL is inline and parameterized (no string concatenation, no injection risk).

## Transaction Handling

- **Simple writes** (create product, update category): Single SQL statement via Dapper. No explicit transaction needed (single statement is atomic).
- **Complex writes** (register movement): Explicit Dapper transaction. For exits, uses atomic conditional UPDATE (`WHERE Stock >= @Quantity`) and verifies row count before inserting the movement. Committed or rolled back as a unit (ADR-008, approved).

## Connection Management

- EF Core: DbContext manages its own connection (registered as Scoped).
- Dapper: `IDbConnectionFactory` creates a new `SqlConnection` per operation. The connection is opened/closed within the repository method using `using` statements.
- No shared connection between EF Core and Dapper.

## Unit of Work

**Not used.**

**Justification:** The assessment has no cross-aggregate transactions. Each write operation is self-contained. The RegisterMovement transaction is handled within a single repository method. Adding a Unit of Work abstraction would add complexity with no benefit for this scope.

---

# 10. OAuth2 / Keycloak Architecture

## Keycloak Responsibility

- Acts as the OAuth2/OIDC Authorization Server.
- Issues JWT access tokens.
- Hosts the login page.
- Pre-configured via realm import JSON containing: realm, public client (for Swagger PKCE), and a test user.

## API Responsibility

- Acts as Resource Server only.
- Validates JWT access tokens using ASP.NET Core JWT Bearer authentication.
- Reads Keycloak's OIDC discovery document to obtain signing keys automatically.
- Does NOT issue tokens, handle credentials, or store users.

## JWT Bearer Validation

- Authority: Keycloak realm URL (e.g., `http://keycloak:8080/realms/inventory`).
- Audience: configured to match the Keycloak client.
- Token validation: signature, expiration, issuer, audience.
- Configuration via environment variables.

## Swagger + PKCE Flow

- Swagger UI configured with OAuth2 Authorization Code + PKCE.
- Authorization URL and Token URL point to Keycloak endpoints.
- Client ID matches the Keycloak public client.
- Evaluator clicks "Authorize" → Keycloak login page → token returned → Swagger includes token in requests.

## Docker-Compose Interaction

```
keycloak:8080  ◄── Swagger UI (browser, Authorization Code + PKCE)
       │
       ▼
  Issues JWT
       │
       ▼
inventory-api:8081  ── validates JWT ──► keycloak:8080 (OIDC discovery)
       │
       ▼
sqlserver:1433
```

- Keycloak must start before the API can validate tokens.
- The API should handle Keycloak unavailability gracefully during startup (retry or health check).
- Keycloak uses its embedded H2 database (sufficient for a test environment — no additional DB container needed).

---

# 11. Error Handling Strategy

## Approach

A single global exception-handling middleware in the API layer catches exceptions and maps them to the spec error contract.

## Mapping

| Source | Exception Type | HTTP Status | Response |
|---|---|---|---|
| Model binding / FluentValidation | `ValidationException` | 400 | `{ status, message, errors[] }` |
| Handler: resource not found | `NotFoundException` | 404 | `{ status, message }` |
| Handler: business rule violation | `ConflictException` | 409 | `{ status, message }` |
| Handler: invalid reference (category) | `ValidationException` (business) | 422 | `{ status, message }` |
| Auth middleware | — | 401 | Standard ASP.NET Core response |
| Unhandled | `Exception` | 500 | `{ status: 500, message: "An unexpected error occurred." }` |

## Rules

- Never expose stack traces, exception messages, or internal details.
- Log the full exception internally (via ILogger).
- Return only the spec-defined error contract to the client.

---

# 12. Validation Strategy

## Two Levels of Validation

**Input validation (API boundary):**
- Required fields, data types, format constraints.
- Performed via ASP.NET Core model validation attributes or FluentValidation.
- Returns 400 Bad Request with field-level errors.
- Lives in the API layer (request DTOs).

**Business validation (Application/Domain):**
- Category exists when creating/updating a product.
- Product exists when registering a movement.
- Stock sufficient for exit movement.
- Performed in command handlers or domain entities.
- Throws `NotFoundException`, `ConflictException`, or `ValidationException`.

**Why two levels:** Input validation catches malformed requests before they reach business logic. Business validation enforces domain rules. They operate at different layers and catch different categories of errors.

**No duplication:** Input validation checks shape (is the field present? is it the right type?). Business validation checks meaning (does this category exist? is there enough stock?). They do not overlap.

---

# 13. Testing Architecture

## What Will Be Tested

### Domain Tests
- `Product.RemoveStock` throws when quantity exceeds stock.
- `Product.AddStock` increases stock correctly.
- `InventoryMovement` constructor rejects zero/negative quantity.
- Entity validation rules.

### Command Handler Tests
- `CreateProductCommandHandler` calls write repository with correct data.
- `RegisterMovementCommandHandler` validates stock and throws ConflictException when insufficient.
- `DeleteCategoryCommandHandler` throws ConflictException when category has products (OD-004, approved).
- Each handler tested with mocked repository interfaces.

### Query Handler Tests
- `GetProductQueryHandler` returns correct DTO.
- `GetProductQueryHandler` throws NotFoundException when product does not exist.
- Each handler tested with mocked read repository interfaces.

### Validation Tests
- Request DTOs reject missing required fields.
- Request DTOs reject invalid values.

## What Will NOT Be Tested (in unit tests)

- EF Core query correctness (integration test concern).
- Dapper SQL correctness (integration test concern).
- Keycloak token validation (infrastructure concern).
- Docker networking (deployment concern).

## Testing Tools

- **xUnit** — test framework (industry standard for .NET).
- **Mocking library** — NSubstitute or Moq (implementation detail, deferred to implementation phase). AI recommends NSubstitute for cleaner syntax.

---

# 14. Docker Architecture

## Runtime Topology

```
┌──────────────────────────────────────┐
│           docker-compose             │
│                                      │
│  ┌────────────┐  ┌───────────────┐  │
│  │  keycloak   │  │   sqlserver    │  │
│  │  :8080      │  │   :1433       │  │
│  └──────┬──────┘  └───────┬───────┘  │
│         │                 │          │
│         │    ┌────────────┤          │
│         │    │            │          │
│  ┌──────▼────▼──┐        │          │
│  │ inventory-api │────────┘          │
│  │   :8081       │                   │
│  └───────────────┘                   │
└──────────────────────────────────────┘
```

## Containers

| Container | Image | Port | Purpose |
|---|---|---|---|
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 | SQL Server database |
| `keycloak` | `quay.io/keycloak/keycloak:latest` | 8080 | OAuth2 Authorization Server |
| `inventory-api` | Custom Dockerfile (.NET 8) | 8081 | Inventory API |

## Startup Dependencies

1. `sqlserver` starts first (no dependencies).
2. `keycloak` starts first (no dependencies, uses embedded H2).
3. `inventory-api` depends on both `sqlserver` and `keycloak`.
4. API applies EF Core migrations on startup.
5. API retries Keycloak connection if not yet ready (OIDC discovery).

## Networking

- All containers on the same Docker network.
- Internal DNS: containers reference each other by service name (e.g., `sqlserver`, `keycloak`).
- Only API and Keycloak ports are exposed to the host.

---

# 15. Configuration Strategy

All configuration via environment variables, defined in docker-compose.yml.

| Variable | Purpose | Used By |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | API |
| `Authentication__Authority` | Keycloak realm URL | API |
| `Authentication__Audience` | Keycloak client ID | API |
| `ACCEPT_EULA` | SQL Server license | SQL Server |
| `SA_PASSWORD` | SQL Server admin password | SQL Server |
| `KEYCLOAK_ADMIN` | Keycloak admin username | Keycloak |
| `KEYCLOAK_ADMIN_PASSWORD` | Keycloak admin password | Keycloak |

**No secrets in code.** Connection strings, Keycloak URLs, and credentials are all externalized.

**ASP.NET Core configuration** reads environment variables via the standard configuration provider (`builder.Configuration`). Double-underscore (`__`) maps to section nesting.

---

# 16. Patterns Used

| Pattern | Where | Why |
|---|---|---|
| **CQRS** | Application layer (commands/queries) | Required by assessment. Natural fit for EF Core reads / Dapper writes. |
| **Repository** | Infrastructure layer (read/write repos) | Abstracts persistence behind interfaces. Enables testability. Enforces CQRS split. |
| **Dependency Injection** | API layer (composition root) | Required by ASP.NET Core. Enforces dependency inversion. |
| **Strategy** | Command/query handlers | Each handler is a strategy for a specific operation. Interchangeable via interface. |
| **Factory** | `IDbConnectionFactory` | Encapsulates Dapper connection creation. Single responsibility. |

---

# 17. Patterns Explicitly Avoided

| Pattern | Why Avoided |
|---|---|
| **MediatR** | Approved decision. Adds indirection without proportional benefit for 12 endpoints. The candidate should demonstrate the pattern, not the library. |
| **Generic Repository** | `IRepository<T>` with generic CRUD methods hides the meaningful differences between read and write operations. Specific interfaces are clearer and more testable. |
| **Unit of Work** | No cross-aggregate transactions in this project. Each command is self-contained. Adding UoW would be complexity without purpose. |
| **Stored Procedures** | Approved decision. Inline parameterized SQL is sufficient and simpler for ~6 write operations. |
| **Specification Pattern** | No complex query composition needed. Direct LINQ/EF Core queries are sufficient. |
| **Domain Events** | No cross-aggregate side effects. Movements update stock within the same transaction. No event bus needed. |
| **AutoMapper** | For ~6 DTOs, manual mapping is clearer, faster, and avoids a dependency. Mapping methods are explicit and testable. |

---

# 18. Architecture Risks

| Risk | Impact | Mitigation |
|---|---|---|
| **EF Core + Dapper consistency** | If EF Core migrations create a schema that Dapper SQL does not match, writes fail silently. | Write Dapper SQL that matches EF Core entity configurations. Test write operations against real DB in integration tests. |
| **Keycloak startup time** | Keycloak takes 15-30 seconds to start. API may fail to validate tokens during this window. | API uses retry logic or `depends_on` with health checks in docker-compose. Document startup wait in README. |
| **Transaction boundaries in RegisterMovement** | Stock check and update must be atomic to prevent race conditions. | Concurrency-safe conditional UPDATE with row count verification inside a Dapper transaction (ADR-008, approved). |
| **Docker networking** | Keycloak issuer URL differs between inside Docker (http://keycloak:8080) and outside (http://localhost:8080). JWT `iss` claim may not match API validation. | Configure Keycloak with a hostname that works for both contexts, or configure API to accept both issuers. Document in README. |
| **Excessive abstraction** | Over-layering can make the project harder to follow. | Keep each layer focused. No abstract base classes. No unnecessary indirection. If a class exists only to call another class, remove it. |
| **Testing complexity** | Too many mocks can make tests fragile. | Mock only repository interfaces. Keep handlers simple enough that tests are straightforward. |

---

# 19. Remaining Implementation-Phase Decisions

All architecture-level decisions have been approved. The following are implementation details that do not block architecture or require separate human approval. AI recommendations are provided; they can be adjusted during implementation without reopening architecture.

| ID | Decision | Classification | AI Recommendation | Blocks Implementation? |
|---|---|---|---|---|
| OD-006 | Field uniqueness | Implementation detail | No uniqueness beyond PK | No |
| OD-007 | Pagination | Implementation detail | No pagination — return all records | No |
| OD-011 | Category product count in response | Implementation detail | Exclude | No |
| OD-012 | Text field validation limits | Implementation detail | Name max 200 chars | No |
| OD-014 | Price constraints | Implementation detail | Price >= 0 | No |
| Mocking library | xUnit mocking framework | Implementation detail | NSubstitute (cleaner syntax) | No |

**None of these block implementation.** Each can be resolved at implementation time without architectural impact.

---

# 20. Approved Architecture Decisions Summary

All architecture decisions are human-approved. See `docs/architecture-decisions.md` for full ADR records.

| ADR | Decision | Status |
|---|---|---|
| ADR-001 | Lightweight CQRS, no MediatR | Approved |
| ADR-002 | Keycloak + OAuth2 Authorization Code + PKCE | Approved |
| ADR-003 | Hard delete | Approved |
| ADR-004 | Reject negative stock (409 Conflict) | Approved |
| ADR-005 | Auto-increment integer identifiers | Approved |
| ADR-006 | Immutable movements (append-only) | Approved |
| ADR-007 | Inline parameterized SQL, no SPs | Approved |
| ADR-008 | Concurrency-safe atomic stock update in Dapper transaction | Modified / Approved |
| ADR-009 | No roles, reject category deletion with products (409), audit timestamps (UTC), categoryName in response, server-assigned movement timestamp | Approved |
| ADR-010 | EF Core Migrations for schema management | Approved |

---

# Architecture Self-Review (Post-Consolidation)

1. Architecture satisfies the approved spec — **PASS.** All 12 functional requirements have a clear path: controller → handler → repository.
2. EF Core is used only for reads — **PASS.** Query handlers use read repositories backed by DbContext. No SaveChanges calls.
3. Dapper is used for writes — **PASS.** Command handlers use write repositories backed by Dapper with inline SQL.
4. CQRS remains lightweight — **PASS.** Simple interfaces, no MediatR, no pipeline.
5. MediatR has not been introduced — **PASS.**
6. OAuth2 remains Keycloak + Authorization Code + PKCE — **PASS.** Section 10 describes the integration.
7. No application code has been generated — **PASS.**
8. No unnecessary abstractions — **PASS.** No generic repository, no Unit of Work, no AutoMapper, no domain events.
9. Clean Code and SOLID supported — **PASS.** Each handler is SRP. Dependencies are injected. Interfaces are specific. Layers enforce dependency inversion.
10. Realistic for assessment timeline — **PASS.** Four projects, 12 endpoints, ~7 handlers each side, straightforward testing.
11. All approved decisions are reflected — **PASS.** ADR-008 concurrency-safe approach integrated. ADR-009 batch (audit fields, no roles, category deletion, categoryName, server timestamps) integrated. ADR-010 EF Core Migrations integrated.
12. No approved decision still appears as pending — **PASS.**
13. No remaining open decision blocks implementation — **PASS.** All 6 remaining are implementation details.

## Implementation Readiness Review

| Check | Status |
|---|---|
| Functional requirements sufficiently specified | PASS — 12 FRs in spec, all mapped to architecture |
| Architecture boundaries explicit | PASS — 4 projects with clear allowed/forbidden dependencies |
| Dependency direction valid | PASS — Domain → nothing, Application → Domain, Infrastructure → Application+Domain, API → Application+Infrastructure(DI) |
| Persistence responsibilities clear | PASS — EF Core reads, Dapper writes, no overlap |
| CQRS responsibilities clear | PASS — ICommandHandler/IQueryHandler, strict separation |
| Authentication architecture clear | PASS — Keycloak + JWT Bearer, Swagger PKCE |
| Transaction and concurrency behavior clear | PASS — ADR-008 atomic conditional UPDATE |
| Error handling strategy clear | PASS — global middleware, spec error contract |
| Test strategy clear | PASS — domain, handler, validation tests with mocked repos |
| Docker topology clear | PASS — 3 containers, startup dependencies, networking |
| Remaining open decisions do not create architectural ambiguity | PASS — all 6 are implementation details |

**Result: The project is READY for implementation planning.**

---
