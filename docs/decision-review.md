# Open Decision Review — Inventory Management API

**Document type:** Decision Analysis (Pre-Architecture)
**Status:** PENDING HUMAN APPROVAL
**Date:** 2026-10-07
**Agents used:** spec (ambiguity analysis), architect (architectural trade-offs)

---

# Decision Classification

## Must be decided BEFORE architecture

| ID | Decision |
|---|---|
| OD-001 | OAuth2 flow/provider |
| OD-003 | Hard delete vs soft delete |
| OD-005 | Negative stock allowed |
| OD-008 | Identifier type |
| OD-010 | Movement immutability |
| OD-015 | Stored Procedures vs inline SQL |
| CQRS | CQRS implementation level |

## Can be decided DURING architecture

| ID | Decision |
|---|---|
| OD-002 | Scopes/roles |
| OD-004 | Category deletion with products |
| OD-009 | Audit fields |
| OD-013 | Category details in product response |
| OD-016 | Movement date source |

## Can wait until implementation

| ID | Decision |
|---|---|
| OD-006 | Field uniqueness |
| OD-007 | Pagination |
| OD-011 | Product count in category response |
| OD-012 | Text field validation limits |
| OD-014 | Price constraints |

---

# Detailed Analysis

---

## CQRS — Implementation Level

**Not in original OD list but required by prompt. The assessment says "de ser posible, el patrón CQRS."**

**Why it matters:** Determines the internal structure of the application layer, how commands and queries are organized, and whether a third-party library is introduced.

### Option A: No CQRS — traditional service layer

A single service class per domain entity (e.g., `ProductService`) handles both reads and writes.

- **Advantages:** Simplest structure. No extra abstractions. Fewer files. Easy to follow.
- **Disadvantages:** Does not satisfy the assessment's explicit mention of CQRS. Misses an opportunity to demonstrate design pattern knowledge. Does not naturally separate EF Core reads from Dapper writes.
- **Risks:** Evaluator may interpret "de ser posible" as "we expect you to do it."
- **Complexity:** Low.

### Option B: Lightweight CQRS — manual command/query separation

Separate command handlers and query handlers as plain classes. No library. Commands use Dapper, queries use EF Core. A simple interface like `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>`.

- **Advantages:** Demonstrates CQRS knowledge. Naturally maps to the EF-reads/Dapper-writes requirement. Clean separation of concerns. No external dependency. Easy to test. Easy to explain in interview. Each handler is a small, focused class (aligns with SRP and <25 lines rule).
- **Disadvantages:** More files than Option A. Slightly more boilerplate.
- **Risks:** Minimal — the pattern is well understood and straightforward.
- **Complexity:** Medium-low.

### Option C: CQRS with MediatR

Use the MediatR library for command/query dispatch via `IRequest`/`IRequestHandler`.

- **Advantages:** Industry-standard implementation. Demonstrates familiarity with a popular library. Pipeline behaviors for cross-cutting concerns (logging, validation). Well-documented.
- **Disadvantages:** Adds a NuGet dependency for a small project. Introduces indirection that can feel over-engineered for 12 endpoints. Pipeline behaviors are not requested by the assessment. Evaluator might question whether the candidate can build patterns without a library.
- **Risks:** Could be perceived as over-engineering. Adds complexity that must be justified. The assessment evaluates "criterio con el que diriges la IA" — blindly adding MediatR without justification could score negatively.
- **Complexity:** Medium.

**AI recommendation:** Option B — Lightweight CQRS.
**Reason:** It directly satisfies the CQRS mention in the assessment. It naturally enforces the EF Core/Dapper split. It demonstrates the candidate understands the pattern without hiding behind a library. Each handler is a small class with single responsibility, which aligns perfectly with the Clean Code rules. It is easy to explain and defend: "I chose lightweight CQRS because the assessment asks for it, and the EF/Dapper split maps naturally to query/command separation. MediatR would add indirection without proportional benefit for 12 endpoints."

**Recommended phase:** Must be decided BEFORE architecture.
**Human approval required:** YES.

---

## OD-001 — OAuth2 Flow/Provider

**Why it matters:** This is the most architecturally significant decision. It determines whether the project includes an authorization server, which NuGet packages are needed, how tokens are issued, and how Docker is configured.

### Option A: External provider (Auth0, Keycloak, etc.)

Use a third-party authorization server. The API validates tokens issued by the external provider.

- **Advantages:** Genuinely OAuth2-compliant. Separates concerns (API does not issue tokens). Industry-standard in production.
- **Disadvantages:** Requires either a cloud account (Auth0) or another Docker container (Keycloak). Adds infrastructure complexity. Evaluator must configure the provider to test. Keycloak container is heavy (~500MB). Auth0 requires internet and an account.
- **Risks:** Evaluator may not want to set up an external account. Docker-compose becomes more complex. If the external service is unavailable, the API cannot authenticate.
- **Complexity:** High.

### Option B: Self-hosted authorization server (OpenIddict / Duende IdentityServer)

Host an authorization server within the API or as a separate project. Issues tokens following OAuth2 spec.

- **Advantages:** Truly OAuth2-compliant. Self-contained in Docker. No external dependency. Demonstrates deep understanding.
- **Disadvantages:** Significant additional code. OpenIddict requires EF Core setup for token storage. Duende requires a license for production (free for dev/testing). Adds complexity beyond what the assessment is testing (inventory management, not auth infrastructure).
- **Risks:** Time investment may take away from core requirements. Over-engineering for a technical test.
- **Complexity:** High.

### Option C: Resource Owner Password Credentials (ROPC) grant — built-in token endpoint

The API itself exposes a `POST /api/auth/token` endpoint that accepts credentials and returns a JWT. The endpoint follows the OAuth2 ROPC grant format (grant_type, username, password → access_token, token_type, expires_in).

- **Advantages:** Technically an OAuth2 flow (RFC 6749 Section 4.3). Self-contained — no external infrastructure. Simple to implement and test. Evaluator can get a token with a single curl/Swagger call. Demonstrates understanding of OAuth2 token structure. Docker-compose stays simple.
- **Disadvantages:** ROPC is deprecated in OAuth 2.1. Not suitable for production with real users. The API acts as both resource server and authorization server.
- **Risks:** A strict evaluator might argue this is "just JWT" not "real OAuth2." However, ROPC is defined in RFC 6749 and is a legitimate OAuth2 grant type.
- **Complexity:** Low-medium.

### Option D: Client Credentials grant — built-in token endpoint

Similar to Option C but uses client_id/client_secret instead of username/password. Better suited for machine-to-machine / API-to-API scenarios.

- **Advantages:** Valid OAuth2 flow (RFC 6749 Section 4.4). Not deprecated. Self-contained. Simple to test. No user management needed. The token endpoint follows the OAuth2 spec: `grant_type=client_credentials`, returns `access_token`, `token_type`, `expires_in`. Clean fit for an API without a user-facing frontend.
- **Disadvantages:** Does not demonstrate user authentication (no username/password). Less familiar to some evaluators. Still has the API acting as its own authorization server.
- **Risks:** Evaluator might expect user-based auth. However, the assessment does not mention users, user registration, or login — it says "autenticación mediante OAuth2."
- **Complexity:** Low-medium.

**AI recommendation:** Option C — ROPC grant with built-in token endpoint.
**Reason:** It is a recognized OAuth2 flow defined in RFC 6749. It is self-contained (no extra Docker containers or external accounts). The evaluator can test it immediately via Swagger. It demonstrates understanding of OAuth2 token format and JWT. It keeps docker-compose simple. The deprecation in OAuth 2.1 is a valid criticism, but for a technical test that says "OAuth2" without further specifics, ROPC is a pragmatic and defensible choice. It can be documented in the README as: "ROPC was chosen for simplicity in the test context; in production, Authorization Code + PKCE or Client Credentials would be preferred."

**Alternative worth considering:** Option D (Client Credentials) is technically cleaner and not deprecated, but may feel unfamiliar to evaluators expecting username/password login. If human reviewer prefers no-user-concept, Option D is better.

**Recommended phase:** Must be decided BEFORE architecture.
**Human approval required:** YES.

---

## OD-002 — Scopes/Roles

**Why it matters:** Determines whether the API has a single access level or multiple.

### Option A: No roles — all authenticated users access everything

- **Advantages:** Simple. Assessment does not mention roles. Fewer moving parts.
- **Disadvantages:** Less realistic. Does not demonstrate authorization knowledge.
- **Risks:** Minimal.
- **Complexity:** Low.

### Option B: Basic roles (admin can write, reader can only read)

- **Advantages:** Demonstrates authorization. More realistic.
- **Disadvantages:** Adds complexity. Not requested by assessment. Requires role assignment mechanism.
- **Risks:** Scope creep. Evaluator did not ask for it.
- **Complexity:** Medium.

**AI recommendation:** Option A — no roles.
**Reason:** The assessment explicitly says "autenticación," not "autorización." Adding roles introduces unrequested complexity. The focus should be on demonstrating OAuth2 authentication, not building a permission system.

**Recommended phase:** Can be decided during architecture.
**Human approval required:** YES (but low-stakes).

---

## OD-003 — Hard Delete vs Soft Delete

**Why it matters:** Affects schema design, query filters, and whether movement history references remain valid after deletion.

### Option A: Hard delete

- **Advantages:** Simple. Assessment says "eliminar," which naturally means delete. No extra columns. No query filters. Fewer edge cases.
- **Disadvantages:** If a product is deleted, its inventory movements lose their product reference (FK violation or orphaned records).
- **Risks:** Orphaned movement records. Can be mitigated by cascade delete or by deleting movements when product is deleted.
- **Complexity:** Low.

### Option B: Soft delete (IsDeleted flag)

- **Advantages:** Preserves referential integrity. Movement history stays intact. Reversible.
- **Disadvantages:** Every query must filter `WHERE IsDeleted = false`. More complex. Not requested by assessment. Adds a column not justified by requirements.
- **Risks:** Adds complexity without assessment justification. Could be flagged as over-engineering.
- **Complexity:** Medium.

**AI recommendation:** Option A — hard delete.
**Reason:** The assessment says "eliminar" (delete). Soft delete is not mentioned and adds complexity that must be justified. For movement history integrity: movements for a deleted product can either be cascade-deleted (simple) or the FK can be set to SET NULL (preserves history without soft delete). Hard delete is simpler, easier to test, and aligns with what was asked.

**Recommended phase:** Must be decided BEFORE architecture.
**Human approval required:** YES.

---

## OD-004 — Category Deletion with Associated Products

**Why it matters:** Defines behavior at a referential boundary.

### Option A: Reject with 409 Conflict

- **Advantages:** Safe. Prevents data loss. Clear error message. Forces user to reassign products first.
- **Disadvantages:** Requires the client to handle the conflict.
- **Risks:** Minimal.
- **Complexity:** Low.

### Option B: Allow deletion, nullify category on products

- **Advantages:** Flexible. No blocking.
- **Disadvantages:** Products lose their category. Requires nullable FK. Inconsistent with BR-007 (product must reference valid category).
- **Risks:** Data inconsistency.
- **Complexity:** Medium.

### Option C: Cascade delete products

- **Advantages:** Clean referential integrity.
- **Disadvantages:** Dangerous — deleting a category silently deletes all its products and their movements. No assessment requirement for this behavior.
- **Risks:** Data loss. Difficult to defend in an interview.
- **Complexity:** Low technically, high in risk.

**AI recommendation:** Option A — reject with 409 Conflict.
**Reason:** Safest option. Prevents accidental data loss. Easy to implement, test, and explain. Consistent with the principle that destructive operations should be explicit.

**Recommended phase:** Can be decided during architecture.
**Human approval required:** YES.

---

## OD-005 — Negative Stock

**Why it matters:** Defines whether exit movements are validated against current stock.

### Option A: Reject exit if quantity exceeds stock (409 Conflict)

- **Advantages:** Prevents impossible physical states. Demonstrates business rule enforcement. More testable (clear success/failure boundary). Realistic inventory behavior.
- **Disadvantages:** Slightly more complex write logic.
- **Risks:** Minimal.
- **Complexity:** Low.

### Option B: Allow negative stock

- **Advantages:** Simpler write logic. No stock check needed.
- **Disadvantages:** Allows impossible physical states. Less realistic. Misses opportunity to demonstrate business rule validation.
- **Risks:** Evaluator might question why negative inventory is allowed.
- **Complexity:** Low.

**AI recommendation:** Option A — reject exit if quantity exceeds stock.
**Reason:** An inventory system that allows negative stock is hard to defend. This is a natural business rule that demonstrates validation capability. It adds one condition check, not significant complexity. It produces a testable scenario (AC-015 boundary).

**Recommended phase:** Must be decided BEFORE architecture.
**Human approval required:** YES.

---

## OD-008 — Identifier Type

**Why it matters:** Affects URL design, database indexing, EF Core configuration, and client usage patterns.

### Option A: Auto-increment integer (int/long)

- **Advantages:** Simple. Sequential. Small in URLs (`/api/products/42`). Efficient indexing. Default EF Core behavior. Familiar to evaluators.
- **Disadvantages:** Predictable (security through obscurity is not real security, but some evaluators care). Tight coupling to database.
- **Risks:** Minimal for a technical test.
- **Complexity:** Low.

### Option B: GUID/UUID

- **Advantages:** Globally unique. No database dependency for generation. Can be generated client-side. Non-sequential (less predictable).
- **Disadvantages:** Longer URLs. Worse clustered index performance in SQL Server (fragmentation). Requires `NEWSEQUENTIALID()` or application-side generation to avoid fragmentation. More verbose in Swagger testing.
- **Risks:** Index fragmentation if not using sequential GUIDs. Slightly harder to test manually.
- **Complexity:** Low-medium.

**AI recommendation:** Option A — auto-increment integer.
**Reason:** Simpler for a technical test. Better SQL Server clustered index performance. Cleaner URLs for Swagger testing. EF Core handles it by default. The assessment does not require GUIDs. Integer IDs are sufficient and easier for the evaluator to work with.

**Recommended phase:** Must be decided BEFORE architecture.
**Human approval required:** YES.

---

## OD-009 — Audit Fields (CreatedAt, UpdatedAt)

**Why it matters:** Common practice but not requested.

### Option A: Include CreatedAt and UpdatedAt

- **Advantages:** Minimal overhead. Useful for debugging. Shows awareness of good practices. CreatedAt on movements serves as the movement date (OD-016 synergy).
- **Disadvantages:** Not requested. Two extra columns per table.
- **Risks:** Evaluator might flag it as unrequested scope. Very unlikely.
- **Complexity:** Low.

### Option B: Exclude

- **Advantages:** Strictly follows requirements. No extra fields.
- **Disadvantages:** Loses useful metadata. Movement "date" must be handled separately.
- **Risks:** None.
- **Complexity:** Low.

**AI recommendation:** Option A — include audit fields.
**Reason:** The overhead is minimal (two columns, auto-set). CreatedAt on InventoryMovement naturally serves as the movement date, which resolves OD-016. It demonstrates awareness of production practices without adding meaningful complexity. If the evaluator questions it, the answer is simple: "Audit timestamps are standard practice and cost almost nothing."

**Recommended phase:** Can be decided during architecture.
**Human approval required:** YES (but low-stakes).

---

## OD-010 — Movement Immutability

**Why it matters:** Determines whether movements can be edited or deleted after creation.

### Option A: Immutable (append-only)

- **Advantages:** Standard in inventory/accounting systems. Simpler — only POST, no PUT/DELETE for movements. Audit trail is intact. Easier to reason about stock correctness. Fewer endpoints to build.
- **Disadvantages:** Correcting a mistake requires a compensating movement.
- **Risks:** Minimal.
- **Complexity:** Low (fewer operations to implement).

### Option B: Mutable (editable/deletable)

- **Advantages:** More flexible. Allows corrections.
- **Disadvantages:** Complex stock recalculation on edit/delete. Audit trail is broken. Assessment only asks to "register" movements, not edit them. More endpoints to build and test.
- **Risks:** Stock inconsistency on edit/delete. Over-engineering.
- **Complexity:** High.

**AI recommendation:** Option A — immutable movements.
**Reason:** The assessment says "registrar movimientos de inventario (entrada/salida)." It asks for registration, not management. Immutable movements are standard in inventory systems. It reduces scope (no PUT/DELETE for movements), reduces risk (no stock recalculation), and is easy to defend: "Inventory movements are append-only, like accounting ledger entries."

**Recommended phase:** Must be decided BEFORE architecture.
**Human approval required:** YES.

---

## OD-013 — Category Details in Product Response

**Why it matters:** Affects whether GET product queries need a join.

### Option A: Include categoryId and categoryName

- **Advantages:** Better client experience. Avoids a second API call to get the category name. Natural with EF Core navigation properties.
- **Disadvantages:** Requires a join or navigation property. Slightly more complex query.
- **Risks:** Minimal — EF Core handles this naturally with `.Include()`.
- **Complexity:** Low.

### Option B: Include only categoryId

- **Advantages:** Simpler query. No join needed.
- **Disadvantages:** Client must make a second call to get category name. Poor API ergonomics.
- **Risks:** Evaluator might question why the API is not user-friendly.
- **Complexity:** Low.

**AI recommendation:** Option A — include categoryId and categoryName.
**Reason:** EF Core navigation properties make this trivial. It demonstrates proper use of EF Core for reads. Better API design. The overhead is negligible.

**Recommended phase:** Can be decided during architecture.
**Human approval required:** YES (but low-stakes).

---

## OD-015 — Stored Procedures vs Inline Parameterized SQL

**Why it matters:** Determines how Dapper write operations are structured.

### Option A: Inline parameterized SQL via Dapper

```
// Conceptual example:
await connection.ExecuteAsync(
    "INSERT INTO Products (Name, CategoryId, Price, Stock) VALUES (@Name, @CategoryId, @Price, @Stock)",
    product);
```

- **Advantages:** SQL lives alongside the C# code that uses it. Easier to read and maintain in a small project. No database objects to manage separately. EF Core migrations can handle schema; Dapper handles writes with plain SQL.
- **Disadvantages:** SQL is in C# strings (though parameterized). Less separation between application and database.
- **Risks:** SQL strings can become hard to manage at scale (not relevant for this project size).
- **Complexity:** Low.

### Option B: Stored Procedures via Dapper

- **Advantages:** SQL lives in the database. Clear separation. Easier to optimize independently. Demonstrates SP knowledge.
- **Disadvantages:** Requires managing SP creation scripts alongside EF Core migrations. Two schema management mechanisms. More files. SPs must be deployed to the Docker SQL Server container on startup.
- **Risks:** Dual schema management (EF migrations + SP scripts) adds friction. Assessment does not request SPs.
- **Complexity:** Medium.

### Option C: Mix — simple writes inline, complex writes via SP

- **Advantages:** Best of both worlds for the right cases.
- **Disadvantages:** Inconsistent approach. In this project, no write is complex enough to justify an SP.
- **Risks:** Inconsistency.
- **Complexity:** Medium.

**AI recommendation:** Option A — inline parameterized SQL.
**Reason:** The assessment says "Dapper para escrituras," not "Stored Procedures." Inline parameterized SQL is the most natural Dapper usage. It keeps the project simpler (no SP scripts to manage alongside EF migrations). It is secure (parameterized, no injection risk). For a project with ~6 write operations, inline SQL is cleaner and easier to review. If the evaluator wanted SPs, they would have said so — the previous project used SPs, but this assessment does not.

**Recommended phase:** Must be decided BEFORE architecture.
**Human approval required:** YES.

---

## OD-016 — Movement Date Source

**Why it matters:** Determines whether the server or client controls timestamps.

### Option A: Server-assigned (UTC now)

- **Advantages:** Consistent. No clock skew issues. Simpler request body. Trustworthy timestamps.
- **Disadvantages:** Cannot record backdated movements.
- **Risks:** Minimal.
- **Complexity:** Low.

### Option B: Client-provided

- **Advantages:** Flexible. Allows backdating.
- **Disadvantages:** Client can send incorrect timestamps. Requires validation. More complex request.
- **Risks:** Untrusted timestamps.
- **Complexity:** Medium.

**AI recommendation:** Option A — server-assigned UTC.
**Reason:** Simpler. Trustworthy. The assessment does not mention backdating. If audit fields are included (OD-009), CreatedAt naturally serves as the movement date. One fewer field in the request body.

**Recommended phase:** Can be decided during architecture.
**Human approval required:** YES (but low-stakes).

---

## Remaining Decisions (Can Wait Until Implementation)

### OD-006 — Field Uniqueness
**AI recommendation:** No uniqueness constraint beyond PK. Assessment does not mention SKUs or unique names. Add only if evaluator feedback requests it.

### OD-007 — Pagination
**AI recommendation:** No pagination. The assessment does not mention it. Return all records. For a technical test with small data sets, pagination adds unrequested complexity. Can be added later if needed.

### OD-011 — Product Count in Category Response
**AI recommendation:** Exclude. Keep response minimal. Not requested. Can be added trivially later.

### OD-012 — Text Field Validation Limits
**AI recommendation:** Reasonable defaults (name max 200 chars). Implemented as validation attributes during implementation. No architectural impact.

### OD-014 — Price Constraints
**AI recommendation:** Price >= 0 (allow zero for free items). Simple validation. No architectural impact.

---

# Proposed Decision Set for Human Approval

## Approved Decisions (Prompt 1.1.1 — Human Review)

| ID | Decision | Approved Choice | Status |
|---|---|---|---|
| CQRS | Implementation level | Lightweight manual handlers, no MediatR | **APPROVED** |
| OD-003 | Deletion type | Hard delete | **APPROVED** |
| OD-005 | Negative stock | Reject exit exceeding stock (409 Conflict) | **APPROVED** |
| OD-008 | Identifier type | Auto-increment integer | **APPROVED** |
| OD-010 | Movement immutability | Immutable (append-only) | **APPROVED** |
| OD-015 | Dapper write style | Inline parameterized SQL, no SPs | **APPROVED** |

## Rejected and Replaced Recommendation

| ID | Decision | Rejected Choice | Approved Replacement | Status |
|---|---|---|---|---|
| OD-001 | OAuth2 flow | ROPC grant | Authorization Code + PKCE with Keycloak | **APPROVED (revised)** |

**Human reasoning for rejection:**
- ROPC requires the client to directly handle user credentials.
- Not appropriate for a new OAuth2 design.
- Provides a weaker, less defensible security design for a Senior-level assessment.
- The assessment explicitly requires OAuth2; the solution should use a modern flow.

**OD-001 remains OPEN. Revised analysis below.**

## Pending Decisions (AI recommendations, not yet approved)

| ID | Decision | AI Recommendation | Key Reason |
|---|---|---|---|
| OD-002 | Scopes/roles | No roles — flat access | Assessment says "autenticación," not "autorización" |
| OD-004 | Category with products | Reject deletion (409 Conflict) | Safe, prevents accidental data loss |
| OD-006 | Field uniqueness | No uniqueness beyond PK | Not requested |
| OD-007 | Pagination | No pagination | Not requested, small data set |
| OD-009 | Audit fields | Include CreatedAt/UpdatedAt | Minimal overhead, resolves OD-016 |
| OD-011 | Category product count | Exclude | Not requested |
| OD-012 | Text field limits | Max 200 chars | Reasonable default |
| OD-013 | Category in product response | Include categoryId + categoryName | Trivial with EF Core, better API ergonomics |
| OD-014 | Price constraints | Price >= 0 | Allow free items, simple validation |
| OD-016 | Movement date | Server-assigned UTC (via CreatedAt) | Simple, trustworthy, synergy with OD-009 |

---

# OD-001 REVISED — OAuth2 Flow (Modern Approach)

**Previous recommendation (ROPC) was rejected.** This section provides a revised analysis focused on modern OAuth2 flows.

## Context

The assessment requires "Autenticación mediante OAuth2." The solution must:
- Genuinely comply with OAuth2 (not just JWT issuance).
- Be feasible within the scope of a technical test.
- Be easy to run locally with `docker-compose up`.
- Work well with Swagger for testing protected endpoints.
- Avoid unnecessary infrastructure.
- Be defensible in a technical interview as a Senior Developer.

## Revised Options

### Option E: Keycloak in Docker with Authorization Code + PKCE

Use Keycloak as a full OAuth2/OIDC authorization server running in the same docker-compose. The API validates JWT tokens issued by Keycloak. Swagger UI is configured with Authorization Code + PKCE flow to obtain tokens interactively.

- **How it works:**
  1. docker-compose starts three containers: API, SQL Server, Keycloak.
  2. Keycloak is pre-configured via realm import (JSON file) with a client and a test user.
  3. Swagger UI uses the Authorization Code + PKCE flow: the evaluator clicks "Authorize," logs in via Keycloak, and gets a token.
  4. The API validates the JWT using Keycloak's OIDC discovery endpoint.

- **Advantages:**
  - Genuinely OAuth2/OIDC compliant. Authorization Code + PKCE is the recommended modern flow.
  - Keycloak is open source, widely used, and well-documented.
  - Realm export/import allows pre-configuration: no manual setup for the evaluator.
  - The API does NOT issue tokens — clean separation between resource server and authorization server.
  - Swagger + PKCE is a standard integration pattern.
  - Defensible in interview: "I used a real authorization server with the recommended OAuth2 flow."
  - Demonstrates senior-level understanding of OAuth2 architecture.

- **Disadvantages:**
  - Keycloak Docker image is ~400-500MB. First pull takes time.
  - Adds a third container to docker-compose.
  - Realm configuration requires a JSON export file (one-time setup, committed to repo).
  - Slightly more complex docker-compose (Keycloak needs a database or uses embedded H2).

- **Risks:**
  - Keycloak startup is slow (~15-30 seconds). API must wait or handle Keycloak unavailability gracefully.
  - If Keycloak configuration is wrong, authentication breaks entirely.
  - Evaluator's machine needs enough resources for 3 containers.

- **Complexity:** Medium.

### Option F: Lightweight self-hosted OAuth2 with OpenIddict

Use OpenIddict (open source) embedded in the API project or as a minimal companion project. Implements Authorization Code + PKCE with a minimal token endpoint.

- **Advantages:**
  - Truly OAuth2-compliant. OpenIddict implements the OAuth2/OIDC specs.
  - No external container needed — runs inside the API.
  - Lighter than Keycloak.
  - Full .NET ecosystem (EF Core integration for token storage).

- **Disadvantages:**
  - Significant additional code: token endpoint, authorization endpoint, consent flow, client registration.
  - OpenIddict setup is non-trivial: requires configuring signing keys, client definitions, scopes, and EF Core stores.
  - The assessment is testing inventory management, not auth server implementation.
  - Time spent on OpenIddict configuration is time not spent on core requirements.
  - Harder to explain concisely: "I built a custom authorization server" raises the question "why not use an existing one?"

- **Risks:**
  - Over-engineering. The evaluator is evaluating inventory API design, not auth server construction.
  - If OpenIddict is misconfigured, debugging takes time.

- **Complexity:** High.

### Option G: Keycloak with Client Credentials flow

Same as Option E but using Client Credentials instead of Authorization Code + PKCE. No user login required — the evaluator gets a token by sending client_id/client_secret to Keycloak's token endpoint.

- **Advantages:**
  - Valid modern OAuth2 flow (not deprecated).
  - Simpler than Authorization Code — no interactive login, no PKCE.
  - Works well for machine-to-machine API scenarios.
  - Can be tested easily via curl, Postman, or Swagger.
  - Keycloak is still pre-configured via realm import.

- **Disadvantages:**
  - Does not demonstrate user-based authentication.
  - Still requires the Keycloak container (same size/startup concern as Option E).

- **Risks:**
  - Evaluator might expect user login. However, the assessment does not mention user registration, login, or user management.

- **Complexity:** Medium-low (simpler auth flow, same infrastructure).

### Option H: Keycloak with both flows available

Configure Keycloak with two clients: one for Authorization Code + PKCE (Swagger UI interactive login) and one for Client Credentials (API/curl testing). The evaluator can use whichever is more convenient.

- **Advantages:**
  - Maximum flexibility for the evaluator.
  - Demonstrates understanding of multiple OAuth2 flows.
  - Swagger UI uses PKCE for interactive testing.
  - curl/Postman uses Client Credentials for quick testing.

- **Disadvantages:**
  - Same Keycloak infrastructure concerns.
  - Slightly more Keycloak configuration (two clients instead of one).

- **Risks:**
  - Marginal over-engineering on the auth side. Two clients is standard practice but adds documentation burden.

- **Complexity:** Medium.

## AI Recommendation (Revised)

**Option E — Keycloak with Authorization Code + PKCE.**

**Reason:**

1. **Genuinely OAuth2-compliant.** Authorization Code + PKCE is the current best practice for OAuth2. This directly addresses the rejection reason: "the solution should use a modern OAuth2 flow."

2. **Clean architecture.** The API is purely a resource server. It validates tokens but does not issue them. This is the correct OAuth2 separation of concerns and demonstrates senior-level understanding.

3. **Zero auth code in the API.** The API only needs `AddAuthentication().AddJwtBearer()` with Keycloak's OIDC discovery URL. No token endpoint, no credential handling, no signing key management.

4. **Pre-configured and self-contained.** A Keycloak realm export JSON committed to the repo contains: the realm, a public client (for Swagger PKCE), and a test user. The evaluator runs `docker-compose up` and everything works.

5. **Swagger integration.** Swagger UI natively supports Authorization Code + PKCE. The evaluator clicks "Authorize," logs in with the test user, and tests all endpoints with a valid token.

6. **Defensible in interview.** "I chose Keycloak as the authorization server because the assessment requires OAuth2. Authorization Code + PKCE is the recommended flow. Keycloak is pre-configured via realm import so `docker-compose up` starts everything. The API is a pure resource server — it validates tokens but doesn't issue them."

7. **Trade-offs are manageable.** The Keycloak image is large but pulled once. Startup is slow but the API can retry connection. These are operational concerns, not architectural flaws.

**If the Keycloak image size or startup time is a concern,** Option G (Client Credentials with Keycloak) is a simpler flow with the same infrastructure. But Authorization Code + PKCE is stronger for a Senior assessment.

**Status: APPROVED — Human approved Keycloak with Authorization Code + PKCE (Prompt 1.1.1, 2026-10-07).**
