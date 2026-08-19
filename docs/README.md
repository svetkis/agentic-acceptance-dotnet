# Knowledge Map

> Unified table of contents for all repository artifacts.  
> If you are here for the first time — start with [GLOSSARY.md](../GLOSSARY.md), then return here.
>

---

## Quick Start by Role

| I am a ... | Where to start |
|------------|----------------|
| **Newcomer** to agentic development | [GLOSSARY.md](../GLOSSARY.md) → [PYRAMID.md](../PYRAMID.md) → `examples/DemoProject/` |
| **Tech Lead** implementing guardrails | [ONBOARDING.md](ONBOARDING.md) → [.agents/skills/skeptical-ai-bootstrap/SKILL.md](../.agents/skills/skeptical-ai-bootstrap/SKILL.md) → [ADAPTATION.md](../templates/skills/ADAPTATION.md) → "Outer Loop" section below |
| **Developer** looking for a test pattern | [tests/patterns/](#test-patterns) → copy into your project |
| **Implementing SAE from scratch** | [ONBOARDING.md](ONBOARDING.md) → step-by-step guide with checkpoints |
| **Auditor** preparing for an audit | [templates/skills/](#skills-audits) → take CHECKLIST.md → [human-audit-bridge.md](solutions/human-audit-bridge.md) for manual walkthrough |
| **Contributor** | [CONTRIBUTING.md](../CONTRIBUTING.md) → "What can be added" section |

---

## Pyramid: 3 Layers (0–2) + Outer Loop

| Layer | Sub-layer | What it is | Main document | Artifacts |
|-------|-----------|------------|---------------|-----------|
| **0** | — | Instructions for the agent before code | [PYRAMID.md §Layer 0](../PYRAMID.md#layer-0) | `rules/AGENTS_TEMPLATE.md` + Decision Guards (ADR) |
| **1** | 1.1 Compiler | Fast feedback from types | [PYRAMID.md §1.1](../PYRAMID.md#layer-1-compiler) | `.editorconfig`, `Directory.Build.props`, `DemoProject.Analyzers` (custom Roslyn analyzer) |
| **1** | 1.2 Architecture | Auto-check of layers and anti-patterns | [PYRAMID.md §1.2](../PYRAMID.md#layer-1-architecture) | [tests/patterns/ArchitectureRules.cs](../tests/patterns/ArchitectureRules.cs), [RatchetTest.cs](../tests/patterns/RatchetTest.cs), [ArchUnitNetSliceTest.cs](../tests/patterns/ArchUnitNetSliceTest.cs) |
| **1** | 1.3 Tests | Regressions, snapshot, vibe-refactoring, API contracts | [PYRAMID.md §1.3](../PYRAMID.md#layer-1-tests) | [tests/patterns/](#test-patterns) |
| **1** | 1.4 Code Review | Agent checks agent (pre-commit / PR) | [PYRAMID.md §1.4](../PYRAMID.md#layer-1-code-review) | [templates/skills/code-review/SKILL.md](../templates/skills/code-review/SKILL.md), [templates/skills/frontend-code-review/SKILL.md](../templates/skills/frontend-code-review/SKILL.md) |
| **1** | 1.5 Smoke | Fast run of critical scenarios | [PYRAMID.md §1.5](../PYRAMID.md#layer-1-smoke) | — |
| **2** | 2.1 E2E / MCP | Full scenarios through external systems | [PYRAMID.md §2.1](../PYRAMID.md#layer-2-e2e) | [tests/patterns/SnapshotTest.cs](../tests/patterns/SnapshotTest.cs) |
| **2** | 2.2 Audits | Deep checks on schedule | [PYRAMID.md §2.2](../PYRAMID.md#layer-2-audits) | [templates/skills/](#skills-audits) |
| **2** | 2.3 Load | Silent breakdown under load (NBomber) | [PYRAMID.md §2.3](../PYRAMID.md#layer-2-load) | [tests/patterns/LoadTest.cs](../tests/patterns/LoadTest.cs) |
| **Outer Loop** | — | Final human validation, business and product decisions | [PYRAMID.md §Outer Loop](../PYRAMID.md#outer-loop) | — |

---

## Test Patterns

All templates are `copy-paste friendly`. Each contains comments `// TRAP:` and `// GUARDRAIL:`.

| Pattern | Purpose | Location | Working example in DemoProject |
|---------|---------|----------|-------------------------------|
| **ArchitectureRules** | Universal layer dependency check (NetArchTest) | [tests/patterns/ArchitectureRules.cs](../tests/patterns/ArchitectureRules.cs) | `examples/DemoProject/tests/DemoProject.Tests/ArchitectureRules.cs` |
| **EfCoreGuardRules** | EF Core-specific guardrails: `FindAsync`, `Include`, `AsNoTracking` | [tests/patterns/EfCoreGuardRules.cs](../tests/patterns/EfCoreGuardRules.cs) | `examples/DemoProject/tests/DemoProject.Tests/EfCoreGuardRules.cs` |
| **DapperGuardRules** | Dapper / Raw SQL guardrails: parameterization, injections, timeouts | [tests/patterns/DapperGuardRules.cs](../tests/patterns/DapperGuardRules.cs) | — |
| **ArchUnitNetSliceTest** | Cyclic dependencies between slices (ArchUnitNET) | [tests/patterns/ArchUnitNetSliceTest.cs](../tests/patterns/ArchUnitNetSliceTest.cs) | `examples/DemoProject.Traps/tests/DemoProject.Traps.Tests/ArchUnitNetSliceTest.cs` |
| **RatchetTest** | Public types and tests did not decrease | [tests/patterns/RatchetTest.cs](../tests/patterns/RatchetTest.cs) | `examples/DemoProject/tests/DemoProject.Tests/RatchetTests.cs` |
| **SnapshotTest** | JSON serialization contract, OpenAPI | [tests/patterns/SnapshotTest.cs](../tests/patterns/SnapshotTest.cs) | `examples/DemoProject/tests/DemoProject.Tests/SnapshotTests.cs` |
| **LoadTest** | Silent breakdown under load: read optimizations that break write path | [tests/patterns/LoadTest.cs](../tests/patterns/LoadTest.cs) | `examples/DemoProject/tests/DemoProject.Tests/LoadTests.cs` |
| **ComplexityRatchetTest** | Methods with `S3776` / `S1541` violations do not grow (baseline + ratchet) | [tests/patterns/ComplexityRatchetTest.cs](../tests/patterns/ComplexityRatchetTest.cs) | — |
| **AllocationBudgetTest** | `[HotPath]` method allocations do not exceed baseline + 10% | [tests/patterns/AllocationBudgetTest.cs](../tests/patterns/AllocationBudgetTest.cs) | `examples/DemoProject/tests/DemoProject.Tests/AllocationBudgetTests.cs` (green) / `examples/DemoProject.Traps/src/DemoProject.Traps/AllocationBudgetHotspot.cs` + `tests/DemoProject.Traps.Tests/AllocationBudgetTests.cs` (red) |
| **SpellcheckGuardTest** | No new typos appear in public symbols / docs | [tests/patterns/SpellcheckGuardTest.cs](../tests/patterns/SpellcheckGuardTest.cs) | — |
| **ReleaseReadinessTest** | Critical artifacts and runtime guardrails exist before release | [tests/patterns/ReleaseReadinessTest.cs](../tests/patterns/ReleaseReadinessTest.cs) | — |
| **MutationGuardTest** | Mutation score does not drop (Stryker.NET) | [tests/patterns/MutationGuardTest.cs](../tests/patterns/MutationGuardTest.cs) | — |
| **AnalyzerTests** | Positive / negative tests for custom Roslyn analyzers | [tests/patterns/AnalyzerTests.cs](../tests/patterns/AnalyzerTests.cs) | — |
| **PiiGuardTest** | `[SensitiveData]` + redaction guard | [tests/patterns/PiiGuardTest.cs](../tests/patterns/PiiGuardTest.cs) | — |
| **VersionAuditTest** | Audit of SDK/NuGet and frontend dependency versions | [tests/patterns/VersionAuditTest.cs](../tests/patterns/VersionAuditTest.cs) | — |
| **DuplicationGuardTest** | Business logic is not duplicated between services | [tests/patterns/DuplicationGuardTest.cs](../tests/patterns/DuplicationGuardTest.cs) | `examples/DemoProject/tests/DemoProject.Tests/DuplicationGuardTest.cs` |
| **DependencyDriftTest** | Cyclic dependencies between projects and layer drift | [tests/patterns/DependencyDriftTest.cs](../tests/patterns/DependencyDriftTest.cs) | `examples/DemoProject/tests/DemoProject.Tests/DependencyDriftTest.cs` |
| **EntityLeakTest** | Application interfaces do not return Domain Entity (ratchet) | [tests/patterns/EntityLeakTest.cs](../tests/patterns/EntityLeakTest.cs) | `examples/DemoProject/tests/DemoProject.Tests/EntityLeakTest.cs` |
| **StronglyTypedIds** | Domain entities must use strongly typed IDs, not raw Guid/string/int | [tests/patterns/StronglyTypedIds.cs](../tests/patterns/StronglyTypedIds.cs) | `examples/DemoProject/tests/DemoProject.Tests/StronglyTypedIds.cs` |
| **BUG_TEMPLATE** | Regression test format | [tests/conventions/BUG_TEMPLATE.cs](../tests/conventions/BUG_TEMPLATE.cs) | — |
| **TUnit_Guide** | Test conventions | [tests/conventions/TUnit_Guide.md](../tests/conventions/TUnit_Guide.md) | — |
| **AnalyzerDiagnostics** | Catalog of custom Roslyn analyzer diagnostics (SAE001-SAE009) | [tests/conventions/AnalyzerDiagnostics.md](../tests/conventions/AnalyzerDiagnostics.md) | `examples/DemoProject/src/DemoProject.Analyzers/` |
| **Traps Demo** | Intentionally broken code to demonstrate guardrails (9 failing tests) | — | `examples/DemoProject.Traps/` |
| **MinimalApi Demo** | Single-project MVP without Clean Architecture — naming, banned APIs, ratchet | — | `examples/DemoProject.MinimalApi/` |

---

## Skills (Audits)

Each standalone skill = an agent role. It usually contains `SKILL.md` (instructions) + `CHECKLIST.md` (checklist).
Exception: `templates/skills/skeptical-ai-bootstrap/` contains supporting templates; the executable bootstrap skill lives in `.agents/skills/skeptical-ai-bootstrap/`.

| Skill | When to run |
|-------|-------------|
| [code-review](../templates/skills/code-review/SKILL.md) | On every commit (pre-commit) / PR |
| [frontend-code-review](../templates/skills/frontend-code-review/SKILL.md) | On every commit (pre-commit) / PR with React/TS |
| [task-compliance](../templates/skills/task-compliance/SKILL.md) | On every PR |
| [security-audit](../templates/skills/security-audit/SKILL.md) | Once per sprint / on PR with Api/Infra |
| [dba-audit](../templates/skills/dba-audit/SKILL.md) | Once per sprint / on migrations (EF Core) |
| [dba-audit-dapper](../templates/skills/dba-audit-dapper/SKILL.md) | Once per sprint / on repository changes (Dapper / Raw SQL) |
| [performance-audit](../templates/skills/performance-audit/SKILL.md) | Before release / on suspicion |
| [api-design-audit](../templates/skills/api-design-audit/SKILL.md) | Once per sprint |
| [bot-audit](../templates/skills/bot-audit/SKILL.md) | Once per sprint |
| [business-risk-audit](../templates/skills/business-risk-audit/SKILL.md) | After a batch of domain audits / on a large refactor |
| [i18n-audit](../templates/skills/i18n-audit/SKILL.md) | Once per sprint |
| [version-audit](../templates/skills/version-audit/SKILL.md) | Once per sprint |
| [tech-debt-audit](../templates/skills/tech-debt-audit/SKILL.md) | Once per sprint / before quarterly planning |
| [test-audit](../templates/skills/test-audit/SKILL.md) | After 3-5 features / before release |
| [simplicity-audit](../templates/skills/simplicity-audit/SKILL.md) | Once per sprint / when code is hard to explain |
| [ux-audit](../templates/skills/ux-audit/SKILL.md) | During UI rework / before beta |
| [type-safety](../templates/skills/type-safety/SKILL.md) | On PR with Domain/DTO / when refactoring identifiers |
| [complexity-audit](../templates/skills/complexity-audit/SKILL.md) | Once per sprint / when technical debt grows |
| [allocation-budget-audit](../templates/skills/allocation-budget-audit/SKILL.md) | Before release / when hot paths change |
| [spellcheck-audit](../templates/skills/spellcheck-audit/SKILL.md) | Once per sprint / before public release |
| [release-readiness-audit](../templates/skills/release-readiness-audit/SKILL.md) | Before release / beta launch |
| [mutation-audit](../templates/skills/mutation-audit/SKILL.md) | Before release / once per sprint |
| [analyzer-tests-audit](../templates/skills/analyzer-tests-audit/SKILL.md) | When creating / updating Roslyn analyzers |
| [skeptical-ai-bootstrap](../.agents/skills/skeptical-ai-bootstrap/SKILL.md) | Once at project start |
| [adaptation-guide](../templates/skills/ADAPTATION.md) | Before first skill run |

### Artifact Grooming

| Skill | When to run |
|-------|-------------|
| [memory-hygiene](../templates/skills/memory-hygiene/SKILL.md) | Once per sprint or on agent change |
| [doc-hygiene](../templates/skills/doc-hygiene/SKILL.md) | Once per sprint or after refactoring |
| [backlog-hygiene](../templates/skills/backlog-hygiene/SKILL.md) | Once per sprint |

---

## Agent Traps (docs/traps/)

Read before implementation — each trap explains **why** a guardrail exists.

| Trap | Essence | Pattern solution |
|------|---------|------------------|
| [silent-breakdown](traps/silent-breakdown.md) | `AsNoTracking` in write-path → silent breakdown | [LoadTest.cs](../tests/patterns/LoadTest.cs) |
| [vibe-refactoring](traps/vibe-refactoring.md) | Agent removes "unnecessary" — breaks hot paths | [RatchetTest.cs](../tests/patterns/RatchetTest.cs) |
| [context-blindness](traps/context-blindness.md) | Agent does not see business context | [AGENTS.md](../rules/AGENTS_TEMPLATE.md) |
| [false-safety](traps/false-safety.md) | Green CI ≠ working code | [verify-tests.sh](../ci/scripts/verify-tests.sh) |
| [p50-vs-max](traps/p50-vs-max.md) | Average latency is good, tail is terrible | [LoadTest.cs](../tests/patterns/LoadTest.cs) |
| [agent-circles](traps/agent-circles.md) | Agents loop on one problem | [task-compliance](../templates/skills/task-compliance/SKILL.md) |
| [stale-stack](traps/stale-stack.md) | Agent uses outdated stack due to training cutoff | [VersionAuditTest.cs](../tests/patterns/VersionAuditTest.cs) |
| [log-leak](traps/log-leak.md) | PII leaks into logs | [PiiGuardTest.cs](../tests/patterns/PiiGuardTest.cs) |
| [code-duplication](traps/code-duplication.md) | Agent duplicates business logic instead of reuse | [DuplicationGuardTest.cs](../tests/patterns/DuplicationGuardTest.cs) |
| [dependency-drift](traps/dependency-drift.md) | +1 using/#include closes a cycle in the dependency graph | [DependencyDriftTest.cs](../tests/patterns/DependencyDriftTest.cs) |
| [over-engineering](traps/over-engineering.md) | Agent builds an architectural cathedral instead of a simple solution | [simplicity-audit](../templates/skills/simplicity-audit/SKILL.md) |
| [non-validating-tests](traps/non-validating-tests.md) | Test is green but cannot fail when behavior breaks | [test-audit](../templates/skills/test-audit/SKILL.md), [mutation-audit](../templates/skills/mutation-audit/SKILL.md) |

---

## Solutions and Patterns (docs/solutions/)

| Document | What's inside |
|----------|---------------|
| [architecture-tests.md](solutions/architecture-tests.md) | Detailed guide to NetArchTest.eNhancedEdition, ArchUnitNET and architecture boundaries |
| [roslyn-analyzers.md](solutions/roslyn-analyzers.md) | Roslyn-first guardrails for C#: IDE / `dotnet build` diagnostics instead of regex over `.cs` |
| [ai-patterns.md](solutions/ai-patterns.md) | 10 proven AI-driven development patterns |
| [human-audit-bridge.md](solutions/human-audit-bridge.md) | How to use AI checklists for manual human audit |
| [ARCHITECTURE-INVENTORY.md](../templates/skills/skeptical-ai-bootstrap/ARCHITECTURE-INVENTORY.md) | Template for recording current architecture before implementing guardrails |
| [DECISION-GUARDS.md](../templates/skills/skeptical-ai-bootstrap/DECISION-GUARDS.md) | Template for intentional deviation registry (`PERF-###`, `DB-###`, `AUD-###`) |

---

## Case Studies (docs/case-studies/)

| Document | What's inside |
|----------|---------------|
| [small-project-minimal-api.md](case-studies/small-project-minimal-api.md) | Small project (single-project MVP): risk profile, selected/rejected controls, false positives, cost |
| [production-like-layered-service.md](case-studies/production-like-layered-service.md) | Production-like layered service: full level set, removed/rejected guardrails, maintenance cost |

Active plans: [SELF-CHECKING-TESTS-WORKSTREAM.md](SELF-CHECKING-TESTS-WORKSTREAM.md) (self-checking tests; SV-006 in progress).

---

## Agent Integrations (docs/agents/)

> **⚠️ Agents:** Read [BOOTSTRAP-PROTOCOL.md](agents/BOOTSTRAP-PROTOCOL.md) before starting work.  
> It defines the boundary between "methodology repository" and "target project".

| Agent | File | Configuration format |
|-------|------|----------------------|
| Kimi Code CLI | [KIMI.md](agents/KIMI.md) | `.kimi/skills/{name}/SKILL.md` |
| Claude Code | [CLAUDE-CODE.md](agents/CLAUDE-CODE.md) | `.claude/CLAUDE.md` + commands |
| Cursor | [CURSOR.md](agents/CURSOR.md) | `.cursorrules` + `.cursor/rules/` |
| Codex (OpenAI) | [CODEX.md](agents/CODEX.md) | `AGENTS.md` + `~/.codex/config.toml` |
| OpenCode | [OPENCODE.md](agents/OPENCODE.md) | `.opencode/instructions.md` |
| Bootstrap Protocol | [BOOTSTRAP-PROTOCOL.md](agents/BOOTSTRAP-PROTOCOL.md) | Agent behavior rules during onboarding |
| Comparison | [README.md](agents/README.md) | Comparison table of all agents |

---

## CI / CD

| Artifact | Purpose |
|----------|---------|
| [ci/github-actions/safe-ci.yml](../ci/github-actions/safe-ci.yml) | Workflow template: build + test + verify-tests |
| [ci/scripts/run-tests.sh](../ci/scripts/run-tests.sh) | Automatically finds and runs all test projects via `dotnet run --project` |
| [ci/scripts/verify-tests.sh](../ci/scripts/verify-tests.sh) | Checks that `dotnet run` actually executed tests (not 0 ran) |
| [.github/workflows/demo-project-ci.yml](../.github/workflows/demo-project-ci.yml) | CI of this repository — builds DemoProject and DemoProject.MinimalApi |
| `traps-guardrails` job in `demo-project-ci.yml` | Ensures intentionally broken tests in DemoProject.Traps actually fail (guardrails are working) |

---

## Project Rules

| File | What's inside |
|------|---------------|
| [rules/AGENTS_TEMPLATE.md](../rules/AGENTS_TEMPLATE.md) | Base constitution for AI agents: tests, dates, cache, commits (universal) |
| [rules/AGENTS_TEMPLATE.efcore.md](../rules/AGENTS_TEMPLATE.efcore.md) | Add-on: EF Core-specific rules (read/write path, `AsNoTracking`) |
| [rules/AGENTS_TEMPLATE.dapper.md](../rules/AGENTS_TEMPLATE.dapper.md) | Add-on: Dapper / Raw SQL-specific rules (parameterization, timeouts) |
| [rules/CONVENTIONS.md](../rules/CONVENTIONS.md) | Test naming, workflow, CI guardrails |
| [BannedSymbols.txt](../examples/DemoProject/BannedSymbols.txt) | Compile-time guard: banned APIs (BannedApiAnalyzers RS0030) |

---

## How to Update This Map

When adding a new artifact:
1. Add a row to the corresponding table
2. Provide a link to the pattern/solution
3. If it's a new pyramid layer — update [PYRAMID.md](../PYRAMID.md)
