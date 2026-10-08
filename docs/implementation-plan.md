# Implementation Plan — Inventory Management API

**Document type:** Implementation Plan
**Status:** APPROVED (Prompt 3.0.1)
**Version:** 1.0
**Date:** 2026-10-07

**Source documents:**
- `docs/spec.md` (v2.0, all decisions resolved)
- `docs/architecture.md` (v2.0, approved)
- `docs/architecture-decisions.md` (ADR-001 through ADR-011)

---

# Phase 1 — Solution Bootstrap

**Objective:** Create the .NET solution, projects, references, folder structure, and install required packages. Establish a baseline that compiles.

**Related ADRs:** ADR-001 (CQRS), ADR-005 (integer IDs), ADR-007 (Dapper), ADR-010 (EF Core Migrations).

**Actions:**
1. Create `InventoryApi.sln`.
2. Create projects:
   - `src/Inventory.Domain/` — .NET 8 class library, no NuGet packages.
   - `src/Inventory.Application/` — .NET 8 class library, references Domain.
   - `src/Inventory.Infrastructure/` — .NET 8 class library, references Application + Domain. Packages: `Microsoft.EntityFrameworkCore.SqlServer`, `Dapper`, `Microsoft.Data.SqlClient`.
   - `src/Inventory.Api/` — ASP.NET Core Web API, references Application + Infrastructure. Packages: `Swashbuckle.AspNetCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.EntityFrameworkCore.Design`.
   - `tests/Inventory.Tests/` — xUnit test project, references all src projects. Packages: `xUnit`, `NSubstitute`, `Microsoft.NET.Test.Sdk`, `FluentAssertions` (optional).
3. Create folder structure per architecture.md section 2.
4. Verify `dotnet build` succeeds.
5. Verify `dotnet test` runs (with zero tests).

**Dependencies:** None (first phase).

**Expected tests:** None yet — empty test project compiles.

**Validation:** Solution builds without errors. Project references match architecture dependency diagram.

**Human approval required:** No — mechanical scaffolding.

---

# Phase 2 — Domain Model + Domain Tests (TDD)

**Objective:** Implement domain entities, enums, and domain rules using TDD. Tests are written first.

**Related Spec:** FR-001 (product creation, stock=0), FR-005 (hard delete), FR-011/FR-012 (entry/exit), BR-006 (positive quantity), BR-010 (entry/exit semantics), BR-011 (no negative stock), BR-012 (hard delete), BR-013 (immutable movements).

**Related ADRs:** ADR-003 (hard delete), ADR-004 (no negative stock), ADR-005 (integer IDs), ADR-006 (immutable movements), ADR-009 (audit fields, name max 200, price >= 0).

### TDD Sequence

**Step 1: Write failing tests for Product entity.**

Tests:
- `Product_AddStock_IncreasesStockByQuantity`
- `Product_RemoveStock_DecreasesStockByQuantity`
- `Product_RemoveStock_ThrowsWhenQuantityExceedsStock`
- `Product_Constructor_SetsInitialStockToZero`
- `Product_Constructor_RejectsNullOrEmptyName`
- `Product_Constructor_RejectsNameExceeding200Characters`
- `Product_Constructor_RejectsNegativePrice`

**Step 2: Implement Product entity to make tests pass.**

**Step 3: Write failing tests for InventoryMovement.**

Tests:
- `InventoryMovement_Constructor_RejectsZeroQuantity`
- `InventoryMovement_Constructor_RejectsNegativeQuantity`
- `InventoryMovement_Constructor_SetsCreatedAtToUtcNow`
- `InventoryMovement_Constructor_AcceptsEntryType`
- `InventoryMovement_Constructor_AcceptsExitType`

**Step 4: Implement InventoryMovement to make tests pass.**

**Step 5: Create Category entity (minimal rules, no TDD cycle needed — trivial construction).**

**Step 6: Create MovementType enum.**

**Files created:**
- `src/Inventory.Domain/Entities/Product.cs`
- `src/Inventory.Domain/Entities/Category.cs`
- `src/Inventory.Domain/Entities/InventoryMovement.cs`
- `src/Inventory.Domain/Enums/MovementType.cs`
- `tests/Inventory.Tests/Domain/ProductTests.cs`
- `tests/Inventory.Tests/Domain/InventoryMovementTests.cs`

**Validation:** All domain tests pass. `dotnet test` green.

**Human approval required:** No — domain rules are already approved.

---

# Phase 3 — Application Contracts and CQRS Interfaces

**Objective:** Define command/query records, handler interfaces, repository interfaces, and application exceptions. No implementation of handlers yet.

**Related Spec:** All FRs (commands and queries for each use case).

**Related ADRs:** ADR-001 (lightweight CQRS), ADR-007 (inline SQL via Dapper), ADR-008 (RegisterMovement atomicity).

**Actions:**
1. Create `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>` interfaces.
2. Create command records:
   - `CreateProductCommand(string Name, int CategoryId, decimal Price)`
   - `UpdateProductCommand(int Id, string Name, int CategoryId, decimal Price)`
   - `DeleteProductCommand(int Id)`
   - `CreateCategoryCommand(string Name)`
   - `UpdateCategoryCommand(int Id, string Name)`
   - `DeleteCategoryCommand(int Id)`
   - `RegisterMovementCommand(int ProductId, MovementType Type, int Quantity)`
3. Create query records:
   - `GetProductQuery(int Id)`
   - `ListProductsQuery()`
   - `GetCategoryQuery(int Id)`
   - `ListCategoriesQuery()`
4. Create result DTOs:
   - `ProductResult(int Id, string Name, int CategoryId, string CategoryName, decimal Price, int Stock, DateTime CreatedAt, DateTime UpdatedAt)`
   - `CategoryResult(int Id, string Name, DateTime CreatedAt, DateTime UpdatedAt)`
   - `MovementResult(int Id, int ProductId, string Type, int Quantity, DateTime CreatedAt)`
5. Create repository interfaces:
   - `IProductReadRepository` — `GetByIdAsync`, `GetAllAsync`
   - `IProductWriteRepository` — `CreateAsync`, `UpdateAsync`, `DeleteAsync`
   - `ICategoryReadRepository` — `GetByIdAsync`, `GetAllAsync`, `ExistsAsync`
   - `ICategoryWriteRepository` — `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `HasProductsAsync`
   - `IMovementWriteRepository` — `RegisterMovementAsync` (atomic: validate stock + insert movement + update stock)
   - `IDbConnectionFactory` — `CreateConnectionAsync`
6. Create application exceptions:
   - `NotFoundException`
   - `ConflictException`

**Files created:**
- `src/Inventory.Application/Interfaces/ICommandHandler.cs`
- `src/Inventory.Application/Interfaces/IQueryHandler.cs`
- `src/Inventory.Application/Interfaces/IProductReadRepository.cs`
- `src/Inventory.Application/Interfaces/IProductWriteRepository.cs`
- `src/Inventory.Application/Interfaces/ICategoryReadRepository.cs`
- `src/Inventory.Application/Interfaces/ICategoryWriteRepository.cs`
- `src/Inventory.Application/Interfaces/IMovementWriteRepository.cs`
- `src/Inventory.Application/Interfaces/IDbConnectionFactory.cs`
- `src/Inventory.Application/Commands/*.cs` (7 command records)
- `src/Inventory.Application/Queries/*.cs` (4 query records)
- `src/Inventory.Application/Results/*.cs` (3 result DTOs)
- `src/Inventory.Application/Exceptions/NotFoundException.cs`
- `src/Inventory.Application/Exceptions/ConflictException.cs`

**Dependencies:** Phase 1 (solution exists), Phase 2 (Domain entities referenced by commands).

**Expected tests:** None in this phase — these are contracts/interfaces.

**Validation:** Solution builds. No handler implementations yet.

**Human approval required:** No — contracts follow approved spec and architecture.

---

# Phase 4 — Command Handlers + Tests (TDD)

**Objective:** Implement all command handlers using TDD. Tests mock repository interfaces.

**Related Spec:** FR-001, FR-004, FR-005, FR-006, FR-009, FR-010, FR-011, FR-012. AC-001, AC-004, AC-005, AC-007, AC-008, AC-011, AC-012, AC-014, AC-015, AC-020.

**Related ADRs:** ADR-001 (CQRS), ADR-003 (hard delete), ADR-004 (no negative stock), ADR-006 (immutable), ADR-008 (concurrency-safe stock update), ADR-009 (reject category deletion with products).

### TDD Sequence

**Product commands:**

1. Write failing test: `CreateProductHandler_ValidCommand_ReturnsCreatedId` → implement handler.
2. Write failing test: `CreateProductHandler_NonexistentCategory_ThrowsNotFoundException` → add category check.
3. Write failing test: `UpdateProductHandler_ValidCommand_CallsRepository` → implement handler.
4. Write failing test: `UpdateProductHandler_NonexistentProduct_ThrowsNotFoundException` → add check.
5. Write failing test: `DeleteProductHandler_ValidId_CallsRepository` → implement handler.
6. Write failing test: `DeleteProductHandler_NonexistentProduct_ThrowsNotFoundException` → add check.

**Category commands:**

7. Write failing test: `CreateCategoryHandler_ValidCommand_ReturnsCreatedId` → implement handler.
8. Write failing test: `UpdateCategoryHandler_NonexistentCategory_ThrowsNotFoundException` → implement.
9. Write failing test: `DeleteCategoryHandler_CategoryHasProducts_ThrowsConflictException` → implement.
10. Write failing test: `DeleteCategoryHandler_NoProducts_CallsRepository` → implement.

**Movement commands:**

11. Write failing test: `RegisterMovementHandler_Entry_CallsRepository` → implement handler.
12. Write failing test: `RegisterMovementHandler_NonexistentProduct_ThrowsNotFoundException` → add check.
13. Write failing test: `RegisterMovementHandler_ExitInsufficientStock_ThrowsConflictException` → implement.
14. Write failing test: `RegisterMovementHandler_ExitSufficientStock_Succeeds` → implement.

**Files created:**
- `src/Inventory.Application/Commands/Handlers/CreateProductCommandHandler.cs`
- `src/Inventory.Application/Commands/Handlers/UpdateProductCommandHandler.cs`
- `src/Inventory.Application/Commands/Handlers/DeleteProductCommandHandler.cs`
- `src/Inventory.Application/Commands/Handlers/CreateCategoryCommandHandler.cs`
- `src/Inventory.Application/Commands/Handlers/UpdateCategoryCommandHandler.cs`
- `src/Inventory.Application/Commands/Handlers/DeleteCategoryCommandHandler.cs`
- `src/Inventory.Application/Commands/Handlers/RegisterMovementCommandHandler.cs`
- `tests/Inventory.Tests/Commands/CreateProductCommandHandlerTests.cs`
- `tests/Inventory.Tests/Commands/UpdateProductCommandHandlerTests.cs`
- `tests/Inventory.Tests/Commands/DeleteProductCommandHandlerTests.cs`
- `tests/Inventory.Tests/Commands/CreateCategoryCommandHandlerTests.cs`
- `tests/Inventory.Tests/Commands/UpdateCategoryCommandHandlerTests.cs`
- `tests/Inventory.Tests/Commands/DeleteCategoryCommandHandlerTests.cs`
- `tests/Inventory.Tests/Commands/RegisterMovementCommandHandlerTests.cs`

**Dependencies:** Phase 2 (Domain), Phase 3 (contracts).

**Validation:** All command handler tests pass.

**Human approval required:** No.

---

# Phase 5 — Query Handlers + Tests (TDD)

**Objective:** Implement all query handlers using TDD. Tests mock read repository interfaces.

**Related Spec:** FR-002, FR-003, FR-007, FR-008. AC-002, AC-003, AC-006, AC-009, AC-010, AC-013.

**Related ADRs:** ADR-001 (CQRS — queries use EF Core via read repos).

### TDD Sequence

1. Write failing test: `GetProductHandler_ExistingProduct_ReturnsResult` → implement.
2. Write failing test: `GetProductHandler_NonexistentProduct_ThrowsNotFoundException` → add check.
3. Write failing test: `ListProductsHandler_ReturnsAllProducts` → implement.
4. Write failing test: `GetCategoryHandler_ExistingCategory_ReturnsResult` → implement.
5. Write failing test: `GetCategoryHandler_NonexistentCategory_ThrowsNotFoundException` → add check.
6. Write failing test: `ListCategoriesHandler_ReturnsAllCategories` → implement.

**Files created:**
- `src/Inventory.Application/Queries/Handlers/GetProductQueryHandler.cs`
- `src/Inventory.Application/Queries/Handlers/ListProductsQueryHandler.cs`
- `src/Inventory.Application/Queries/Handlers/GetCategoryQueryHandler.cs`
- `src/Inventory.Application/Queries/Handlers/ListCategoriesQueryHandler.cs`
- `tests/Inventory.Tests/Queries/GetProductQueryHandlerTests.cs`
- `tests/Inventory.Tests/Queries/ListProductsQueryHandlerTests.cs`
- `tests/Inventory.Tests/Queries/GetCategoryQueryHandlerTests.cs`
- `tests/Inventory.Tests/Queries/ListCategoriesQueryHandlerTests.cs`

**Dependencies:** Phase 3 (contracts).

**Validation:** All query handler tests pass.

**Human approval required:** No.

---

# Phase 6 — Infrastructure: Persistence

**Objective:** Implement EF Core DbContext, entity configurations, migrations, Dapper repositories, and DbConnectionFactory.

**Related Spec:** BR-001 (EF reads), BR-002 (Dapper writes), BR-004 (SQL Server), Section 10.

**Related ADRs:** ADR-005 (integer IDs), ADR-007 (inline SQL), ADR-008 (concurrency-safe stock update), ADR-009 (audit fields, name 200 chars), ADR-010 (EF Core Migrations).

**Actions:**

1. **InventoryDbContext** — DbSets for Product, Category, InventoryMovement. `AsNoTracking` by default for read queries.
2. **Entity configurations** — `ProductConfiguration`, `CategoryConfiguration`, `InventoryMovementConfiguration`. Column types, max lengths (200 for names), FK relationships, indexes.
3. **Initial migration** — `dotnet ef migrations add InitialCreate`.
4. **DbConnectionFactory** — creates `SqlConnection` from connection string env var.
5. **Read repositories** — `ProductReadRepository`, `CategoryReadRepository` using DbContext.
6. **Write repositories:**
   - `ProductWriteRepository` — inline parameterized SQL for create, update, delete.
   - `CategoryWriteRepository` — inline SQL, `HasProductsAsync` check.
   - `MovementWriteRepository` — atomic Dapper transaction: conditional `UPDATE Products SET Stock = Stock - @Quantity WHERE Id = @ProductId AND Stock >= @Quantity` for exits, row count verification, movement INSERT, commit/rollback.
7. **DependencyInjection.cs** — extension method registering all infrastructure services.

**Files created:**
- `src/Inventory.Infrastructure/Persistence/InventoryDbContext.cs`
- `src/Inventory.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`
- `src/Inventory.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs`
- `src/Inventory.Infrastructure/Persistence/Configurations/InventoryMovementConfiguration.cs`
- `src/Inventory.Infrastructure/Persistence/DbConnectionFactory.cs`
- `src/Inventory.Infrastructure/Persistence/Repositories/ProductReadRepository.cs`
- `src/Inventory.Infrastructure/Persistence/Repositories/CategoryReadRepository.cs`
- `src/Inventory.Infrastructure/Persistence/Repositories/ProductWriteRepository.cs`
- `src/Inventory.Infrastructure/Persistence/Repositories/CategoryWriteRepository.cs`
- `src/Inventory.Infrastructure/Persistence/Repositories/MovementWriteRepository.cs`
- `src/Inventory.Infrastructure/DependencyInjection.cs`
- `src/Inventory.Infrastructure/Migrations/` (generated)

**Why not TDD here:** Repository implementations contain SQL and EF Core queries. These are infrastructure concerns best verified by integration testing against a real database, not unit tests with mocked DbContext. The handlers that use these repositories are already unit-tested in Phases 4-5.

**Dependencies:** Phase 2 (Domain entities), Phase 3 (interfaces).

**Validation:** Solution builds. Migration generates. SQL in write repositories matches EF Core configuration.

**Human approval required:** No.

---

# Phase 7 — API Layer

**Objective:** Implement controllers, request/response DTOs, error handling middleware, Swagger configuration, and DI composition.

**Related Spec:** Section 6 (REST contract), Section 7 (request/response models), Section 8 (error contract), AC-018 (unauthorized), AC-019 (invalid request).

**Related ADRs:** ADR-001 (handlers injected into controllers), ADR-002 (JWT Bearer), ADR-009 (no roles).

**Actions:**

1. **Request DTOs** — `CreateProductRequest`, `UpdateProductRequest`, `CreateCategoryRequest`, `UpdateCategoryRequest`, `CreateMovementRequest`. Validation attributes (Required, MaxLength(200), Range for price).
2. **Response DTOs** — `ProductResponse`, `CategoryResponse`, `MovementResponse`, `ErrorResponse`.
3. **Controllers:**
   - `ProductsController` — POST, GET, GET/{id}, PUT/{id}, DELETE/{id}.
   - `CategoriesController` — POST, GET, GET/{id}, PUT/{id}, DELETE/{id}.
   - `InventoryMovementsController` — POST /api/products/{productId}/movements.
4. **ExceptionHandlingMiddleware** — maps NotFoundException→404, ConflictException→409, ValidationException→400/422, Exception→500.
5. **Program.cs** — DI composition, authentication (placeholder until Phase 8), Swagger, middleware pipeline, migration execution on startup.
6. **Swagger configuration** — endpoint documentation (OAuth2 config added in Phase 8).

**Files created:**
- `src/Inventory.Api/Models/Requests/*.cs`
- `src/Inventory.Api/Models/Responses/*.cs`
- `src/Inventory.Api/Controllers/ProductsController.cs`
- `src/Inventory.Api/Controllers/CategoriesController.cs`
- `src/Inventory.Api/Controllers/InventoryMovementsController.cs`
- `src/Inventory.Api/Middleware/ExceptionHandlingMiddleware.cs`
- `src/Inventory.Api/Program.cs`

**Dependencies:** Phase 4 (command handlers), Phase 5 (query handlers), Phase 6 (infrastructure DI).

**Expected tests:** Controller tests are optional — business logic is in handlers which are already tested. If time permits, validation attribute tests for request DTOs.

**Validation:** API starts locally. Swagger UI loads. Endpoints respond (auth not yet enforced).

**Human approval required:** No.

---

# Phase 8 — OAuth2 / Keycloak Integration

**Objective:** Configure Keycloak, create realm import, configure JWT Bearer in API, configure Swagger OAuth2.

**Related Spec:** Section 9 (authentication requirements), BR-003 (OAuth2), AC-018 (unauthorized request).

**Related ADRs:** ADR-002 (Keycloak + Authorization Code + PKCE).

**Actions:**

1. **Keycloak realm export JSON** — create `docker/keycloak/realm-export.json` with:
   - Realm: `inventory`
   - Public client: `inventory-api-swagger` (Authorization Code + PKCE enabled)
   - Confidential client (optional, for API audience validation): `inventory-api`
   - Test user: `testuser` / `testpassword`
   - Redirect URIs for Swagger
2. **API JWT Bearer configuration** — configure `AddAuthentication().AddJwtBearer()` with:
   - Authority from env var (`Authentication__Authority`)
   - Audience from env var (`Authentication__Audience`)
   - Token validation parameters
3. **Swagger OAuth2 configuration** — add security definition for Authorization Code + PKCE:
   - Authorization URL: Keycloak's `/protocol/openid-connect/auth`
   - Token URL: Keycloak's `/protocol/openid-connect/token`
   - Client ID: `inventory-api-swagger`
   - PKCE enabled
4. **`[Authorize]` on all controllers.**
5. **Handle Keycloak issuer URL** — configure `ValidIssuers` to accept both internal Docker URL and localhost URL.

**Files created:**
- `docker/keycloak/realm-export.json`
- Modified: `src/Inventory.Api/Program.cs` (auth + Swagger OAuth2)

**Dependencies:** Phase 7 (API layer exists).

**Why not TDD:** Keycloak configuration and JWT Bearer setup are infrastructure integration. Verified by running the full stack and testing with Swagger.

**Validation:** API rejects requests without token (401). Swagger OAuth2 flow works against Keycloak. Valid token grants access.

**Human approval required:** No — follows approved ADR-002 precisely.

---

# Phase 9 — Docker

**Objective:** Create Dockerfile, docker-compose.yml, configure all containers, verify `docker-compose up` starts the full system.

**Related Spec:** NFR-003 (dockerized API), NFR-004 (dockerized SQL Server), NFR-005 (docker-compose), NFR-006 (env vars), BR-005.

**Related ADRs:** ADR-010 (EF Core Migrations on startup).

**Actions:**

1. **Dockerfile** — multi-stage build for .NET 8 API.
2. **docker-compose.yml** — three services:
   - `sqlserver` — `mcr.microsoft.com/mssql/server:2022-latest`, port 1433, env vars.
   - `keycloak` — `quay.io/keycloak/keycloak:latest`, port 8080, realm import volume.
   - `inventory-api` — custom build, port 8081, depends_on sqlserver + keycloak.
3. **Environment variables** — connection string, Keycloak authority/audience, SQL Server credentials.
4. **Health checks / startup order** — API waits for SQL Server and Keycloak.
5. **Migration execution** — API applies EF Core migrations on startup.

**Files created:**
- `Dockerfile`
- `docker-compose.yml`

**Dependencies:** Phase 8 (Keycloak config), Phase 6 (migrations).

**Validation:** `docker-compose up` starts all three containers. API is accessible on port 8081. Swagger loads. Keycloak login works. Endpoints respond with valid token.

**Human approval required:** No.

---

# Phase 10 — End-to-End Verification

**Objective:** Verify all functional requirements work through the full stack via Docker.

**Related Spec:** All FRs, all ACs.

**Actions:**

1. Start system with `docker-compose up`.
2. Obtain token via Swagger OAuth2 flow.
3. Test each endpoint:
   - Create category → 201.
   - List categories → 200.
   - Create product with valid category → 201.
   - Get product → 200 (includes categoryName).
   - Update product → 200.
   - Register entry movement → 201, stock increases.
   - Register exit movement → 201, stock decreases.
   - Register exit movement exceeding stock → 409.
   - Delete category with products → 409.
   - Delete product → 204.
   - Delete category (no products) → 204.
   - Request without token → 401.
   - Invalid request body → 400.
   - Nonexistent resource → 404.
4. Fix any issues found.

**Dependencies:** Phase 9 (Docker working).

**Validation:** All acceptance criteria pass.

**Human approval required:** Yes — review before documentation phase.

---

# Phase 11 — Documentation

**Objective:** Create README.md with full setup, run, test, and architecture instructions.

**Related Spec:** NFR-007 (Swagger), NFR-008 (README).

**Actions:**

1. **README.md** sections:
   - Project overview
   - Architecture summary (Clean Architecture, CQRS, EF Core reads, Dapper writes)
   - Prerequisites (Docker, Docker Compose)
   - Quick start (`docker-compose up`)
   - Authentication (Keycloak, test user credentials, Swagger OAuth2 flow)
   - API endpoints summary
   - Running tests (`dotnet test`)
   - Local development/debugging (without Docker)
   - Technology stack
   - Project structure
2. Verify Swagger documents all endpoints correctly.

**Files created:**
- `README.md`

**Dependencies:** Phase 10 (everything works).

**Validation:** README instructions can be followed from scratch to a running system.

**Human approval required:** No.

---

# Phase 12 — Final Compliance Review

**Objective:** QA pass against every requirement in the technical assessment PDF.

**Related Spec:** All sections. All NFRs.

**Checklist:**

| # | Requirement | Verification |
|---|---|---|
| 1 | CRUD productos | All product endpoints work |
| 2 | CRUD categorías | All category endpoints work |
| 3 | Movimientos de inventario | Entry/exit work, stock updates |
| 4 | .NET 8+ | `dotnet --version` in Dockerfile |
| 5 | SQL Server | Container runs, data persists |
| 6 | EF Core lecturas | Query handlers use DbContext |
| 7 | Dapper escrituras | Command handlers use Dapper |
| 8 | OAuth2 | Keycloak + Auth Code + PKCE |
| 9 | Dockerizada API | Dockerfile exists, builds |
| 10 | Dockerizada BD | SQL Server in docker-compose |
| 11 | docker-compose | Single command starts everything |
| 12 | Swagger | All endpoints documented |
| 13 | Pruebas unitarias | `dotnet test` passes, adequate coverage |
| 14 | SOLID | DI, SRP handlers, ISP (specific interfaces) |
| 15 | Clean Code | English, no blank lines, <25 line methods |
| 16 | TDD | Tests written before/with implementation |
| 17 | Patrones | CQRS, Repository, Factory, Strategy, DI |
| 18 | CQRS | Lightweight, no MediatR |
| 19 | README.md | Setup, run, test instructions |
| 20 | Código en inglés | Verify no Spanish in code/comments |
| 21 | Sin código comentado | Grep for commented code |
| 22 | 1 clase/archivo | Verify file structure |
| 23 | Env vars | No hardcoded config |
| 24 | Métodos autodescriptivos | Review naming |
| 25 | Responsabilidad única, <25 líneas | Verify method lengths |
| 26 | Sin anidación profunda | Verify nesting levels |
| 27 | Pasar objetos | Verify parameter patterns |
| 28 | Repositorio público | Push to GitHub |
| 29 | Evidencia IA | docs/ai-development-log.md complete |
| 30 | Spec-Driven Development | docs/spec.md exists |
| 31 | Decisiones arquitectura | docs/architecture-decisions.md exists |

**Dependencies:** Phase 11 (documentation).

**Validation:** All 31 checks pass.

**Human approval required:** Yes — final review before submission.

---

# Phase 13 — Submission Preparation

**Objective:** Prepare the repository for submission. Answer the 3 decision-making questions. Final push to public repo.

**Actions:**
1. Answer the 3 "toma de decisiones" questions (human writes, AI assists if asked).
2. Verify all files are committed.
3. Verify repo is public.
4. Final smoke test: clone repo, `docker-compose up`, test endpoints.

**Dependencies:** Phase 12 (compliance review).

**Human approval required:** Yes — final submission gate.

---

# TDD Strategy

## How TDD Is Demonstrated

TDD is applied in phases where it adds genuine value:

| Phase | TDD Applied? | Reason |
|---|---|---|
| Phase 2 — Domain | **Yes** | Domain rules are pure logic — ideal for TDD. Tests written first, then entities. |
| Phase 4 — Command Handlers | **Yes** | Handlers contain business logic. Tests mock repos, written before handler implementation. |
| Phase 5 — Query Handlers | **Yes** | Query handlers have not-found logic. Tests mock read repos. |
| Phase 6 — Infrastructure | **No** | Repository implementations are infrastructure. Verified through integration (Phase 10). |
| Phase 7 — API Layer | **Partial** | Validation attribute tests can be TDD. Controller tests are optional. |
| Phase 8 — Keycloak | **No** | Configuration, not logic. Verified through integration. |
| Phase 9 — Docker | **No** | Infrastructure. Verified through smoke testing. |

## Test Naming Convention

```
MethodUnderTest_Scenario_ExpectedBehavior
```

Examples:
- `Product_RemoveStock_ThrowsWhenQuantityExceedsStock`
- `CreateProductHandler_NonexistentCategory_ThrowsNotFoundException`
- `RegisterMovementHandler_ExitInsufficientStock_ThrowsConflictException`

## Test Organization

```
tests/Inventory.Tests/
├── Domain/
│   ├── ProductTests.cs
│   └── InventoryMovementTests.cs
├── Commands/
│   ├── CreateProductCommandHandlerTests.cs
│   ├── UpdateProductCommandHandlerTests.cs
│   ├── DeleteProductCommandHandlerTests.cs
│   ├── CreateCategoryCommandHandlerTests.cs
│   ├── UpdateCategoryCommandHandlerTests.cs
│   ├── DeleteCategoryCommandHandlerTests.cs
│   └── RegisterMovementCommandHandlerTests.cs
└── Queries/
    ├── GetProductQueryHandlerTests.cs
    ├── ListProductsQueryHandlerTests.cs
    ├── GetCategoryQueryHandlerTests.cs
    └── ListCategoriesQueryHandlerTests.cs
```

---

# Proposed Implementation Prompts

## Prompt 3.1 — Solution Bootstrap

**Scope:** Create solution, projects, references, folders, packages. Verify build.
**Expected output:** Compilable .NET 8 solution with 5 projects, correct references and packages.
**Related requirements:** Architecture section 2.
**Expected tests:** None — empty test project compiles.

## Prompt 3.2 — Domain Model (TDD)

**Scope:** Domain entities, enums, domain rules. Tests first.
**Expected output:** Product, Category, InventoryMovement, MovementType. All domain tests pass.
**Related requirements:** FR-001, FR-011, FR-012, BR-006, BR-010, BR-011, BR-013.
**Expected tests:** ~12 domain tests.

## Prompt 3.3 — Application Contracts

**Scope:** Commands, queries, handler interfaces, repository interfaces, DTOs, exceptions.
**Expected output:** All Application layer contracts. No handler implementations.
**Related requirements:** All FRs (contracts for each use case).
**Expected tests:** None — interfaces and records only.

## Prompt 3.4 — Command Handlers (TDD)

**Scope:** All 7 command handlers with tests first.
**Expected output:** Implemented handlers, all tests pass.
**Related requirements:** FR-001, FR-004, FR-005, FR-006, FR-009, FR-010, FR-011, FR-012.
**Expected tests:** ~14 command handler tests.

## Prompt 3.5 — Query Handlers (TDD)

**Scope:** All 4 query handlers with tests first.
**Expected output:** Implemented handlers, all tests pass.
**Related requirements:** FR-002, FR-003, FR-007, FR-008.
**Expected tests:** ~6 query handler tests.

## Prompt 3.6 — Infrastructure: Persistence

**Scope:** DbContext, entity configs, migrations, Dapper repos, DbConnectionFactory, DI registration.
**Expected output:** Full persistence layer. Migration generated. SQL matches EF Core config.
**Related requirements:** BR-001, BR-002, ADR-007, ADR-008, ADR-010.
**Expected tests:** None (infrastructure — verified in integration).

## Prompt 3.7 — API Layer

**Scope:** Controllers, DTOs, error middleware, Swagger, Program.cs.
**Expected output:** API layer compiles. Swagger loads. Endpoints defined.
**Related requirements:** Section 6, Section 7, Section 8, NFR-007.
**Expected tests:** Optional validation tests.

## Prompt 3.8 — OAuth2 / Keycloak

**Scope:** Realm export, JWT Bearer config, Swagger OAuth2, [Authorize].
**Expected output:** Keycloak realm file. API validates tokens. Swagger OAuth2 flow configured.
**Related requirements:** Section 9, BR-003, ADR-002, AC-018.
**Expected tests:** None (integration verified in Phase 10).

## Prompt 3.9 — Docker

**Scope:** Dockerfile, docker-compose.yml, env vars, health checks.
**Expected output:** `docker-compose up` starts all 3 containers. API accessible.
**Related requirements:** NFR-003, NFR-004, NFR-005, NFR-006.
**Expected tests:** Smoke test via docker-compose.

## Prompt 3.10 — End-to-End Verification

**Scope:** Full stack testing. All acceptance criteria verified.
**Expected output:** All ACs pass. Bug fixes if needed.
**Related requirements:** All ACs.
**Expected tests:** Manual acceptance test results.

## Prompt 3.11 — Documentation and Compliance

**Scope:** README.md, final compliance review against all 31 assessment requirements.
**Expected output:** Complete README. Compliance checklist fully green.
**Related requirements:** NFR-007, NFR-008, all Clean Code rules.
**Expected tests:** Compliance checklist.

## Prompt 3.12 — Submission Preparation

**Scope:** Decision-making questions, final commit, public repo, smoke test.
**Expected output:** Repository ready for submission.
**Related requirements:** All.
**Expected tests:** Clone-and-run smoke test.

---

# Implementation Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Keycloak realm configuration complexity | Auth may not work on first try | Test Keycloak standalone before full integration |
| Docker issuer URL mismatch | 401 errors despite valid tokens | Configure ValidIssuers for both internal and localhost URLs |
| EF Core migration + Dapper SQL mismatch | Write operations fail | Verify Dapper SQL column names match EF Core configurations |
| Concurrency-safe stock update SQL | Incorrect SQL causes data issues | Test RegisterMovement with concurrent requests |
| Time pressure (deadline Oct 9) | Incomplete submission | Prioritize core requirements. Docker + auth can be simplified if needed |
| Keycloak Docker image size | Slow first pull on evaluator's machine | Document in README, suggest `docker-compose pull` first |

---
