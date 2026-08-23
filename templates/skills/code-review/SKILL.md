---
name: code-review
description: |
  Pre-commit code review for backend projects with agent-generated code.
  Trigger this skill right before `git commit`, when the user asks to review staged changes,
  or when the staged diff contains backend source files.
  Reviews staged changes against architectural rules, ORM constraints, test conventions,
  and security hygiene.
whenToUse:
  - Before committing backend changes (pre-commit).
  - User says "pre-commit review", "review my staged changes", "check the diff", "code review".
  - Staged diff includes source files of the project's backend language.
triggers:
  - pre-commit
  - review staged diff
  - code review
  - check diff before commit
invocation:
  manual: true
  auto: true
version: 1.0.0
---

# Pre-commit Code Review Agent

## Purpose and Non-Goals

- Review **only staged changes** (`git diff --cached`) and only added (`+`) lines with immediate context, against architectural rules, ORM constraints, test conventions, and security hygiene.
- Act as a second, focused reviewer: a dedicated review agent outperforms the agent that wrote the code (Focused Agent), and catches silent task misalignment before it reaches `main` (Silent Misalignment).
- **Non-goals:** reviewing unchanged code or entire files without a diff; business-logic sign-off; running `git commit` on the user's behalf.

## Applicability and Exclusions

**Requires adaptation to the project:**
- Language / stack: C#, Java, Python, Go, TypeScript, etc.
- Authorization framework: MVC attributes, Minimal API middleware, decorators, guards.
- ORM / data access: EF Core, Dapper, Hibernate, SQLAlchemy, raw SQL, etc.
- Architectural boundaries: Clean Architecture, Vertical Slices, Ports & Adapters, single-project MVP.
- Test framework and bug-regression test naming convention.

**Adaptation notes:**
- **Single-project MVP without layered architecture** → skip cross-project dependency checks. If there are no separate Domain / Infrastructure / Application projects — the dependency rule does not apply.
- **Minimal API / non-MVC** → check middleware / fluent authorization, not MVC attributes.
- **ORM with projection DTOs** — if the ORM does not track projections, `AsNoTracking` (or equivalent) is not required. Do not flag missing read-only mode on projections.
- **Dapper / raw SQL (no ORM tracking)** → skip ORM-specific checks (change tracking, eager loading, async find helpers). Use the raw SQL section.

**Not applicable when:**
- Changes are only in frontend / mobile / desktop UI — out of scope for this .NET skill; mark N/A or use a stack-specific skill created per `templates/skills/ADAPTATION.md`.
- There are no staged changes.
- The user asks for a full-file review without a diff.

## Required Inputs

- Read access to the repository and the staged diff (`git diff --cached`; in PR flow use `git diff main...[branch]`).
- Project stack facts: language, ORM, architecture layout, test framework and bug-regression test naming convention.
- Optional: the project's `AGENTS.md` / rules files for project-specific conventions.

## Procedure

1. Read the staged diff: backend source files only. Review ONLY `+` lines and directly related context lines — NEVER unchanged code or entire files.
2. If there are no staged backend changes, tell the user there is nothing to review and stop.
3. Apply the checks below to every `+` block. If some checks are not applicable to the stack — mark them N/A, do not report as findings.
4. Produce findings in the required format and verify evidence for each (see Evidence Requirements).
5. State a verdict. If **CHANGES_REQUESTED**, advise the user to fix BLOCKER/CRITICAL/MAJOR issues and stage the fixes before committing. Do NOT run `git commit` yourself.

### Backend-specific checks (adapt to your stack)

#### If project uses async public methods
- Every public `async` method accepts `CancellationToken ct = default`.

#### If project uses ORM with change tracking
- **Read-path:** if a method returns an entity (not a projection), it must have explicit read-only mode (`AsNoTracking` or equivalent). Projections to DTO/record do not require read-only mode if the ORM does not track them.
- **Write-path:** no read-only mode on write operations. Exception: raw SQL and bulk API that the change tracker does not track anyway.

#### If project uses raw SQL / Dapper / ADO.NET
- **SQL Injection:** any string interpolation or concatenation in SQL query — BLOCKER.
- **Parameterization:** all SQL queries use parameters. No `string.Format` in SQL.
- **CommandTimeout:** every query/execute call has a timeout or uses a global default.
- **Transactions:** write operations must be inside a transaction.
- **Dynamic IN:** dynamic `IN` lists must use TVP, temp table, or ORM equivalent. `string.Join` in SQL — BLOCKER.

#### If project uses layered / hexagonal architecture
- Domain MUST NOT reference Infrastructure **only if the project has layer separation** (2+ projects with Domain / Infrastructure / Application suffixes). For single-project MVP — mark N/A.

#### API / DTO
- API returns DTOs/records, not entities directly.
- Naming is consistent: `OrderResponse`, `CreateOrderRequest`, `OrderListItem` — not generic `OrderDto` everywhere.

#### Tests
- The project uses a single test framework (no mixing of xUnit/NUnit/MSTest/Jest/Mocha without reason).
- Every bug fix has a named regression test per project convention.

#### Common hygiene
- Dates: if the project uses UTC/ISO 8601 — check compliance.
- Exception handling: no empty `catch { }`. At minimum log + rethrow or throw custom exception.
- Nullability: respect nullable reference types / optional types.
- **Duplication (Literal):** if validation/calculation/status check is added, verify it does not already exist in another service. Business rules belong in Domain, not copy-pasted into Application/API.
  - Automated guardrail: an equivalent `DuplicationGuardTest` catches literal copy-paste.
  - Human guard: CHECKLIST.md "Semantic Duplication" catches `IsConfirmed()` vs `Status == Confirmed` — same rule, different code.

### Cross-Layer Drift Checks

Refactors that change DTOs, domain events, or models often break contracts
between layers. For changes touching 2+ layers, check:

- [ ] **DTO / domain event contract:** a field change in a DTO or domain event
  is accompanied by updates to all consumers (handlers, jobs, frontend,
  API schema snapshot).
- [ ] **AuthZ / ownership drift:** a permission check change in API does not leave
  a bypass in domain service or background job.
- [ ] **Cache invalidation drift:** a write operation addition/change is accompanied by
  cache invalidation on all levels where this object is cached.
- [ ] **Timezone / date contract:** a storage or transmission format change
  is agreed between UI, API, DB, and jobs.
- [ ] **Migration / runtime drift:** a domain model change is accompanied by
  a migration; breaking changes are not merged without backward compatibility.
- [ ] **Broken invariant after refactor:** a business rule that was previously
  checked in one place has not been accidentally removed or smeared across
  several services with contradictory semantics.
- [ ] **What could silently go wrong?** For every finding ask: which
  end-to-end invariant will break after merge, even though per-layer unit tests
  are green?

### Project-specific examples

> The examples below illustrate applying portable checks to a concrete stack. Replace with your own stack.

#### Example: .NET 10 + EF Core + PostgreSQL + Minimal API

- **EF Core Read-Path:** if read-path returns an entity (not a projection) — it must have `.AsNoTracking()`. Projections `.Select()` to DTO/record do NOT require `.AsNoTracking()`.
- **EF Core Write-Path:** no `.AsNoTracking()` on write operations. Exception: raw SQL (`FromSqlRaw`, `ExecuteSqlRaw`, `ExecuteUpdateAsync`).
- **Dapper / Raw SQL:** interpolation in SQL — BLOCKER; parameterization required; `CommandTimeout`; `IDbTransaction` for writes; dynamic `IN` via TVP/temp table.
- **Architecture:** Domain MUST NOT reference Infrastructure **only if the project has layer separation**.
- **Tests:** `[Test]` + `await Assert.That(value).IsEqualTo(expected)` (TUnit convention). No xUnit/NUnit syntax.
- **DateTime:** All backend dates MUST be UTC (`DateTime.UtcNow`, `DateTimeKind.Utc`).
- **PostgreSQL:** Column names `snake_case` via `.HasColumnName()`.
- **BUG Pattern:** Every bug fix MUST have `BUG###_DescriptiveName` test.

## Evidence Requirements

Every finding MUST include:
1. **Exact file path** and **line number**
2. **Quoted snippet** (3-5 lines)
3. **Rule violated** (from checks above)
4. **Fix**: specific action or code suggestion
5. **Self-Correction**: "Can I point to exact line? Did I read the file myself?"

If you cannot satisfy 1-4, you MUST NOT report the finding.

## Finding Schema

```text
ID
Severity: BLOCKER | CRITICAL | MAJOR | MINOR
Confidence: CONFIRMED | NEEDS_REVIEW
Category / Control
Evidence: file:line, command output, trace or reproduction
Impact
Recommended action
Owner / disposition
```

Compact one-line form for pre-commit output:

```text
[SEVERITY] [CONFIDENCE] Title | File:line | Rule | Evidence: "quoted snippet" | Fix: action
```

## Severity and Confidence

| Severity | Meaning |
|----------|---------|
| **BLOCKER** | Change/release must not proceed; immediate action required |
| **CRITICAL** | High impact; fix in the current iteration |
| **MAJOR** | Degradation or defect; schedule the fix |
| **MINOR** | Improvement; backlog |

Project-specific mapping:
- **BLOCKER**: Security vulnerability, data loss risk, compilation error, test breakage.
- **CRITICAL**: ORM write-path with read-only tracking OR SQL injection, missing `CancellationToken` (if project uses async), `async void`, race condition, architecture violation.
- **MAJOR**: Missing test, exception swallowing, N+1 query, unhandled nullable, DTO mismatch, business logic duplication.
- **MINOR**: Naming inconsistency, missing XML doc, magic number, formatting, trailing whitespace, unused import/using.

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Proven by evidence: file:line, reproduction, command output |
| **NEEDS_REVIEW** | Investigation signal; requires human judgment before action |

Use **NEEDS_REVIEW** for architectural checks in projects without Clean Architecture, for ORM rules with projections, etc.

## Outputs and Downstream Consumer

**Output format:** findings list in the format above, then a verdict:
- **APPROVED**: 0 BLOCKER/CRITICAL/MAJOR
- **APPROVED_WITH_NITS**: only MINOR findings (style/formatting)
- **CHANGES_REQUESTED**: any BLOCKER/CRITICAL/MAJOR

**Consumer:** the user, right before commit — findings + verdict + whether it is safe to commit. The user fixes and stages; this skill never commits. In PR flow the same report is posted on the branch diff.

## Trigger or Schedule

- **Default trigger:** automatically right before every `git commit` when staged changes include backend source files.
- **Explicit invocation:** `/skill:code-review` or phrases: "pre-commit review", "review my staged changes", "check the diff", "code review".
- **PR flow:** run after Task Compliance Agent confirms no scope creep; use `git diff main...[branch]`.

## Limitations and Expected False Positives

- Stack-sensitive checks (layered architecture, ORM tracking modes, MVC attributes) produce false positives on single-project MVPs, projection-heavy code, and Minimal API — mark N/A after adaptation instead of reporting.
- Scope is limited to the staged diff and its immediate context; cross-file invariant breaks beyond the diff may be missed (Cross-Layer Drift checks partially compensate).
- A finding without verifiable file:line evidence is an investigation signal, not a defect.

> Optional interaction convention (agent-specific): some agents add `🔍` to their starter-character stack while this skill is active. Not required — the skill is fully usable without emoji.
