# Architecture Decision Records — Inventory Management API

**Document type:** Architecture Decision Log
**Date:** 2026-10-07
**Last updated:** 2026-10-07 (Prompt 2.2 — Final implementation decisions)

**Related documents:**
- Specification: [spec.md](spec.md) (requirements these decisions address)
- Architecture: [architecture.md](architecture.md) (where these decisions are applied)
- AI usage log: [ai-development-log.md](ai-development-log.md) (full prompt history)

---

## ADR-001: CQRS Implementation Level

**Context:** The assessment mentions CQRS as desirable. The project already requires EF Core for reads and Dapper for writes, which naturally maps to query/command separation.
**AI recommendation:** Lightweight manual command/query handlers without MediatR.
**Human decision:** Accepted AI recommendation.
**Status:** Accepted.
**Reason:** Natural fit with EF Core/Dapper split. Demonstrates pattern knowledge without introducing a library dependency. Each handler is a small, focused class aligning with SRP and Clean Code rules. MediatR would add indirection without proportional benefit for 12 endpoints.
**Consequences:** Application layer will contain `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>` interfaces with concrete handlers. No MediatR pipeline behaviors. Commands use Dapper, queries use EF Core.

---

## ADR-002: OAuth2 Flow and Provider

**Context:** The assessment requires "Autenticación mediante OAuth2" without specifying the flow, provider, or token format.

### Original Recommendation — REJECTED

**AI recommendation:** Resource Owner Password Credentials (ROPC) grant with a built-in token endpoint.
**Human decision:** Rejected.
**Status:** Rejected.
**Reason:** ROPC requires the client to directly handle user credentials. Not appropriate for a new OAuth2 design. Provides a weaker, less defensible security design for a Senior-level assessment. The assessment explicitly requires OAuth2, so a modern flow should be used.

### Revised Recommendation — APPROVED

**AI recommendation:** Keycloak with Authorization Code + PKCE.
**Human decision:** Accepted revised recommendation.
**Status:** Accepted.
**Reason:** The assessment required OAuth2 but did not mandate a specific provider. Keycloak was selected because the solution needed to be self-contained and reproducible. Using an external provider such as Auth0 or Azure AD would require external tenant configuration. With Keycloak, the realm, OAuth client, evaluation user, and complete Authorization Code + PKCE flow can be included in the Docker environment. Implementing a custom JWT/login system was not selected because JWT alone would not represent the required OAuth2 authorization-server flow. Authorization Code + PKCE is the current best practice for OAuth2. The API acts purely as a Resource Server (validates tokens, does not issue them).
**Documentation note:** The approach is documented as "OAuth2 Authorization Code with PKCE using Keycloak" without referencing a specific future OAuth specification version.
**Consequences:** docker-compose includes a Keycloak container. A realm import JSON file is committed to the repo with: realm, public client, and test user. The API uses standard ASP.NET Core JWT Bearer authentication against Keycloak's OIDC discovery endpoint. No custom token endpoint in the API.

**Hostname strategy (updated Prompt 3.8.2):**
- Keycloak image: `quay.io/keycloak/keycloak:26.0` (Hostname v2 support).
- `KC_HOSTNAME=http://localhost:8080` (canonical issuer, Hostname v2 format).
- `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true` (allows Docker-internal OIDC discovery).
- `KC_HTTP_ENABLED=true` (HTTP for local development).
- Canonical issuer: `http://localhost:8080/realms/inventory`.
- API `Authority`: Docker-internal URL for metadata retrieval.
- API `ValidIssuer`: canonical public URL for JWT `iss` validation.
- No `ValidIssuers` (plural) workaround. Strict single-issuer validation.
- AI initially proposed `KC_HOSTNAME_URL` (Hostname v1). Human identified as outdated. Corrected to `KC_HOSTNAME` (Hostname v2).

---

## ADR-003: Deletion Behavior

**Context:** The assessment requires delete operations for products and categories but does not specify hard vs soft delete.
**AI recommendation:** Hard delete.
**Human decision:** Accepted AI recommendation.
**Status:** Accepted.
**Reason:** The assessment says "eliminar" (delete). Soft delete was not mentioned and would add unrequested complexity (IsDeleted flag, filtered queries). Hard delete is simpler, easier to test, and aligned with what was asked.
**Consequences:** DELETE endpoints permanently remove records. Inventory movements referencing a deleted product may be cascade-deleted or handled via FK constraints (to be decided during architecture/DBA phase).

---

## ADR-004: Negative Stock Prevention

**Context:** The assessment does not specify whether inventory can go below zero.
**AI recommendation:** Reject exit movements that would produce negative stock with 409 Conflict.
**Human decision:** Accepted AI recommendation.
**Status:** Accepted.
**Reason:** An inventory system allowing negative stock is hard to defend. This is a natural business rule that demonstrates validation capability. Adds one condition check, not significant complexity. Produces a testable scenario.
**Consequences:** Exit movement command handler must check current stock before applying the movement. If `currentStock < requestedQuantity`, return 409 Conflict. This adds a business rule test case.

---

## ADR-005: Identifier Type

**Context:** The assessment does not specify identifier format.
**AI recommendation:** Auto-increment integer.
**Human decision:** Accepted AI recommendation.
**Status:** Accepted.
**Reason:** Simpler for a technical test. Better SQL Server clustered index performance. Cleaner URLs for Swagger testing. EF Core handles auto-increment by default.
**Consequences:** All entities use `int` primary keys with SQL Server `IDENTITY`. URLs follow the pattern `/api/products/42`.

---

## ADR-006: Movement Immutability

**Context:** The assessment says "registrar movimientos de inventario" (register movements) without mentioning editing or deleting them.
**AI recommendation:** Immutable (append-only).
**Human decision:** Accepted AI recommendation.
**Status:** Accepted.
**Reason:** The assessment asks for registration, not management. Immutable movements are standard in inventory/accounting systems. Reduces scope (no PUT/DELETE for movements). Reduces risk (no stock recalculation on edit/delete). Easy to defend in interview.
**Consequences:** Only POST endpoint for movements. No PUT or DELETE. Corrections require compensating entries (not explicitly required by the assessment but implied by immutability).

---

## ADR-007: Dapper Write Style

**Context:** The assessment requires Dapper for write operations but does not specify Stored Procedures vs inline SQL.
**AI recommendation:** Inline parameterized SQL.
**Human decision:** Accepted AI recommendation.
**Status:** Accepted.
**Reason:** The assessment says "Dapper para escrituras," not "Stored Procedures." Inline parameterized SQL is the most natural Dapper usage. Keeps the project simpler (no SP scripts to manage alongside EF Core). Secure (parameterized, no injection risk). For ~6 write operations, inline SQL is cleaner and easier to review.
**Consequences:** Command handlers write SQL directly as parameterized strings via Dapper. No SP creation scripts. Schema is managed by EF Core migrations only.

---

## ADR-008: RegisterMovement Stock Check Atomicity

**Context:** RegisterMovement must read current stock, validate sufficiency for exits, insert the movement, and update stock. These operations must be atomic and concurrency-safe.
**AI recommendation:** Single Dapper transaction that reads stock, validates, inserts movement, and updates stock.
**Human decision:** Modified and accepted.
**Status:** Modified / Approved.
**Modification:** The human reviewer strengthened the concurrency requirement. A transaction alone does not prevent two concurrent operations from validating against the same stock value. The approved approach must guarantee atomic stock consumption.

**Approved behavior:**
- RegisterMovement executes atomically within a single Dapper transaction.
- For EXIT movements, stock reduction must be concurrency-safe using an atomic conditional update (e.g., `UPDATE Products SET Stock = Stock - @Quantity WHERE Id = @ProductId AND Stock >= @Quantity`).
- The affected row count must be verified. If zero rows are affected, stock is insufficient.
- If stock is insufficient, the movement must NOT be recorded and the transaction must be rolled back.
- Stock modification and movement insertion must be committed or rolled back together.
- Concurrent requests must not be able to consume the same available stock.

**Reason for modification:** A simple read-then-write within a transaction can still allow two concurrent transactions to read the same stock value before either writes. The atomic conditional update pattern eliminates this race condition at the SQL level.

**Consequences:** The write repository for RegisterMovement uses a conditional UPDATE with a WHERE clause that checks stock sufficiency. The row count determines success or failure. No separate stock read query is needed for exits.

---

## ADR-009: Remaining Open Decisions Batch

**Context:** Five open decisions from the spec needed resolution during the architecture phase.
**AI recommendation:** Resolve all five as recommended.
**Human decision:** Accepted all five.
**Status:** Accepted.

### OD-002 — Roles / Scopes
**Decision:** No application-specific roles or complex authorization rules. Authenticated access to protected endpoints is sufficient.
**Reason:** Assessment says "autenticación," not "autorización."
**Consequences:** All authenticated users have equal access. No role claims, no policy-based authorization.

### OD-004 — Category Deletion with Products
**Decision:** Reject deletion with HTTP 409 Conflict when products still reference the category.
**Reason:** Safe, prevents accidental data loss. Forces explicit product reassignment first.
**Consequences:** DeleteCategory command handler must check for associated products. FK constraint in database enforces this at the persistence level.

### OD-009 — Audit Fields
**Decision:** Include CreatedAt and UpdatedAt as minimal audit timestamps. Use UTC.
**Reason:** Minimal overhead, useful for debugging, CreatedAt on movements serves as the authoritative movement timestamp.
**Consequences:** All entities gain CreatedAt (set on creation) and UpdatedAt (set on creation and update). No complex auditing framework.

### OD-013 — Category Name in Product Response
**Decision:** Include `categoryName` in Product response models.
**Reason:** Trivial with EF Core navigation properties. Better API ergonomics.
**Consequences:** Read queries use EF Core `.Include()` for category navigation. CategoryName is NOT duplicated in the Products table — it comes from the join.

### OD-016 — Movement Timestamp
**Decision:** Server-assigned UTC. Client must not provide or override the authoritative timestamp.
**Reason:** Simple, trustworthy, consistent. No clock skew issues.
**Consequences:** Movement CreatedAt is set server-side. No `date` field in the movement create request. The response returns the server-assigned timestamp.

---

## ADR-010: EF Core Migrations for Schema Management

**Context:** The database schema needs to be created and evolved. Two options: EF Core Migrations or raw SQL scripts.
**AI recommendation:** EF Core Migrations.
**Human decision:** Accepted.
**Status:** Accepted.
**Reason:** Schema stays in sync with the EF Core model. Single schema management mechanism. Standard .NET workflow. Applied on API startup. Does not conflict with the runtime persistence rule (EF Core for reads, Dapper for writes) — migrations are a schema management tool, not a runtime write mechanism.
**Consequences:** EF Core design-time tools generate migration files. Migrations are applied on API startup. No separate SQL script management needed.

---

## ADR-011: Implementation Detail Decisions (Batch)

**Context:** Six remaining implementation-level decisions needed resolution before implementation could begin.
**AI recommendation:** Resolve all six as recommended in architecture.md section 19.
**Human decision:** Accepted all six.
**Status:** Accepted.
**Decided in:** Prompt 2.2.

### OD-006 — Uniqueness Constraints
**Decision:** No uniqueness constraints beyond primary keys.
**Reason:** Assessment does not define uniqueness rules for product name, category name, or SKU. Adding them would introduce unsupported business behavior.
**Consequences:** No unique indexes on name fields. No duplicate-name validation.

### OD-007 — Pagination
**Decision:** No pagination for list endpoints. Return complete collection.
**Reason:** Not required by the assessment. Adds unnecessary complexity for this scope.
**Consequences:** GET /api/products and GET /api/categories return all records.

### OD-011 — Category Product Count
**Decision:** Do not include product count in Category responses.
**Reason:** Not required by the assessment. Avoids expanding the API contract unnecessarily.
**Consequences:** Category response contains only `id` and `name` (plus audit fields).

### OD-012 — Text Validation Limits
**Decision:** Maximum 200 characters for Product and Category name fields.
**Reason:** Reasonable default applied consistently across request validation, domain, and database schema.
**Consequences:** Request validation rejects names > 200 chars. Database columns configured as `nvarchar(200)`.

### OD-014 — Price Constraint
**Decision:** Product price must be >= 0. Negative prices are invalid.
**Reason:** Consistent with the spec error contract. Logical business constraint.
**Consequences:** Request validation and domain enforce price >= 0. Negative price returns 400 Bad Request.

### NSubstitute — Mocking Library
**Decision:** Use NSubstitute as the unit test mocking library.
**Reason:** Cleaner syntax, less boilerplate, aligns with Clean Code goals.
**Consequences:** Inventory.Tests project references NSubstitute. No other mocking framework used.

---

## ADR-012: Product → InventoryMovement FK and Delete Behavior

**Context:** The initial EF Core migration (Prompt 3.6) configured InventoryMovement → Product with CASCADE delete. This was discovered during human persistence review.
**AI-generated behavior:** Cascade delete — deleting a Product would automatically remove its InventoryMovements.
**Human review:** Rejected.
**Status:** Modified by Human.
**Human rationale:** Cascade deletion would destroy immutable inventory movement history, conflicting with the approved append-only principle (ADR-006). Movement records must be preserved.
**Final decision:**
- Product → InventoryMovement FK uses RESTRICT (no cascade delete).
- Product deletion is rejected with Conflict when InventoryMovements reference that Product.
- Hard delete remains allowed when the Product has no movement history.
**Consequences:**
- Migration regenerated with `DeleteBehavior.Restrict`.
- `IProductWriteRepository` gained `HasMovementsAsync` method.
- `DeleteProductCommandHandler` checks for movement history before deleting.
- New tests verify conflict behavior for products with movements.
- FR-005 updated in spec.md.

---
