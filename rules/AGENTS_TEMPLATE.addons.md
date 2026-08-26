# AGENTS Template — Optional Add-ons

> Specialized sections that do NOT belong in the base constitution
> (`AGENTS_TEMPLATE.md`). Copy a section into your project's `AGENTS.md`
> only when the project actually uses the technology.
>
> **Principle:** the constitution carries rules every commit touches
> (tests, dates, style, decision guards). Everything below is opt-in per stack.

## How to use

1. Start from `AGENTS_TEMPLATE.md` (base).
2. Add an ORM add-on: `AGENTS_TEMPLATE.efcore.md` or `AGENTS_TEMPLATE.dapper.md`.
3. Add sections from this file one by one — only those that match your stack.
4. A section copied "just in case" is an unjustified guardrail (see base: Guardrails Justified by Risk).

## Symbolic Navigation (Context Economy)

> Copy if the agent has symbolic tools available (Serena MCP, language server).
> This is not a guardrail — it is a context budget rule: the agent's window is
> a limited resource, and whole-file reads burn it.

- Prefer symbol tools over file reads: `get_symbols_overview` for a new file,
  `find_symbol` / `find_referencing_symbols` for navigation
- ❌ Reading entire files when a symbol tool answers the question — **FORBIDDEN**
- ❌ Rewriting a whole file to change one symbol — use symbol-level edit tools
- Text search (`search_for_pattern`) is for strings and regex — never for
  finding classes/methods; `find_symbol` is precise

## API Type Sync Pipeline (Backend ↔ Frontend)

> Copy if the project has a typed frontend consuming the backend API.
> A prohibition alone ("no DTO change without client types") does not generate
> the types — a pipeline does. Prohibition + tool beats prohibition.

- Client types are **generated** from OpenAPI, not written by hand
  (e.g., `openapi-typescript` → `api.generated.ts`, marked "do not edit")
- ❌ Editing generated files — **FORBIDDEN**
- ❌ DTO change without regenerating client types — **FORBIDDEN**
- Regeneration is one command, documented in the project README

## Tooling Pinning

> Copy if the project uses local dotnet tools (Stryker, format validators, etc.).

- Local tools are pinned with exact versions in `dotnet-tools.json` — an agent
  running `dotnet tool run` gets the same tool version as CI and as every other
  developer. Pin the SDK separately via `global.json` (`rollForward: false`)
- ❌ Running an unpinned/global tool version for a gated check — **FORBIDDEN**

## Caching

> Copy if the project uses a cache (MemoryCache, IDistributedCache, Redis).

- ❌ `cache.Set()` without size limit / expiration — **FORBIDDEN** (risk of OOM)
- ✅ Explicit size or expiration — **MANDATORY**
- Keys centralized — no string literals in services
- Every write-path that changes data must invalidate related caches

## Performance / Hot Path

> Copy if the project tracks allocations on hot paths.

- After an agent's perf commit — **manual audit of write-paths**
- Agent optimizes read, human verifies write is not broken
- Load test scenario must pass before deploy (if applicable)
- Every `[HotPath]` method must have `{MethodName}_AllocationBudget` test;
  regressions > 10% are forbidden. `[HotPath]` is a project-local marker
  attribute — define your own convention (see `tests/patterns/AllocationBudgetTest.cs`)

## Complexity Thresholds

> Copy once the codebase is large enough for complexity drift to matter
> (for a young project, `AnalysisLevel=latest-recommended` in Directory.Build.props is enough).

- Cognitive complexity (`S3776`) threshold: default 15; API layer 10
- Cyclomatic complexity (`S1541`) threshold: default 10; API layer 7
- For new projects: `error` severity in `.editorconfig`
- For legacy: baseline + ratchet; number of violations must not increase
  (see `tests/patterns/ComplexityRatchetTest.cs`)
- Intentional deviations use `COMPLEXITY-###` ID and are recorded in `DECISION-GUARDS.md`

## Spellcheck

> Copy if the project has a public API or user-facing docs.

- `cspell` runs on markdown, comments, public type/property names, OpenAPI contracts
- Project dictionary lives in `cspell.json`
- New misspellings in public API names are **FORBIDDEN**
- Intentional domain terms use `SPELL-###` ID if they cannot be added to dictionary

## Mutation Testing

> Copy for critical assemblies before release. Skip if Stryker does not support your test framework.

- Run Stryker on critical assemblies (e.g., Domain) before release
- Mutation score must not decrease from baseline
- Survived mutants in critical code must be analyzed and covered or documented
- Intentional exceptions use `MUTATION-###` ID

## Analyzer Tests

> Copy only if the project has custom Roslyn analyzers.

- Every custom diagnostic ID must have positive + negative tests
- Tests must verify exact diagnostic span/location
- Run analyzer tests in CI when `Microsoft.CodeAnalysis.*` packages update
  (see `tests/patterns/AnalyzerTests.cs`)
