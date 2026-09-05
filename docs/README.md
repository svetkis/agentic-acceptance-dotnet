# Knowledge Map

> Unified table of contents for all repository artifacts.  
> If you are here for the first time — start with [GLOSSARY.md](../GLOSSARY.md), then return here.
>

---

## Quick Start by Role

| I am a ... | Where to start |
|------------|----------------|
| **Newcomer** to agentic development | [GLOSSARY.md](../GLOSSARY.md) (terms) → [README.md "How it works"](../README.md#how-it-works) (levels model) → this map → `examples/DemoProject/` → [ONBOARDING.md](ONBOARDING.md) (apply to your project) |
| **Tech Lead** implementing guardrails | [ONBOARDING.md](ONBOARDING.md) → [.agents/skills/acceptance-bootstrap/SKILL.md](../.agents/skills/acceptance-bootstrap/SKILL.md) → [ADAPTATION.md](../templates/skills/ADAPTATION.md) → [EVIDENCE.md](EVIDENCE.md) (what each level caught) |
| **Developer** looking for a test pattern | [tests/patterns/](#test-patterns) → copy into your project |
| **Implementing Agentic Acceptance from scratch** | [ONBOARDING.md](ONBOARDING.md) → step-by-step guide with checkpoints |
| **Auditor** preparing for an audit | [templates/skills/](#skills-audits) → take CHECKLIST.md → [human-audit-bridge.md](solutions/human-audit-bridge.md) for manual walkthrough |
| **Contributor** | [CONTRIBUTING.md](../CONTRIBUTING.md) → "What can be added" section |

---

## Control Levels (Engineering Assurance Levels)

The canonical classifier is the **Engineering Assurance Levels** model in
[README.md](../README.md#how-it-works). Evidence behind it — what each level caught
in the observed case, ROI, and the risk-justification principle — lives in
[EVIDENCE.md](EVIDENCE.md).

| Level / process | What it is | Key artifacts | Extended reading |
|-----------------|------------|---------------|------------------|
| **Control Foundation** | Instructions for the agent before code: constitution + Decision Guards | [AGENTS_TEMPLATE.md](../rules/AGENTS_TEMPLATE.md) (+ [efcore](../rules/AGENTS_TEMPLATE.efcore.md) / [dapper](../rules/AGENTS_TEMPLATE.dapper.md) add-ons), [DECISION-GUARDS.md](../templates/skills/acceptance-bootstrap/DECISION-GUARDS.md) | [ONBOARDING.md Step 3](ONBOARDING.md#step-3-write-the-constitution-control-foundation) |
| **1. Change Checks** | Fast feedback from compiler, types, analyzers | `.editorconfig`, `Directory.Build.props`, banned APIs, `DemoProject.Analyzers` (custom Roslyn analyzers) | [architecture-tests.md §Roslyn](solutions/architecture-tests.md#11-roslyn-analyzers-as-the-default-for-c) |
| **2. Behavior Checks** | Tests, architecture rules, ratchets, pre-commit review, scope gates | [tests/patterns/](#test-patterns), [code-review](../templates/skills/code-review/SKILL.md), [task-compliance](../templates/skills/task-compliance/SKILL.md) | [ONBOARDING.md Steps 5–7](ONBOARDING.md#step-5-implement-behavior-checks-architecture-rules) |
| **3. System Checks** | Smoke, E2E, load — the system works as a whole | [LoadTest.cs](../tests/patterns/LoadTest.cs), [SnapshotTest.cs](../tests/patterns/SnapshotTest.cs) | [ONBOARDING.md Steps 8–9, 11](ONBOARDING.md#step-8-implement-system-checks-smoke-tests) |
| **4. Reality Checks** | Deep audits on schedule: security, DB, performance, business risk | [templates/skills/](#skills-audits) | [ONBOARDING.md Step 10](ONBOARDING.md#step-10-implement-reality-checks-audits) |
| **Engineering Governance** *(process)* | Final human validation, residual risk acceptance, release decision | [human-audit-bridge.md](solutions/human-audit-bridge.md) | [EVIDENCE.md](EVIDENCE.md) |
| **Control Maintenance** *(process)* | Keeping instructions, memory, baselines, and guardrails themselves up to date | [memory-hygiene](../templates/skills/memory-hygiene/SKILL.md), [doc-hygiene](../templates/skills/doc-hygiene/SKILL.md), [backlog-hygiene](../templates/skills/backlog-hygiene/SKILL.md) | [EVIDENCE.md §Grooming ROI](EVIDENCE.md#the-invisible-decay-paradox) |

---

## Test Patterns

All templates are `copy-paste friendly`. Each contains comments `// TRAP:` and `// GUARDRAIL:`.

| Pattern | Purpose | Location | Working example in DemoProject |
|---------|---------|----------|-------------------------------|
| **ArchitectureRules** | Universal layer dependency check (NetArchTest) | [tests/patterns/ArchitectureRules.cs](../tests/patterns/ArchitectureRules.cs) | `examples/DemoProject/tests/DemoProject.Tests/ArchitectureRules.cs` |
| **EfCoreGuardRules** | EF Core-specific guardrails: `FindAsync`, `Include`, `AsNoTracking` | [tests/patterns/EfCoreGuardRules.cs](../tests/patterns/EfCoreGuardRules.cs) | `examples/DemoProject/tests/DemoProject.Tests/EfCoreGuardRules.cs` |
| **DapperGuardRules** | Dapper / Raw SQL guardrails: parameterization, injections, timeouts | [tests/patterns/DapperGuardRules.cs](../tests/patterns/DapperGuardRules.cs) | — |
| **ArchUnitNetSliceTest** | Cyclic dependencies between slices (ArchUnitNET) | [tests/patterns/ArchUnitNetSliceTest.cs](../tests/patterns/ArchUnitNetSliceTest.cs) | `examples/DemoProject/tests/DemoProject.Traps.Tests/ArchUnitNetSliceTest.cs` |
| **RatchetTest** | Public types and tests did not decrease | [tests/patterns/RatchetTest.cs](../tests/patterns/RatchetTest.cs) | `examples/DemoProject/tests/DemoProject.Tests/RatchetTests.cs` |
| **SnapshotTest** | JSON serialization contract, OpenAPI | [tests/patterns/SnapshotTest.cs](../tests/patterns/SnapshotTest.cs) | `examples/DemoProject/tests/DemoProject.Tests/SnapshotTests.cs` |
| **PropertyBasedTest** | Invariants over generated inputs (FsCheck) instead of hand-picked examples | [tests/patterns/PropertyBasedTest.cs](../tests/patterns/PropertyBasedTest.cs) | — |
| **LoadTest** | Silent breakdown under load: read optimizations that break write path | [tests/patterns/LoadTest.cs](../tests/patterns/LoadTest.cs) | `examples/DemoProject/tests/DemoProject.Tests/LoadTests.cs` |
| **ProductionConfigurationTest** | Silent production config breakage: Dockerfile env vars, GC limits, deploy manifests (`BUG_CONFIG###`) | [tests/patterns/ProductionConfigurationTest.cs](../tests/patterns/ProductionConfigurationTest.cs) | — |
| **ComplexityRatchetTest** | Methods with `S3776` / `S1541` violations do not grow (baseline + ratchet) | [tests/patterns/ComplexityRatchetTest.cs](../tests/patterns/ComplexityRatchetTest.cs) | — |
| **AllocationBudgetTest** | `[HotPath]` method allocations do not exceed baseline + 10% | [tests/patterns/AllocationBudgetTest.cs](../tests/patterns/AllocationBudgetTest.cs) | `examples/DemoProject/tests/DemoProject.Tests/AllocationBudgetTests.cs` (green) / `examples/DemoProject/traps-src/DemoProject.Traps/AllocationBudgetHotspot.cs` + `tests/DemoProject.Traps.Tests/AllocationBudgetTests.cs` (red) |
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
| **DecisionGuardLinkTest** | Decision registry does not rot: unique IDs, code links resolve, ID present in the linked file | [tests/patterns/DecisionGuardLinkTest.cs](../tests/patterns/DecisionGuardLinkTest.cs) | — |
| **BUG_TEMPLATE** | Regression test format | [tests/conventions/BUG_TEMPLATE.cs](../tests/conventions/BUG_TEMPLATE.cs) | — |
| **TUnit_Guide** | Test conventions | [tests/conventions/TUnit_Guide.md](../tests/conventions/TUnit_Guide.md) | — |
| **AnalyzerDiagnostics** | Catalog of custom Roslyn analyzer diagnostics (SAE001-SAE009) | [tests/conventions/AnalyzerDiagnostics.md](../tests/conventions/AnalyzerDiagnostics.md) | `examples/DemoProject/src/DemoProject.Analyzers/` |
| **Traps Demo** | Intentionally broken code to demonstrate guardrails (see [`TRAPS.md`](../examples/DemoProject/TRAPS.md) for the current failing-test count) | — | `examples/DemoProject/TRAPS.md` |
| **MinimalApi Demo** | Single-project MVP without Clean Architecture — naming, banned APIs, ratchet | — | `examples/DemoProject.MinimalApi/` |

---

## Skills (Audits)

Each standalone skill = an agent role. It usually contains `SKILL.md` (instructions) + `CHECKLIST.md` (checklist).
Exception: `templates/skills/acceptance-bootstrap/` contains supporting templates; the executable bootstrap skill lives in `.agents/skills/acceptance-bootstrap/`.

| Skill | When to run |
|-------|-------------|
| [code-review](../templates/skills/code-review/SKILL.md) | On every commit (pre-commit) / PR |
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
| [external-contract-verification](../templates/skills/external-contract-verification/SKILL.md) | Before implementing integrations / on PR with webhooks, provider DTOs, signature checks |
| [load-test-ops](../templates/skills/load-test-ops/SKILL.md) | Before release / after infrastructure changes (load runs and trend comparison) |
| [perf-test-authoring](../templates/skills/perf-test-authoring/SKILL.md) | When adding / changing hot paths and their budget tests |
| [guardrails-review](../templates/skills/guardrails-review/SKILL.md) | On any change to guardrail/verifier/gate code (fail-closed adversarial audit) |
| [acceptance-bootstrap](../.agents/skills/acceptance-bootstrap/SKILL.md) | Once at project start |
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
| [silent-breakdown](traps/testing.md#silent-breakdown) | `AsNoTracking` in write-path → silent breakdown | [LoadTest.cs](../tests/patterns/LoadTest.cs) |
| [vibe-refactoring](traps/agent-behavior.md#vibe-refactoring) | Agent removes "unnecessary" — breaks hot paths | [RatchetTest.cs](../tests/patterns/RatchetTest.cs) |
| [context-blindness](traps/agent-behavior.md#context-blindness) | Agent does not see business context | [AGENTS.md](../rules/AGENTS_TEMPLATE.md) |
| [false-safety](traps/testing.md#false-safety) | Green CI ≠ working code | [run-and-verify-tests.sh](../ci/scripts/run-and-verify-tests.sh) |
| [p50-vs-max](traps/runtime.md#p50-vs-max) | Average latency is good, tail is terrible | [LoadTest.cs](../tests/patterns/LoadTest.cs) |
| [agent-circles](traps/agent-behavior.md#agent-circles) | Agents loop on one problem | [task-compliance](../templates/skills/task-compliance/SKILL.md) |
| [stale-stack](traps/agent-behavior.md#stale-stack) | Agent uses outdated stack due to training cutoff | [VersionAuditTest.cs](../tests/patterns/VersionAuditTest.cs) |
| [log-leak](traps/runtime.md#log-leak) | PII leaks into logs | [PiiGuardTest.cs](../tests/patterns/PiiGuardTest.cs) |
| [code-duplication](traps/code-quality.md#code-duplication) | Agent duplicates business logic instead of reuse | [DuplicationGuardTest.cs](../tests/patterns/DuplicationGuardTest.cs) |
| [dependency-drift](traps/code-quality.md#dependency-drift) | +1 using/#include closes a cycle in the dependency graph | [DependencyDriftTest.cs](../tests/patterns/DependencyDriftTest.cs) |
| [over-engineering](traps/agent-behavior.md#over-engineering) | Agent builds an architectural cathedral instead of a simple solution | [simplicity-audit](../templates/skills/simplicity-audit/SKILL.md) |
| [non-validating-tests](traps/testing.md#non-validating-tests) | Test is green but cannot fail when behavior breaks | [test-audit](../templates/skills/test-audit/SKILL.md), [mutation-audit](../templates/skills/mutation-audit/SKILL.md) |
| [false-green-gate](traps/testing.md#false-green-gate) | Gate over an external source answers "clean" when the source stopped serving data | canary + tri-state exit codes in audit scripts |

---

## Solutions and Patterns (docs/solutions/)

| Document | What's inside |
|----------|---------------|
| [architecture-tests.md](solutions/architecture-tests.md) | Detailed guide to NetArchTest.eNhancedEdition, ArchUnitNET and architecture boundaries |
| [architecture-tests.md §Roslyn](solutions/architecture-tests.md#11-roslyn-analyzers-as-the-default-for-c) | Roslyn-first guardrails for C#: IDE / `dotnet build` diagnostics instead of regex over `.cs` |
| [ai-patterns.md](solutions/ai-patterns.md) | 10 proven AI-driven development patterns |
| [human-audit-bridge.md](solutions/human-audit-bridge.md) | How to use AI checklists for manual human audit |
| [EVIDENCE.md](EVIDENCE.md) | Effectiveness metrics and ROI of the control levels (observed case), risk-justification principle for guardrails |
| [ARCHITECTURE-INVENTORY.md](../templates/skills/acceptance-bootstrap/ARCHITECTURE-INVENTORY.md) | Template for recording current architecture before implementing guardrails |
| [DECISION-GUARDS.md](../templates/skills/acceptance-bootstrap/DECISION-GUARDS.md) | Template for intentional deviation registry (`PERF-###`, `DB-###`, `AUD-###`) |

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
| Frontier agents (Kimi, Claude Code, Codex) | [FRONTIER-AGENTS.md](agents/FRONTIER-AGENTS.md) | skills + constitution, goal-driven |
| Step-by-step agents (Cursor, OpenCode) | [STEP-BY-STEP-AGENTS.md](agents/STEP-BY-STEP-AGENTS.md) | explicit steps + paste-ready prompts |
| Bootstrap Protocol | [BOOTSTRAP-PROTOCOL.md](agents/BOOTSTRAP-PROTOCOL.md) | Agent behavior rules during onboarding |
| Comparison | [README.md](agents/README.md) | Comparison table of all agents |

---

## CI / CD

| Artifact | Purpose |
|----------|---------|
| [ci/github-actions/safe-ci.yml](../ci/github-actions/safe-ci.yml) | Workflow template: build + test + verification |
| [ci/lefthook.yml](../ci/lefthook.yml) | Template for local pre-commit hooks (lefthook): format + static checks on staged files — enforcement the agent cannot forget |
| [ci/scripts/run-and-verify-tests.sh](../ci/scripts/run-and-verify-tests.sh) | Finds and runs all test projects via `dotnet run --project`, then verifies that tests actually ran (not 0 ran) |
| [.github/workflows/demo-project-ci.yml](../.github/workflows/demo-project-ci.yml) | CI of this repository — builds DemoProject and DemoProject.MinimalApi |
| `traps-guardrails` job in `demo-project-ci.yml` | Ensures intentionally broken tests in DemoProject.Traps actually fail (guardrails are working) |

---

## Project Rules

| File | What's inside |
|------|---------------|
| [rules/AGENTS_TEMPLATE.md](../rules/AGENTS_TEMPLATE.md) | Base constitution for AI agents: only what every commit touches — tests, dates, style, decision guards (universal) |
| [rules/AGENTS_TEMPLATE.efcore.md](../rules/AGENTS_TEMPLATE.efcore.md) | Add-on: EF Core-specific rules (read/write path, `AsNoTracking`) |
| [rules/AGENTS_TEMPLATE.dapper.md](../rules/AGENTS_TEMPLATE.dapper.md) | Add-on: Dapper / Raw SQL-specific rules (parameterization, timeouts) |
| [rules/AGENTS_TEMPLATE.addons.md](../rules/AGENTS_TEMPLATE.addons.md) | Add-on: optional sections (caching, hot path, complexity, spellcheck, mutation, analyzer tests) — copy only what matches the stack |
| [rules/CONVENTIONS.md](../rules/CONVENTIONS.md) | Test naming, workflow, CI guardrails |
| [BannedSymbols.txt](../examples/DemoProject/BannedSymbols.txt) | Compile-time guard: banned APIs (BannedApiAnalyzers RS0030) |

---

## How to Update This Map

When adding a new artifact:
1. Add a row to the corresponding table
2. Provide a link to the pattern/solution
3. If it's a new control level — update the Engineering Assurance Levels model in the root [README.md](../README.md) (the canonical classifier) and, if you have effectiveness data, [EVIDENCE.md](EVIDENCE.md)
