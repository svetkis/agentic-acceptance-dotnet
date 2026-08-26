# AGENTS.md — Guardrails for .NET Agentic Engineering

> ⚠️ **TEMPLATE** — Adapt to your project before use.
> Replace all `[ADAPT]` blocks with rules for your stack.
> Do not copy as-is: the specific technologies below are an example from DemoProject.
>
> **How to use:**
> 1. Copy this file to the project root as `AGENTS.md`.
> 2. Choose an ORM add-on:
>    - EF Core → supplement with `AGENTS_TEMPLATE.efcore.md`
>    - Dapper / Raw SQL → supplement with `AGENTS_TEMPLATE.dapper.md`
>    - Other ORM / No ORM → use this file as-is and add your own rules
> 3. Add optional sections (caching, hot path, complexity, spellcheck, mutation,
>    analyzer tests) from `AGENTS_TEMPLATE.addons.md` — **only those matching your stack**.
>
> **Principle:** the constitution carries only what every commit touches.
> Specialized rules live in add-ons, not here.

## Semantic Anchors

This file uses established terms instead of descriptions. Each term activates a specific methodology:

| Term | Meaning |
|------|---------|
| **BUG###** | Regression test: one bug = one file `BUG###_DescriptiveName.cs`, all paths covered |
| **Ratchet** | Test inventory: count of public types / tests / complexity violations / allocation budgets must not decrease |
| **Numbered Decision** | Intentional deviation: `PERF-###`, `DB-###`, `AUD-###`, `COMPLEXITY-###`, `SPELL-###`, `MUTATION-###` in comment, enforced by arch-test |

> Adapted from [Semantic Anchors](https://github.com/lexler/augmented-coding-patterns/blob/main/documents/patterns/semantic-anchors.md) pattern.

## Permission to Push Back

You MUST:
- Ask questions when instructions are unclear or contradictory
- Flag instructions that do not make sense for the codebase
- Describe the structure you see before making changes
- Require a plan or outline before implementation if the task is complex

You MUST NOT silently comply with instructions that violate:
- The test framework conventions defined below
- The hard prohibitions listed at the end of this file

> Adapted from [Silent Misalignment](https://github.com/lexler/augmented-coding-patterns/blob/main/documents/anti-patterns/silent-misalignment.md) anti-pattern.

## Context Markers

> Adapted from [Context Markers](https://github.com/lexler/augmented-coding-patterns/blob/main/documents/patterns/context-markers.md) pattern.

**ALWAYS** start replies with `STARTER_CHARACTER` + space. Stack emojis when requested, don't replace.

### Base markers

| Marker | Meaning |
|--------|---------|
| 🍀 | Ground rules loaded (default `STARTER_CHARACTER`) |
| ❗️ | Flagging an error or critical warning |
| ♻️ | Rules were just re-read (e.g., after context loss signal) |
| ✨📂 | Creating new repository / project structure |

### Role markers (stackable)

| Marker | Meaning | Trigger |
|--------|---------|---------|
| ✅ | Committer role active | User says "you're a committer" |
| 🔍 | Code Reviewer role active | User asks for diff review |
| 🔴 | TDD — Red phase | Process file `tdd.md` loaded, writing failing test |
| 🌱 | TDD — Green phase | Process file `tdd.md` loaded, making test pass |
| 🌀 | TDD — Refactor phase | Process file `tdd.md` loaded, refactoring |

### Read protocol

| Situation | Marker rule |
|---|---|
| Ground rules read **for the first time** in this session | `🍀` |
| Ground rules **re-read** on explicit request or context loss | `♻️` (replaces 🍀 temporarily, then stack returns to 🍀) |
| Skill read **for the first time** | Add skill marker to stack |
| Skill **re-read** | Add `♻️` before the skill marker: `♻️ 🔍` |

### Stacking rules

- Stack markers left-to-right: `🍀 ✅` = base rules + committer role.
- Always keep a space between any emojis and the text: `🍀 ✅ Commit message`.
- Never replace a marker — add to the stack. If context ends, remove only that marker.

### Impromptu markers

When adding a crucial instruction mid-conversation, ask the agent to reply with an additional emoji:

```
> From now on treat all DateTime as UTC. Reply with 🕒 added to your markers.
```

This makes the invisible parts of context visible at a glance.

### Process file integration

In specialized process files (e.g., `tdd.md`):
```markdown
STARTER_CHARACTER = 🔴 for red test, 🌱 for green, 🌀 when refactoring, always followed by a space
```

In role definitions (e.g., committer):
```markdown
When I tell you're a committer, add ✅ to STARTER_CHARACTER emojis. Make sure there's a space between any emojis and the text.
```

## Tests

> `[ADAPT]` — Replace with your test framework and run command.

- **Framework** — `[ADAPT]`: TUnit / xUnit / NUnit / MSTest
- Run command — `[ADAPT]`: for TUnit use `dotnet run --project tests/...`; for another framework, define the exact command explicitly.
- Every reproducible, automatable bug fix must include a test: `BUG###_DescriptiveName` (for configuration/documentation/operational/process defects, another regression control is allowed with an explicit rationale)
- **Trivial fix exception:** a genuinely trivial fix (typo, formatting, comment fix) may skip the regression test **only if** explicitly marked `trivial fix` in the commit message — an unmarked fix without a test blocks the commit. A hidden mark is not an escape hatch; if in doubt, write the test
- Failing test first → then fix → test passes
- **Self-checking, fault-sensitive tests only** — every test must determine pass/fail automatically (no manual interpretation), and its assertions must be reachable on every successful path. A test must fail when the behavior its name promises is broken. Forbidden: tests without assertions, `IsNotNull()`-only assertions, assertions that can be bypassed on the successful path (an assert inside an `if` that may not execute), tautological assertions (`x == x`, `expect(true)`), `waitForTimeout` in UI tests, negative-only assertions without a positive control — unless the weaker check *is* the contract and the reason is documented. For critical behavior, verify fault sensitivity explicitly (mutation testing or deliberate fault injection: break the code, confirm the test fails). See the [Non-Validating Tests trap](https://github.com/svetkis/dotnet-ai-guardrails/blob/main/docs/traps/testing.md#non-validating-tests).

## Database Conventions

> `[ADAPT]` — Replace with your DB naming conventions.

- Columns — `[ADAPT]`: `snake_case` / `PascalCase` / `camelCase`
- Tables — `[ADAPT]`: naming and pluralization rules
- Indexes — `[ADAPT]`: naming convention (e.g., `IX_table_column`)

## API / DTO

> `[ADAPT]` — Define your API contract rules.

- ❌ Changing DTO without updating client types — **FORBIDDEN**
- ❌ Changing Response DTO without regenerating contract snapshot — **FORBIDDEN**
- If a typed frontend consumes the API: prefer a generation pipeline over manual
  sync — see `AGENTS_TEMPLATE.addons.md` → API Type Sync Pipeline

## Dates

- All dates in DB: **UTC**
- Backend: `DateTime.UtcNow`, `DateTime.SpecifyKind(..., DateTimeKind.Utc)`
- JSON: `"2025-02-27T10:00:00Z"` (with Z!)
- ❌ UTC→Local conversion on backend — **FORBIDDEN**

## Commits

- Conventional Commits: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `db`
- Documentation update — in the same commit as code

## Code Style

- Follow **Microsoft C# Naming Conventions**: interfaces (`I` + PascalCase), types (PascalCase), members (PascalCase), parameters/locals (camelCase), private fields (`_camelCase`), static fields (`s_camelCase`), async methods (`…Async`)
- `.editorconfig` is enforced at build time (`EnforceCodeStyleInBuild = true`)
- Code-style warnings are treated as errors (`CodeAnalysisTreatWarningsAsErrors = true`)
- Run `dotnet format` before commit if IDE does not format on save

## Code Review by Agent

- Before commit, run a **separate agent** to review changes and show results
- When reviewing a commit with `fix:` — check that a regression test file exists (new or modified)
- Without a regression test, the fix is not considered complete

## Decision Guards in Code

- Intentional deviation from standard must be documented with ID: `PERF-###`, `DB-###`, `AUD-###`
- IDs must be unique — checked by architectural test
- Example: `// PERF-022: QueryFilter removed — JOIN added 3ms to every query`
- Full registry template: [`DECISION-GUARDS.md`](../templates/skills/skeptical-ai-bootstrap/DECISION-GUARDS.md)

## Guardrails: Justified by Risk

> Any guardrail (test, regex scan, arch-test, linter rule) must answer: **"What specific risk does this cover?"**

- ✅ Guardrail justified by a real incident, a credible threat model, a regulatory requirement, or a documented high-impact failure scenario — **REQUIRED**
- ✅ Regression test for every reproducible, automatable defect — **REQUIRED** (for configuration/documentation/operational/process defects, another regression control is allowed with an explicit rationale)
- ⚠️ Zero triggers is **not** sufficient grounds for removal — weigh risk severity, likelihood, maintenance cost, false-positive rate, and compensating controls
- ❌ Guardrail with no risk justification at all ("just in case") — **FORBIDDEN** (unjustified guardrail = over-engineering)

## Hard Prohibitions

- ❌ Commit without `dotnet build` + tests
- ❌ New env var without updating deployment docs
- ❌ Raw SQL without explanatory comment
- ❌ Guardrail without a risk justification (see section above)

> Optional prohibitions (hot path, complexity, spellcheck, analyzers) live in
> `AGENTS_TEMPLATE.addons.md` — copy them only with the matching section.
