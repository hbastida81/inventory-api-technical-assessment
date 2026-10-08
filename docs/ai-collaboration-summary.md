# AI-Assisted Development Summary

This document summarizes how AI tools were used during the development of the Inventory Management API and how the human developer supervised the process.

## Collaboration Model

### Human Developer

Responsible for:
- Interpreting assessment requirements.
- Approving or rejecting architecture proposals.
- Making business and security decisions.
- Reviewing all AI output before it became part of the solution.
- Identifying implementation issues (cascade delete, hostname configuration, audience validation, etc.).
- Interactive OAuth2 verification through the real browser-based PKCE flow.
- Final responsibility for the delivered solution.

### ChatGPT

Used for:
- Requirement analysis and prompt design.
- Architecture discussion and alternative evaluation.
- Reviewing Claude Code results and identifying risks or inconsistencies.
- Preparing structured prompts with clear scope and acceptance criteria.
- Supervising the development sequence.

### Claude Code (Claude CLI)

Used for:
- Repository inspection and file operations.
- Code implementation following approved specifications.
- TDD execution (writing tests first, verifying failures, implementing, verifying green).
- Build and test execution.
- Docker operations and runtime verification.
- Documentation updates through numbered prompts.

### Runtime Independence

The application has no runtime dependency on AI tools. The evaluator does not need ChatGPT, Claude Code, or any AI tool to build, run, test, or use the application. All instructions in the README use standard development workflows (Docker, .NET CLI).

## Development Process

```
Requirements (assessment PDF)
  -> Specification (docs/spec.md)
    -> Architecture (docs/architecture.md)
      -> Human-approved decisions (docs/architecture-decisions.md)
        -> Implementation (numbered prompts)
          -> Tests (TDD)
            -> Verification (E2E against Docker)
              -> Documentation
```

Every meaningful implementation action originated from a numbered prompt. Each prompt was documented in [docs/ai-development-log.md](ai-development-log.md) with: objective, agents used, AI proposals, human decisions, files changed, and results.

## Spec-Driven Development

The specification was created before any application code (Prompt 1). It defined:
- Functional requirements (FR-001 through FR-012).
- Business rules (BR-001 through BR-015).
- REST API contracts.
- Acceptance criteria (AC-001 through AC-020).
- Open decisions requiring human review.

All 16 original open decisions were reviewed, analyzed, and resolved through explicit human approval before implementation began.

## TDD Evidence

Tests were written before implementation for:
- **Domain entities** (Prompt 3.2): 32 tests covering Product, Category, and InventoryMovement behavior. Tests failed initially because the entity classes did not exist, then passed after implementation.
- **Command handlers** (Prompt 3.4): 21 tests covering all 7 command handlers. Tests failed initially, then passed.
- **Query handlers** (Prompt 3.5): 8 tests covering all 4 query handlers. Individual Red-Green cycles for each handler.

Total verified test baseline: 63 tests.

## Human Supervision Examples

The following cases demonstrate that AI output was reviewed and corrected by the human developer, not blindly accepted.

### 1. Cascade Delete Corrected to Restrict

AI generated an EF Core configuration with cascade delete between Product and InventoryMovement. Human review identified this contradicted the approved immutable/append-only movement design. Corrected to Restrict with 409 Conflict when movement history exists.

### 2. Keycloak Hostname v1 Corrected to v2

AI proposed `KC_HOSTNAME_URL` (Hostname v1). Human identified this as outdated. Corrected to `KC_HOSTNAME` (Hostname v2), which accepts a full URL in current Keycloak versions.

### 3. Swashbuckle Package Compatibility

Swashbuckle.AspNetCore 10.3.0 caused a compile error (`Microsoft.OpenApi.Models` namespace removed). Resolved by using Swashbuckle 6.9.0, which is compatible with .NET 8.

### 4. SQL Server Port Portability

AI used a fixed host port `1434:1433`. Human identified the portability concern. Changed to `${SQLSERVER_PORT:-1434}:1433` for evaluator flexibility.

### 5. RequireHttpsMetadata Security

AI set `RequireHttpsMetadata = false` unconditionally. Human rejected this as too broad. Changed to configuration-driven with a secure default (`true`), explicitly set to `false` only in the local Docker evaluation environment.

### 6. Keycloak Health Check

Initial Docker Compose startup failed because the Keycloak health check assumed `/health/ready` was available in `start-dev` mode. Corrected to a TCP-based health check.

### 7. Audience Validation Restored

During E2E testing, AI disabled `ValidateAudience` because Keycloak tokens lacked the expected audience claim. Human rejected this workaround. Solution: added a Keycloak audience mapper so tokens include `aud: inventory-api`, and restored strict audience validation.

### 8. Direct Access Grants Disabled

AI enabled Direct Access Grants on the public client for CLI testing convenience. Human rejected this because it reintroduced the password/ROPC-style flow that had been explicitly rejected earlier. Direct Access Grants were disabled. Real Authorization Code + PKCE was verified manually through Swagger.

## ROPC Rejection

The AI initially recommended Resource Owner Password Credentials (ROPC) for OAuth2 (Prompt 1.1). The human reviewer rejected it because:
- ROPC requires the client to handle user credentials directly.
- It is not appropriate for a new OAuth2 design.
- The assessment requires OAuth2, so a modern flow should be used.

The approved replacement is Authorization Code + PKCE with Keycloak, verified through the real browser-based Swagger flow.

## Reproducibility and Auditability

- Application execution is fully independent of AI tools.
- Docker, build, and test instructions are deterministic project workflows.
- Generative AI output is not expected to be byte-for-byte reproducible across sessions.
- The repository preserves the prompts, specifications, decisions, reviews, corrections, and resulting changes so the development process is auditable.

## Detailed Evidence

The complete chronological development log with every prompt, AI proposal, human decision, and correction is available in [docs/ai-development-log.md](ai-development-log.md).
