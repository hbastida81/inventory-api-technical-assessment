# Inventory Management API — Specification

**Document type:** Functional Specification (Spec-Driven Development)
**Status:** APPROVED — All decisions resolved (Prompt 2.2)
**Version:** 2.0
**Date:** 2026-10-07
**Candidate:** Hector Bastida

**Related documents:**
- Architecture: [architecture.md](architecture.md)
- Decision records: [architecture-decisions.md](architecture-decisions.md) (ADR-001 through ADR-011)
- AI usage log: [ai-development-log.md](ai-development-log.md)

---

# 1. Purpose

This specification defines the observable behavior of a RESTful API that manages a product inventory system. The API supports creating, reading, updating, and deleting products and categories, as well as registering inventory movements (entries and exits).

This document serves as the contract between requirements and implementation. No code should be written until this spec is reviewed and approved.

The system is developed as a technical assessment for a Developer Senior position at Redarbor.

---

# 2. Scope

## In Scope

- Product CRUD operations.
- Category CRUD operations.
- Inventory movement registration (entry and exit).
- OAuth2 authentication protecting all endpoints.
- SQL Server as the relational database.
- EF Core for read operations.
- Dapper for write operations.
- Dockerized API, database, and Keycloak authorization server.
- Single `docker-compose.yml` to start the full system (API, SQL Server, Keycloak).
- Swagger API documentation.
- Unit tests for critical components.
- README with setup, run, and test instructions.

## Out of Scope

- User registration or user management (the assessment does not mention it).
- Role-based authorization beyond authentication (not requested).
- Reporting or analytics.
- Multi-tenancy.
- File uploads or image management for products.
- Notification systems.
- Frontend or UI.
- CI/CD pipelines.
- Production deployment beyond Docker.
- Inventory transfer between locations.
- Purchase orders or sales orders.
- Supplier or customer management.

## Resolved Decisions (formerly undefined)

The following were undefined by the assessment and have been resolved through human review:

- **Deletion behavior:** Hard delete (decided: OD-003).
- **Negative stock:** Rejected — exit movements that would produce negative stock return 409 Conflict (decided: OD-005).
- **OAuth2 flow:** Authorization Code + PKCE with Keycloak (decided: OD-001).
- **Identifier type:** Auto-increment integer (decided: OD-008).
- **Movement mutability:** Immutable / append-only (decided: OD-010).
- **Dapper write style:** Inline parameterized SQL, no Stored Procedures (decided: OD-015).
- **CQRS:** Lightweight manual command/query handlers, no MediatR (decided: CQRS).

## Still Undefined

The following remain open and are tracked in section 14:

- Whether products require unique identifiers beyond the primary key (e.g., SKU) — OD-006.
- Pagination, sorting, or filtering behavior for list endpoints — OD-007.
- Maximum length or format constraints for text fields — OD-012.
- Whether a category can be deleted if it has associated products — OD-004.
- Whether scopes or roles are required — OD-002.
- Whether audit fields (CreatedAt, UpdatedAt) should be included — OD-009.
- Whether the Category response should include a product count — OD-011.
- Whether the Product response should include category name — OD-013.
- Minimum/maximum price constraints — OD-014.
- Movement date source (server vs client) — OD-016.

---

# 3. Domain Concepts

## Product

A product is an item tracked in the inventory system. It belongs to one category.

**Required attributes:**
- Identity — auto-increment integer assigned by the system (decided: OD-008).
- Name — human-readable name of the product.
- Category — reference to the category this product belongs to.
- Price — monetary value of the product.
- Stock — current quantity available in inventory. Cannot go below zero (decided: OD-005).

**Justification for Stock:** The assessment requires inventory entry/exit movements. The system must reflect the current quantity somewhere. Stock on the product is the minimal way to represent the effect of movements.

## Category

A category groups related products.

**Required attributes:**
- Identity — auto-increment integer assigned by the system (decided: OD-008).
- Name — human-readable name of the category.

## Inventory Movement

An inventory movement records a change in quantity for a specific product. Movements are either entries (stock increases) or exits (stock decreases). Movements are immutable — once registered, they cannot be edited or deleted (decided: OD-010).

**Required attributes:**
- Identity — auto-increment integer assigned by the system (decided: OD-008).
- Product — reference to the affected product.
- Type — entry or exit.
- Quantity — number of units moved (positive integer).
- Date — when the movement occurred.

---

# 4. Functional Requirements

## Products

**FR-001: Create Product**
The system must allow creating a new product by providing its name, category, and price. The system assigns an identity and sets the initial stock to zero. The system responds with the created product including its assigned identity.

**FR-002: Retrieve Product**
The system must allow retrieving a single product by its identity. The response includes the product's name, category, price, and current stock. If the product does not exist, the system responds with a not-found error.

**FR-003: List Products**
The system must allow retrieving all products. The response is a collection of products with their name, category, price, and current stock.

**FR-004: Update Product**
The system must allow updating a product's name, category, and price by its identity. Stock is not directly updatable through this endpoint — stock changes only through inventory movements. If the product does not exist, the system responds with a not-found error.

**FR-005: Delete Product**
The system must allow deleting a product by its identity. Deletion is a hard delete — the product is permanently removed (decided: OD-003). If the product has inventory movement history, deletion must be rejected with a conflict response to preserve immutable movement records (decided: Prompt 3.6.1). If the product does not exist, the system responds with a not-found error.

## Categories

**FR-006: Create Category**
The system must allow creating a new category by providing its name. The system assigns an identity and responds with the created category.

**FR-007: Retrieve Category**
The system must allow retrieving a single category by its identity. If the category does not exist, the system responds with a not-found error.

**FR-008: List Categories**
The system must allow retrieving all categories.

**FR-009: Update Category**
The system must allow updating a category's name by its identity. If the category does not exist, the system responds with a not-found error.

**FR-010: Delete Category**
The system must allow deleting a category by its identity. If the category does not exist, the system responds with a not-found error. See OPEN DECISION OD-004 for behavior when the category has associated products.

## Inventory Movements

**FR-011: Register Inventory Entry**
The system must allow registering an inventory entry for a specific product. An entry increases the product's stock by the specified quantity. The system records the movement with its type, quantity, and date.

**FR-012: Register Inventory Exit**
The system must allow registering an inventory exit for a specific product. An exit decreases the product's stock by the specified quantity. If the requested quantity exceeds the product's current stock, the system rejects the operation with a 409 Conflict response (decided: OD-005). The system records the movement with its type, quantity, and date.

---

# 5. Business Rules

## Explicit Rules

**BR-001: Reads via EF Core**
All read operations must use Entity Framework Core.

**BR-002: Writes via Dapper**
All write operations must use Dapper.

**BR-003: OAuth2 Authentication**
All endpoints must be protected using OAuth2 authentication.

**BR-004: SQL Server**
The system must use SQL Server as its relational database.

**BR-005: Dockerized**
Both the API and the database must be dockerized and started with a single `docker-compose.yml`.

## Derived Minimum Rules

**BR-006: Positive Movement Quantity**
Inventory movement quantity must be a positive integer greater than zero.
*Why necessary:* A movement of zero or negative units has no physical meaning and would corrupt stock tracking.

**BR-007: Product Must Reference Valid Category**
When creating or updating a product, the referenced category must exist.
*Why necessary:* Without this rule, products could reference nonexistent categories, making the category relationship meaningless.

**BR-008: Movement Must Reference Valid Product**
When registering an inventory movement, the referenced product must exist.
*Why necessary:* A movement for a nonexistent product cannot affect any stock.

**BR-009: Stock Updated by Movements**
Product stock is only modified as a result of inventory movements, not through product update endpoints.
*Why necessary:* If stock could be set directly and also changed by movements, the two mechanisms would conflict, making inventory tracking unreliable.

**BR-010: Entry Increases Stock, Exit Decreases Stock**
An entry movement adds to the product's stock. An exit movement subtracts from the product's stock.
*Why necessary:* This is the fundamental definition of entry and exit as described by the assessment.

## Decided Rules (from human-approved decisions)

**BR-011: No Negative Stock**
An inventory exit must be rejected if the requested quantity exceeds the product's current stock. The system responds with 409 Conflict. (Decided: OD-005.)

**BR-012: Hard Delete**
Product and category deletion is permanent (hard delete). No soft delete flag or recovery mechanism. (Decided: OD-003.)

**BR-013: Movements Are Immutable**
Once registered, inventory movements cannot be edited or deleted. Corrections require a compensating movement. (Decided: OD-010.)

**BR-014: Write Operations Use Inline Parameterized SQL**
All write operations use Dapper with inline parameterized SQL. Stored Procedures are not used by default. (Decided: OD-015.)

**BR-015: CQRS Separation**
Read operations are handled by query handlers using EF Core. Write operations are handled by command handlers using Dapper. Handlers are plain classes without MediatR. (Decided: CQRS.)

---

# 6. REST API Contract

## Products

### POST /api/products
**Purpose:** Create a new product.
**Request body:** Product name, category identity, price.
**Success response:** 201 Created — the created product with assigned identity and initial stock of zero.
**Error responses:**
- 400 Bad Request — invalid or missing required fields.
- 401 Unauthorized — missing or invalid authentication.
- 422 Unprocessable Entity — referenced category does not exist.

### GET /api/products/{id}
**Purpose:** Retrieve a single product.
**Request body:** None.
**Success response:** 200 OK — the product with name, category, price, and current stock.
**Error responses:**
- 401 Unauthorized — missing or invalid authentication.
- 404 Not Found — product does not exist.

### GET /api/products
**Purpose:** List all products.
**Request body:** None.
**Success response:** 200 OK — collection of products.
**Error responses:**
- 401 Unauthorized — missing or invalid authentication.

### PUT /api/products/{id}
**Purpose:** Update an existing product.
**Request body:** Product name, category identity, price.
**Success response:** 200 OK — the updated product.
**Error responses:**
- 400 Bad Request — invalid or missing required fields.
- 401 Unauthorized — missing or invalid authentication.
- 404 Not Found — product does not exist.
- 422 Unprocessable Entity — referenced category does not exist.

### DELETE /api/products/{id}
**Purpose:** Delete a product.
**Request body:** None.
**Success response:** 204 No Content.
**Error responses:**
- 401 Unauthorized — missing or invalid authentication.
- 404 Not Found — product does not exist.

## Categories

### POST /api/categories
**Purpose:** Create a new category.
**Request body:** Category name.
**Success response:** 201 Created — the created category with assigned identity.
**Error responses:**
- 400 Bad Request — invalid or missing required fields.
- 401 Unauthorized — missing or invalid authentication.

### GET /api/categories/{id}
**Purpose:** Retrieve a single category.
**Request body:** None.
**Success response:** 200 OK — the category.
**Error responses:**
- 401 Unauthorized — missing or invalid authentication.
- 404 Not Found — category does not exist.

### GET /api/categories
**Purpose:** List all categories.
**Request body:** None.
**Success response:** 200 OK — collection of categories.
**Error responses:**
- 401 Unauthorized — missing or invalid authentication.

### PUT /api/categories/{id}
**Purpose:** Update an existing category.
**Request body:** Category name.
**Success response:** 200 OK — the updated category.
**Error responses:**
- 400 Bad Request — invalid or missing required fields.
- 401 Unauthorized — missing or invalid authentication.
- 404 Not Found — category does not exist.

### DELETE /api/categories/{id}
**Purpose:** Delete a category.
**Request body:** None.
**Success response:** 204 No Content.
**Error responses:**
- 401 Unauthorized — missing or invalid authentication.
- 404 Not Found — category does not exist.
- 409 Conflict — category has associated products (see OD-004).

## Inventory Movements

### POST /api/products/{productId}/movements
**Purpose:** Register an inventory movement (entry or exit) for a product.
**Request body:** Movement type (entry or exit), quantity.
**Success response:** 201 Created — the recorded movement with assigned identity and timestamp.
**Error responses:**
- 400 Bad Request — invalid or missing required fields, quantity not positive.
- 401 Unauthorized — missing or invalid authentication.
- 404 Not Found — product does not exist.
- 409 Conflict — insufficient stock for exit (decided: OD-005). Returned when exit quantity exceeds current product stock.

---

# 7. Request and Response Models

## Product

**Create Request:**
- `name` (string, required) — product name.
- `categoryId` (identifier, required) — reference to category.
- `price` (decimal, required) — product price.

**Update Request:**
- `name` (string, required) — product name.
- `categoryId` (identifier, required) — reference to category.
- `price` (decimal, required) — product price.

**Response:**
- `id` (identifier) — system-assigned identity.
- `name` (string) — product name.
- `categoryId` (identifier) — reference to category.
- `categoryName` (string) — category name for display convenience.
- `price` (decimal) — product price.
- `stock` (integer) — current quantity in inventory.

## Category

**Create Request:**
- `name` (string, required) — category name.

**Update Request:**
- `name` (string, required) — category name.

**Response:**
- `id` (identifier) — system-assigned identity.
- `name` (string) — category name.

## Inventory Movement

**Create Request:**
- `type` (string, required) — "Entry" or "Exit".
- `quantity` (integer, required) — number of units, must be positive.

**Response:**
- `id` (identifier) — system-assigned identity.
- `productId` (identifier) — reference to product.
- `type` (string) — "Entry" or "Exit".
- `quantity` (integer) — number of units.
- `date` (datetime) — when the movement was recorded.

---

# 8. Error Contract

All error responses must follow a consistent structure.

**Conceptual error response shape:**
- `status` (integer) — HTTP status code.
- `message` (string) — human-readable description of the error.
- `errors` (collection, optional) — field-level validation errors when applicable.

**Expected error behaviors:**

| Scenario | HTTP Status | Behavior |
|---|---|---|
| Missing or invalid required fields | 400 Bad Request | Return validation details |
| Resource not found | 404 Not Found | Return message identifying the resource |
| Authentication missing or invalid | 401 Unauthorized | Return authentication error |
| Business rule violation (e.g., invalid category reference) | 422 Unprocessable Entity | Return explanation of the violation |
| Conflict (e.g., category has products, insufficient stock) | 409 Conflict | Return explanation of the conflict |
| Unexpected server error | 500 Internal Server Error | Return generic message, never expose internals |

**Rules:**
- Never expose stack traces, exception messages, or internal details in error responses.
- Validation errors should identify which fields failed and why.

---

# 9. Authentication Requirements

## Explicit from Assessment

- All endpoints must be protected using OAuth2 authentication.
- The assessment states "Autenticación mediante OAuth2" without specifying the flow, provider, or token format.

## Decided Approach (OD-001 — Approved)

**OAuth2 Authorization Code with PKCE using Keycloak.**

- **Authorization Server:** Keycloak, running as a Docker container in docker-compose.
- **OAuth2 Flow:** Authorization Code with PKCE.
- **Token Format:** JWT (access tokens issued by Keycloak).
- **API Role:** Resource Server only. The Inventory API validates access tokens using standard ASP.NET Core JWT Bearer authentication. The API does not issue tokens.
- **Swagger Integration:** Swagger UI authenticates against Keycloak using Authorization Code + PKCE. The evaluator clicks "Authorize," logs in with the preconfigured test user, and receives a valid token.
- **Preconfiguration:** A Keycloak realm import file is committed to the repository containing: the realm, a public client configured for PKCE, and test user credentials.
- **No custom token issuer:** The API does not implement any token endpoint or credential handling.

## Observable Requirement

- A request to any endpoint without a valid access token issued by Keycloak must receive a 401 Unauthorized response.
- A request with a valid access token must be processed normally.

## Remaining Open Decision

- OD-002: Whether scopes or roles are required (see section 14).

---

# 10. Persistence Requirements

## Explicit from Assessment

- The database must be SQL Server.
- Entity Framework Core must be used for read operations.
- Dapper must be used for write operations.
- The database must be dockerized.
- The schema must include at least: Products, Categories, and Inventory Movements.

## Observable Constraints

- Read operations (GET endpoints) will use EF Core to query the database.
- Write operations (POST, PUT, DELETE endpoints) will use Dapper to persist changes.
- The database schema must support both access patterns.

## Decided Approach

- **Write operations use inline parameterized SQL via Dapper** (decided: OD-015). Stored Procedures are not used by default.
- **CQRS separation:** Query handlers use EF Core; command handlers use Dapper (decided: CQRS).

## Not Defined by Assessment

- Whether EF Core migrations or raw SQL scripts manage the schema.
- Connection string management beyond "environment variables for configuration."

---

# 11. Non-Functional Requirements

## From Assessment — Framework and Runtime

- NFR-001: .NET 8 or higher.
- NFR-002: ASP.NET Core Web API.

## From Assessment — Infrastructure

- NFR-003: API must be dockerized.
- NFR-004: SQL Server must be dockerized.
- NFR-005: A single `docker-compose.yml` must start API, database, and Keycloak.
- NFR-006: All configuration values must come from environment variables.

## From Assessment — Documentation

- NFR-007: Swagger must document all API endpoints.
- NFR-008: README.md must include setup, run, and test instructions.

## From Assessment — Code Quality

- NFR-009: All code and comments in English.
- NFR-010: No blank lines between code lines (one blank line before each `return`).
- NFR-011: Self-descriptive method and function names.
- NFR-012: Single responsibility per function. Methods must not exceed 25 lines.
- NFR-013: No loops or conditionals nested more than once.
- NFR-014: One class per file. One enum per file.
- NFR-015: Zero commented-out code.
- NFR-016: Pass objects as parameters, not loose properties.
- NFR-017: SOLID principles.
- NFR-018: Clean Code practices.
- NFR-019: Object-Oriented Programming.
- NFR-020: Design patterns must be applied.
- NFR-021: CQRS implemented via lightweight manual command/query handlers (decided: CQRS).

## From Assessment — Testing

- NFR-022: Unit tests for critical components.
- NFR-023: TDD approach.
- NFR-024: Adequate code coverage.

## From Assessment — Repository

- NFR-025: Source code in a public repository.
- NFR-026: Repository must include all files needed to build and run with Docker.

---

# 12. Acceptance Criteria

## Products

**AC-001: Successful Product Creation**
Given a valid authentication token and a valid request with name, existing category identity, and price,
when a POST request is sent to /api/products,
then the system responds with 201 Created and returns the product with an assigned identity and stock of zero.

**AC-002: Product Retrieval**
Given a valid authentication token and an existing product,
when a GET request is sent to /api/products/{id},
then the system responds with 200 OK and returns the product with name, category, price, and current stock.

**AC-003: Product List**
Given a valid authentication token,
when a GET request is sent to /api/products,
then the system responds with 200 OK and returns all products.

**AC-004: Product Update**
Given a valid authentication token, an existing product, and a valid request with name, existing category identity, and price,
when a PUT request is sent to /api/products/{id},
then the system responds with 200 OK and returns the updated product. Stock is not affected.

**AC-005: Product Deletion (Hard Delete)**
Given a valid authentication token and an existing product,
when a DELETE request is sent to /api/products/{id},
then the system responds with 204 No Content and the product is permanently removed and no longer retrievable.

**AC-006: Product Not Found**
Given a valid authentication token and a nonexistent product identity,
when a GET, PUT, or DELETE request is sent to /api/products/{id},
then the system responds with 404 Not Found.

**AC-007: Product Creation with Invalid Category**
Given a valid authentication token and a request with a nonexistent category identity,
when a POST request is sent to /api/products,
then the system responds with 422 Unprocessable Entity.

## Categories

**AC-008: Successful Category Creation**
Given a valid authentication token and a valid request with a name,
when a POST request is sent to /api/categories,
then the system responds with 201 Created and returns the category with an assigned identity.

**AC-009: Category Retrieval**
Given a valid authentication token and an existing category,
when a GET request is sent to /api/categories/{id},
then the system responds with 200 OK and returns the category.

**AC-010: Category List**
Given a valid authentication token,
when a GET request is sent to /api/categories,
then the system responds with 200 OK and returns all categories.

**AC-011: Category Update**
Given a valid authentication token, an existing category, and a valid request with a name,
when a PUT request is sent to /api/categories/{id},
then the system responds with 200 OK and returns the updated category.

**AC-012: Category Deletion**
Given a valid authentication token and an existing category with no associated products,
when a DELETE request is sent to /api/categories/{id},
then the system responds with 204 No Content.

**AC-013: Category Not Found**
Given a valid authentication token and a nonexistent category identity,
when a GET, PUT, or DELETE request is sent to /api/categories/{id},
then the system responds with 404 Not Found.

## Inventory Movements

**AC-014: Successful Inventory Entry**
Given a valid authentication token, an existing product with stock of N, and a valid entry movement with quantity Q,
when a POST request is sent to /api/products/{productId}/movements,
then the system responds with 201 Created, records the movement, and the product's stock becomes N + Q.

**AC-015: Successful Inventory Exit**
Given a valid authentication token, an existing product with stock of N (where N >= Q), and a valid exit movement with quantity Q,
when a POST request is sent to /api/products/{productId}/movements,
then the system responds with 201 Created, records the movement, and the product's stock becomes N - Q.

**AC-016: Movement for Nonexistent Product**
Given a valid authentication token and a nonexistent product identity,
when a POST request is sent to /api/products/{productId}/movements,
then the system responds with 404 Not Found.

**AC-017: Movement with Invalid Quantity**
Given a valid authentication token and a quantity of zero or negative,
when a POST request is sent to /api/products/{productId}/movements,
then the system responds with 400 Bad Request.

## Authentication

**AC-018: Unauthorized Request**
Given no authentication token or an invalid token,
when any request is sent to any endpoint,
then the system responds with 401 Unauthorized.

## Validation

**AC-019: Invalid Request Body**
Given a valid authentication token and a request with missing required fields,
when the request is sent to a creation or update endpoint,
then the system responds with 400 Bad Request with validation details.

**AC-020: Inventory Exit Rejected for Insufficient Stock**
Given a valid authentication token, an existing product with stock of N, and an exit movement with quantity Q where Q > N,
when a POST request is sent to /api/products/{productId}/movements,
then the system responds with 409 Conflict and the product's stock remains N.

---

# 13. Testable Behaviors

## Business Behavior
- Inventory entry increases product stock by the movement quantity.
- Inventory exit decreases product stock by the movement quantity.
- Inventory exit is rejected when quantity exceeds current stock (decided: OD-005).
- Product creation sets initial stock to zero.
- Product update does not affect stock.
- Product deletion permanently removes the product (decided: OD-003).
- Category deletion permanently removes the category (decided: OD-003).

## Validation Behavior
- Product creation rejects missing name.
- Product creation rejects name exceeding 200 characters (decided: OD-012).
- Product creation rejects missing category.
- Product creation rejects missing price.
- Product creation rejects negative price (decided: OD-014).
- Category creation rejects missing name.
- Category creation rejects name exceeding 200 characters (decided: OD-012).
- Movement creation rejects zero or negative quantity.
- Movement creation rejects missing type.
- Product creation rejects nonexistent category reference.
- Movement creation rejects nonexistent product reference.

## Command Behavior (Write Operations)
- Create product persists to database.
- Update product persists changes.
- Delete product removes from database.
- Create category persists to database.
- Update category persists changes.
- Delete category removes from database.
- Register movement persists and updates product stock.

## Query Behavior (Read Operations)
- Get product returns correct data including current stock.
- List products returns all products.
- Get category returns correct data.
- List categories returns all categories.

---

# 14. Decisions

## Resolved Decisions

| ID | Question | Decision | Decided In |
|---|---|---|---|
| CQRS | CQRS implementation level | Lightweight manual command/query handlers, no MediatR | Prompt 1.1.1 |
| OD-001 | OAuth2 flow/provider | Authorization Code + PKCE with Keycloak | Prompt 1.1.1 |
| OD-003 | Hard delete or soft delete | Hard delete | Prompt 1.1.1 |
| OD-005 | Can stock go below zero | No — reject exit with 409 Conflict | Prompt 1.1.1 |
| OD-008 | Identifier type | Auto-increment integer | Prompt 1.1.1 |
| OD-010 | Movement mutability | Immutable (append-only) | Prompt 1.1.1 |
| OD-015 | Dapper write style | Inline parameterized SQL, no SPs | Prompt 1.1.1 |

## All Decisions Resolved

All open decisions have been resolved through human review.

| ID | Question | Decision | Decided In |
|---|---|---|---|
| OD-002 | Scopes/roles | No roles — authenticated access sufficient | Prompt 2.0.1 |
| OD-004 | Category deletion with products | Reject with 409 Conflict | Prompt 2.0.1 |
| OD-006 | Field uniqueness | No uniqueness beyond PK | Prompt 2.2 |
| OD-007 | Pagination | No pagination — return all records | Prompt 2.2 |
| OD-009 | Audit fields | Include CreatedAt/UpdatedAt (UTC) | Prompt 2.0.1 |
| OD-011 | Category product count | Exclude from response | Prompt 2.2 |
| OD-012 | Text validation limits | Name max 200 characters | Prompt 2.2 |
| OD-013 | Category in product response | Include categoryId + categoryName | Prompt 2.0.1 |
| OD-014 | Price constraints | Price >= 0 | Prompt 2.2 |
| OD-016 | Movement timestamp | Server-assigned UTC | Prompt 2.0.1 |

---

# 15. Traceability Matrix

| Assessment Requirement | Spec Coverage |
|---|---|
| CRUD de productos | FR-001 to FR-005, AC-001 to AC-007 |
| CRUD de categorías | FR-006 to FR-010, AC-008 to AC-013 |
| Registro de movimientos de inventario (entrada/salida) | FR-011, FR-012, AC-014 to AC-017 |
| .NET 8 o superior | NFR-001 |
| Base de datos SQL Server | NFR-004, BR-004, Section 10 |
| Dockerizar la base de datos | NFR-004, NFR-005 |
| Dockerizar la API | NFR-003, NFR-005 |
| docker-compose.yml | NFR-005 |
| Entity Framework para lecturas | BR-001, Section 10 |
| Dapper para escrituras | BR-002, Section 10 |
| Autenticación OAuth2 | BR-003, Section 9 (Keycloak + Auth Code + PKCE), AC-018 |
| Pruebas unitarias | NFR-022 to NFR-024, Section 13 |
| SOLID | NFR-017 |
| Clean Code | NFR-009 to NFR-018 |
| TDD | NFR-023 |
| POO | NFR-019 |
| Patrones de diseño | NFR-020 |
| CQRS (deseable) | NFR-021, BR-015 (decided: lightweight manual handlers) |
| Swagger | NFR-007 |
| README.md | NFR-008 |
| Repositorio público | NFR-025, NFR-026 |
| Código/comentarios en inglés | NFR-009 |
| Sin código comentado | NFR-015 |
| Un archivo por clase/enum | NFR-014 |
| Variables de configuración por env vars | NFR-006 |
| Métodos autodescriptivos | NFR-011 |
| Responsabilidad única, métodos < 25 líneas | NFR-012 |
| Sin anidación profunda | NFR-013 |
| Pasar objetos, no propiedades sueltas | NFR-016 |
| Evidencia de uso de IA | AI Development Log (docs/ai-development-log.md) |
| Spec-Driven Development | This document |
| Preguntas de toma de decisiones | Separate deliverable (not part of this spec) |

---

# End of Specification

**Status: APPROVED — All decisions resolved (Prompt 2.2)**

All open decisions have been resolved. The spec is ready for implementation.
