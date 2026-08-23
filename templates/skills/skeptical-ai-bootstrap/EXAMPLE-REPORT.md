# Onboarding Report Example (Principled Approach)

> This example shows how the agent adapts to a non-standard stack
> instead of blindly copying artifacts.

---

# Onboarding Report: Logistics.Worker

**Date:** 2026-06-15  
**Mode:** standard  
**Agent:** skeptical-ai-bootstrap

---

## Summary by Principles

| Level | Principle Adhered | Current State | Decision |
|------|-------------------|---------------|----------|
| Foundation | 🔴 | No AGENTS.md | **Implement**: `rules/AGENTS_TEMPLATE.md` |
| 1. Change Checks | 🟡 | Nullable enable, but no TreatWarningsAsErrors | **Adapt**: Directory.Build.props |
| 2. Behavior — architecture | 🔴 | No arch tests. Vertical Slice — standard NetArchTest rules (about layers) are not applicable | **Adapt**: NetArchTest with custom rules about feature boundaries |
| 2. Behavior — tests | 🟡 | xUnit, 340 tests, no "0 ran" check | **Adapt**: run-and-verify-tests.sh for xUnit |
| 2. (gate) Code review | 🔴 | Dapper + MediatR — ready-made skill does not fit | **Create skill**: `code-review-dapper` |
| 3. System Checks | 🔴 | Worker Service, no HTTP. OpenAPI snapshot impossible | **Create skill**: `e2e-worker` |
| 4. Reality Checks | 🔴 | No audits. Dapper + SQL Server — ready-made DBA does not fit | **Create skill**: `dba-audit-dapper` |

---

## Project Stack

- **.NET:** 8.0 (`net8.0`)
- **Type:** Worker Service (BackgroundService + RabbitMQ)
- **ORM/Data:** Dapper 2.1 + SQL Server (no EF Core)
- **Architecture:** Vertical Slice (Features/Orders/, Features/Deliveries/)
- **Test framework:** xUnit (340 tests, migrating to TUnit is not cost-effective)
- **CI:** GitHub Actions (simple `dotnet test`)
- **Messaging:** RabbitMQ + MassTransit

---

## Agent-Specific Configuration

The project uses **Claude Code**. Therefore, guardrails configuration will be in Claude format:

```
.claude/
├── CLAUDE.md                      # Constitution (adapted from rules/AGENTS_TEMPLATE.md)
├── commands/
│   ├── code-review-dapper.md      # Code review for Dapper + MediatR
│   ├── architecture-audit.md        # NetArchTest with custom rules for VSlice
│   ├── task-compliance.md         # Scope check (adapted)
│   ├── e2e-worker.md              # E2E for Worker + RabbitMQ
│   ├── security-audit.md          # Adapted from templates/skills/security-audit/
│   └── dba-audit-dapper.md        # DBA audit for Dapper
```

If the project used **Kimi** — skills would be in `.kimi/skills/`.
If **Codex** — rules in `AGENTS.md`, audits as skills in `.agents/skills/`.

---

## Why Ready-Made Artifacts Do Not Fit

### Architecture: NetArchTest
The project is Vertical Slice. Boundaries are by feature, not by layer.
NetArchTest checks `Domain` → `Application`, but there are no such projects here.
A slice dependency scanner is needed.

### Code Review: EF-Specific Rules
The ready-made skill checks `AsNoTracking`, `.Include()`, `[Authorize]`.
In a Dapper project, these rules are meaningless. Need rules:
- SQL parameterization (no string interpolation)
- Async Dapper methods (`QueryAsync`, not `Query`)
- Transaction scope in handlers

### E2E: OpenAPI Snapshot
Worker Service has no HTTP. E2E is checking message processing
from queue and side-effects (DB records, logs, metrics).

### DBA Audit: EF Migrations
The ready-made skill looks at EF migrations, `Include()`, `FindAsync()`.
In a Dapper project — need to check raw SQL, indexes, query plans.

---

## Implementation Backlog

### Sprint 0 — Control Foundation + Change Checks (1 day)
- [ ] **Implement** `rules/AGENTS_TEMPLATE.md` → adapt for Worker + Dapper
- [ ] **Implement** `rules/CONVENTIONS.md`
- [ ] **Adapt** `Directory.Build.props`:
  - `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
  - `<Nullable>enable</Nullable>`
- [ ] **Adapt** `.editorconfig` (severity = error for critical rules)

### Sprint 1 — Architecture (3 days)
- [ ] **Adapt** `ArchitectureRules.cs` for Vertical Slice:
  - Check: Slice A does not import internals of Slice B
  - Check: each Slice has Handler + Validator + Endpoint (or Worker consumer)
  - Check: Shared Kernel is explicitly marked // DECISION:
  - Implementation: NetArchTest custom rules + unit tests
  - Template: `NEW-SKILL-TEMPLATE.md`
  - Put in: `tests/ArchitectureTests/ArchitectureRules.VSlice.cs`

### Sprint 2 — Tests (2 days)
- [ ] **Adapt** run-and-verify-tests.sh for xUnit (do not migrate to TUnit!)
  - Parse output of `dotnet test --logger "console;verbosity=detailed"`
  - Verify that `Total tests: > 0`
- [ ] **Adapt** `tests/patterns/RatchetTest.cs`:
  - Add ratchet for project custom attributes (e.g., `[CriticalHandler]`) in addition to test count
- [ ] **Create** integration tests for RabbitMQ consumer:
  - Pattern: In-memory queue + test host

### Sprint 3 — Code Review Agent (2 days)
- [ ] **Create skill** `code-review-dapper`:
  - Rules: parameterized SQL, async Dapper, transaction scope
  - Rules: MediatR handler must not be fat (>50 lines)
  - Template: `NEW-SKILL-TEMPLATE.md`
  - Put in: `.claude/commands/code-review-dapper.md` (for Claude) or `.kimi/skills/code-review-dapper/` (for Kimi)
- [ ] **Adapt** `templates/skills/task-compliance/` (fits 1-to-1, just replace stack references)

### Sprint 4 — E2E + CI (3 days)
- [ ] **Create skill** `e2e-worker`:
  - Test: publish message to in-memory bus → check processing
  - Test: check side-effects (DB records, logs)
  - Test: dead letter queue on error
  - Template: `NEW-SKILL-TEMPLATE.md`
- [ ] **Adapt** `ci/github-actions/safe-ci.yml`:
  - Replace `dotnet run --project` with `dotnet test` (xUnit)
  - Add step: run Worker in test host for integration tests

### Backlog — Audits (once per sprint)
- [ ] **Create skill** `dba-audit-dapper`:
  - Check: all SQL queries are parameterized
  - Check: no `SELECT *` (explicit columns)
  - Check: indexes for WHERE columns (analysis via SQL Server DMVs)
  - Template: `NEW-SKILL-TEMPLATE.md`
- [ ] **Adapt** `templates/skills/security-audit/` (fits 1-to-1)
- [ ] **Skip** `templates/skills/i18n-audit/` (project is Russian only, documented)

---

## New Skills to Create

| Skill | Reason | Complexity |
|-------|--------|------------|
| `ArchitectureRules.cs` (adapted) | Vertical Slice, needs custom NetArchTest rules | Medium |
| `code-review-dapper` | Dapper + MediatR, ready-made skill is about EF Core | Low |
| `e2e-worker` | Worker Service, no HTTP/OpenAPI | Medium |
| `dba-audit-dapper` | Raw SQL, needs query and index audit | Medium |

**Total new skills:** 4  
**Total adapted artifacts:** 5  
**Total implemented 1-to-1:** 2 (`AGENTS.md`, `CONVENTIONS.md`)

---

## New Skill Design (Details)

The agent designed each new skill fully — not just a name, but role, mechanism, integration.

### `ArchitectureRules.cs` (Adapted for VSlice)
- **Threat Model:** Agent adds cross-slice dependency or breaks feature boundary
- **Role:** Build Guard (triggers on build)
- **Mechanism:** NetArchTest custom rules
- **Input:** Project build
- **Output:** Test result (pass/fail)
- **Trigger:** Every `dotnet build` → `dotnet run --project tests/ArchitectureTests/`
- **Gate:** BLOCKER
- **Rules:**
  - `Features/X/.*` does not depend on `Features/Y/.*` (except `Features/Shared/.*`)
  - Each slice has exactly one `*Handler`, one `*Validator`
  - Internal slice types are `internal`, not `public`

### `code-review-dapper`
- **Threat Model:** SQL injection, blocking calls, N+1 via Dapper
- **Role:** Reviewer (triggers on PR)
- **Mechanism:** AI Agent (skill)
- **Input:** Git diff PR
- **Output:** PR comments / report
- **Trigger:** Every PR
- **Gate:** BLOCKER/CRITICAL/MAJOR/MINOR
- **Rules:**
  - No string interpolation in SQL (`$"SELECT ... {id}"`)
  - `QueryAsync`/`ExecuteAsync` instead of sync methods
  - TransactionScope or `using var tran` in write operations
  - Method >50 lines — split or justify // DECISION:

### `e2e-worker`
- **Threat Model:** Message loss, duplicate processing, lack of idempotency
- **Role:** Integration Guard (triggers in CI)
- **Mechanism:** Unit test + test host (In-memory RabbitMQ)
- **Input:** Project build
- **Output:** Test result (pass/fail)
- **Trigger:** Every PR + nightly
- **Gate:** BLOCKER
- **Rules:**
  - Publish message → check processing within 5 seconds
  - Publish duplicate → check idempotency (not 2 DB records)
  - Break handler → check retry + dead letter queue

### `dba-audit-dapper`
- **Threat Model:** Perf degradation due to missing indexes, N+1 in raw SQL
- **Role:** DBA Auditor (triggers on demand)
- **Mechanism:** AI Agent + SQL Server DMV query script
- **Input:** Git diff + DB schema
- **Output:** Report with recommendations
- **Trigger:** Once per sprint / when `*.sql` or `*Repository*.cs` changes
- **Gate:** WARNING (does not block, but requires ack)
- **Rules:**
  - All SQL with `WHERE` has covering index (check via DMV)
  - No `SELECT *` (explicit column list)
  - No `TOP 100` without `ORDER BY`
  - Query execution time < 100ms on test data

---

## Skill Ecosystem Map (Generated)

```markdown
# Project Skills: Logistics.Worker

## 2. Behavior Checks (gate before PR)
| Skill | Role | Mechanism | Trigger | Gate | Status |
|-------|------|-----------|---------|------|--------|
| code-review-dapper | Reviewer | AI Agent | PR | BLOCKER | 🚧 WIP |
| task-compliance | Scope Guard | AI Agent | PR | BLOCKER | 📋 Backlog |
| architecture-audit (VSlice) | Build Guard | NetArchTest custom | Build | BLOCKER | 🚧 WIP |

* compiler-guard (Level 1) is ✅ Active — Sprint 0 activates TreatWarningsAsErrors

## 3. System Checks
No ready-made artifact fits (Worker Service, no HTTP) —
covered by the project-specific `e2e-worker` below.

## 4. Reality Checks
| Skill | Role | Mechanism | Trigger | Gate | Status |
|-------|------|-----------|---------|------|--------|
| security-audit | Security Auditor | AI Agent | Weekly | WARNING | 📋 Backlog |
| dba-audit-dapper | DBA Auditor | AI Agent + Script | Sprint | WARNING | 🚧 WIP |
| performance-audit | Perf Auditor | AI Agent + NBomber | Release | BLOCKER | 📋 Backlog |

## Project-Specific
| Skill | Role | Mechanism | Trigger | Gate | Status |
|-------|------|-----------|---------|------|--------|
| e2e-worker | Integration Guard | Unit Tests | PR + Nightly | BLOCKER | 🚧 WIP |
```

---

## Not Applicable (Documented)

- **OpenAPI Snapshot** — Worker Service project, no HTTP endpoints
- **i18n Audit** — project is for Russian market only, Russian language
- **Standard NetArchTest rules** — layer rules (Domain→Application) are not applicable to VSlice. Replaced with custom rules about feature boundaries
- **TUnit migration** — 340 tests on xUnit, migration cost exceeds benefit

---

## Risks

1. **4 new skills** — this is 2-3 days of work just to describe the rules. But without them, guardrails will be useless.
2. **xUnit remains** — we do not migrate to TUnit because 340 tests + infrastructure. run-and-verify-tests.sh solves the "0 ran" problem.
3. **Vertical Slice + Dapper** — no ready-made patterns in `dotnet-ai-guardrails`. But the principles are the same: auto-check, ratchet, code review.

---

## Recommendation

Start with **Sprint 0** (Control Foundation + Change Checks) + adapting `ArchitectureRules.cs` for VSlice.
This gives 60% effect: agents will stop breaking slice boundaries and nullable.

Other skills can be created as needed — what matters is that
**principles** work, not the specific tools.
