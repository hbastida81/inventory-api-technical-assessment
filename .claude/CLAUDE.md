# Redarbor Technical Test — Inventory Management API

## Project Context

Technical test for a Developer Senior position at Redarbor. The deliverable is a RESTful API for product inventory management.

## Tech Stack

| Area | Technology |
|---|---|
| Framework | .NET 8+, ASP.NET Core Web API |
| Language | C# 12 |
| Database | SQL Server (dockerized) |
| Reads | Entity Framework Core |
| Writes | Dapper |
| Pattern | CQRS |
| Auth | OAuth2 (JWT Bearer) |
| Infra | Docker + docker-compose |
| Testing | xUnit |
| Docs | Swagger + README.md |

## Workflow

```
GPT defines and supervises → Claude executes → Claude documents → GPT reviews → next task
```

Prompts arrive numbered (Prompt 0, 1, 2...). Execute only what the current prompt requests.

## Agent Sequence

```
REQUIREMENTS → /spec → /architect → APPROVED DECISIONS → /dev → /qa → DOCUMENTATION
```

1. **/architect** — analyzes impact, proposes architecture, identifies decision points.
2. **/spec** — transforms requirements into verifiable specs. Gate: human approval before code.
3. **/dev** — implements code following approved spec and architecture.
4. **/dba** — designs schema, migrations, SQL Server configuration.
5. **/qa** — validates implementation against spec, Clean Code rules, and test requirements.

## Mandatory Rules

### Clean Code (from test PDF)
1. All code and comments 100% in English.
2. No blank lines between code lines (one before each `return`).
3. Self-descriptive method and function names.
4. Single responsibility. Methods < 25 lines. No deep nesting.
5. Config from environment variables.
6. One file per class. One file per enum.
7. Zero commented-out code.
8. Pass objects, not loose properties.

### Architecture
- SOLID principles are mandatory.
- CQRS: EF Core for queries, Dapper for commands.
- Clean Architecture when appropriate.
- Patterns must be justified, not imposed.

### Persistence
- EF Core for READ operations.
- Dapper for WRITE operations.
- NO inline SQL in C# controllers or services — keep it in the infrastructure layer.
- Stored Procedures only when justified.

### Auth
- OAuth2 with JWT Bearer tokens.
- Protect all endpoints unless explicitly public.

### Docker
- `docker-compose up` must start API + SQL Server.
- All config via environment variables.

### Testing
- Unit tests for critical components.
- TDD approach.

## IA Evidence

This test evaluates AI usage. Every agent must produce traceable output:
- What was proposed and why.
- What information was used.
- What decisions are pending.
- What assumptions were made.

Evidence documents will be maintained in `docs/` directory.

## Automatic Agent Routing

Before executing any project task:

1. Analyze the requested task.
2. Identify the relevant specialized agent(s) using the routing table below.
3. Invoke only the appropriate agent(s).
4. Respect the workflow phases and human approval gates.
5. Do NOT invoke unrelated agents.
6. Do NOT bypass human approval gates.

### Routing Table

| Agent | Routes to when task involves |
|---|---|
| **spec** | Requirements analysis, specifications, use cases, API contracts, acceptance criteria, ambiguity detection, spec review |
| **architect** | Architecture, project structure, dependency boundaries, CQRS decisions, security architecture, OAuth2 design, Docker architecture, architectural trade-offs |
| **dev** | C# implementation, ASP.NET Core, application/domain code, commands/queries, validation, API endpoints, dependency injection, unit test implementation |
| **dba** | SQL Server, schema design, constraints, relationships, indexes, database scripts/migrations, persistence-specific concerns, SQL Server Docker config |
| **qa** | Validation against spec, endpoint verification, acceptance criteria validation, Clean Code review, SOLID review, test review, Docker verification, Swagger verification, README verification, final compliance |

### Multi-Agent Tasks

When a task crosses multiple responsibilities, use agents sequentially:

- **API contract definition:** spec → architect (only if architecture implications exist).
- **Database design:** architect (boundaries) → dba (relational design).
- **Feature implementation:** dev → qa.
- **Final compliance review:** qa, consulting spec/architect/dba only when needed.

### Phase Gate Enforcement

Do NOT execute a task if it belongs to a phase that has not been reached:

```
REQUIREMENTS → SPEC → ARCHITECTURE → APPROVED DECISIONS → IMPLEMENTATION → TESTING → VALIDATION → DOCUMENTATION
```

If the requested task belongs to a later unapproved phase, STOP and report:
```
PHASE VIOLATION: [task description]
Current phase: [current phase]
Required phase: [phase needed]
Blocking reason: [what approval is missing]
```

### Ambiguity Handling

When an important ambiguity is detected:
1. Route it to the appropriate agent.
2. Document the alternatives.
3. Expose it as a decision point.
4. Wait for human approval.

### Response Traceability

For each project prompt, include in the task result:
- Agent(s) selected and why.
- Current workflow phase.
- Whether human decision is required.

This routing information must also be added to `docs/ai-development-log.md`.

Agent selection is an AI action, not a human decision — document it accordingly.

## Prompt Traceability

Every meaningful project action must originate from a numbered prompt.

**Covered actions:** specification, architecture, technical decisions, project structure, dependencies, database design, source code, tests, Docker configuration, authentication/security, README/documentation, QA/compliance review.

**Rules:**
- Do NOT perform meaningful project changes from unnumbered side instructions.
- If a new task appears during execution and is outside the current prompt's scope: do not execute it. Report it as a proposed follow-up task and wait for a new numbered prompt.
- Do NOT perform opportunistic changes (unrelated refactors, unapproved packages, pattern changes, architecture changes, API contract changes, database behavior changes) unless they are explicitly part of the active numbered prompt.

### AI Development Log (docs/ai-development-log.md)

For every numbered prompt, record:
- Prompt ID, objective, workflow phase.
- Agent(s) selected and why.
- Files created / modified.
- AI proposals.
- Human-approved decisions used.
- New decisions detected.
- Decisions requiring human approval.
- Result and validation performed.

### Implementation Traceability

Once implementation starts, every implementation prompt must also record:
- Feature/use case implemented.
- Related Spec requirement IDs (FR-xxx).
- Related acceptance criteria (AC-xxx).
- Related ADRs (ADR-xxx).
- Files created/modified.
- Tests created/modified.
- Validation result.

Full chain: Requirement → Spec → Architecture Decision → Prompt → Implementation → Test.

### Decision Status Labels

- **Proposed by AI** — AI recommendation, not yet reviewed.
- **Accepted by Human** — human approved as-is.
- **Modified by Human** — human changed before approving.
- **Rejected by Human** — human rejected.
- **Pending Human Decision** — awaiting human review.

Never record an AI recommendation as a human decision.

### Human Review Preservation

When the human accepts, rejects, modifies, questions, or corrects an AI proposal, record in `docs/ai-development-log.md`:

- **AI Proposal** — what AI recommended.
- **Human Review** — what the human said.
- **Human Decision** — accepted / modified / rejected / pending.
- **Human Rationale** — the reason given (do not invent reasoning).
- **Resulting Action** — what changed as a result.

Do not rewrite an AI recommendation as if it were the human's idea. Preserve the technical meaning of human feedback. This applies to: architecture, security, persistence, business rules, dependencies, implementation choices, QA findings, and corrections.

### Cross-Document Updates

- Architecture changes → update `docs/architecture.md`.
- New/changed ADRs → update `docs/architecture-decisions.md`.
- Changed system behavior → update `docs/spec.md`.
- Do NOT modify these documents when the prompt does not affect them.

## Rule Priority

When conflict exists:
1. **Explicit test requirements (PDF)** — HIGHEST
2. Current prompt instructions
3. General best practices
4. Previous project conventions — LOWEST
