# Documentation Index — Inventory Management API

This directory contains the documentation artifacts for the Redarbor Developer Senior technical assessment.

## AI Usage Evidence

| Document | Purpose |
|---|---|
| [ai-development-log.md](ai-development-log.md) | Chronological log of every numbered prompt, AI proposals, human decisions, and results. **Start here for AI usage evidence.** |
| [ai-collaboration-summary.md](ai-collaboration-summary.md) | Evaluator-facing summary of AI collaboration, human supervision, and 8 concrete correction examples. |
| [architecture-decisions.md](architecture-decisions.md) | Architecture Decision Records (ADRs) with AI recommendations, human approvals/rejections, and reasoning. |

## Spec-Driven Development

| Document | Purpose |
|---|---|
| [spec.md](spec.md) | Formal functional specification created before implementation. Contains requirements (FR-xxx), business rules (BR-xxx), REST contracts, acceptance criteria (AC-xxx), and resolved decisions. |

## Architecture

| Document | Purpose |
|---|---|
| [architecture.md](architecture.md) | Approved architecture: project structure, dependency direction, CQRS design, persistence strategy, OAuth2/Keycloak integration, error handling, testing, Docker topology. |
| [architecture-decisions.md](architecture-decisions.md) | Individual decision records (ADR-001 through ADR-012) with context, alternatives, and human-approved outcomes. |

## Implementation

| Document | Purpose |
|---|---|
| [implementation-plan.md](implementation-plan.md) | Approved 13-phase implementation plan with TDD strategy, requirement/ADR mapping, and prompt sequence. |

## Assessment Deliverables

| Document | Purpose |
|---|---|
| [decision-making-answers.md](decision-making-answers.md) | Human-authored answers to the three decision-making questions from the assessment. |

## How to Navigate

- **Evaluator reviewing AI usage:** Start with `ai-collaboration-summary.md` for the overview, then `ai-development-log.md` for the full prompt history.
- **Evaluator reviewing architecture decisions:** Read `architecture-decisions.md`. Each ADR includes the AI proposal, human decision, and reasoning.
- **Evaluator reviewing Spec-Driven Development:** Read `spec.md`. It was created before any implementation code (Prompt 1) and refined through human review.
- **Evaluator reviewing human supervision:** See `ai-collaboration-summary.md` for 8 concrete examples where AI output was reviewed, corrected, or rejected.
- **Evaluator reviewing the rejected ROPC recommendation:** See ADR-002 in `architecture-decisions.md`.
