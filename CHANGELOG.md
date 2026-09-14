# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- New trap `docs/traps/agent-behavior.md#surface-leak` + solution `docs/solutions/public-api-surface.md` — accidental public API surface changes: agent widens visibility "for convenience", renames or removes public symbols; repo tests stay green, contract consumers break at their own build. Defense in three layers: internal-by-default (fail-closed visibility), `InternalsVisibleTo` as a narrow allowlist (removes the "public for tests" excuse), `Microsoft.CodeAnalysis.PublicApiAnalyzers` with RS0016/RS0017 escalated to errors (diagnostic IDs and the `*REMOVED*` removal flow verified against the official roslyn docs; RS0026/RS0027 optional-parameter overload traps, RS0051–RS0061 internal-API tracking noted as opt-in). The `PublicAPI.Unshipped.txt` diff is the contract-change report for review — one file instead of a 2000-line diff. Working demo in DemoProject: `src/DemoProject.Domain` wired green (surface bootstrapped into Shipped, Unshipped empty as the report file) + red build-time trap `traps-src/DemoProject.Traps.PublicApi` (leaked helper → 6×RS0016, renamed+deleted methods → 4×RS0017), outside `DemoProject.sln` on purpose and verified by a new step in the `traps-guardrails` CI job. Field findings folded into the solution doc: bootstrap via `dotnet format analyzers --diagnostics RS0016` (code fix unreliable outside VS; line format has no canonical write-up — accessor-per-line properties, `Member = value -> EnumType` enums, dual record-struct ctors, trailing `!` nullability), and RS0017 severity must be escalated via csproj `<WarningsAsErrors>` because `.editorconfig` diagnostics do not reach additional-file locations. Applicability: libraries and shared contracts, not single-app deployments. Registered in the knowledge map (`docs/README.md`); Level-1 rows of README / README.ru updated; TRAPS.md gained the build-time trap section; methodology wiring: ONBOARDING.md Step 4 item, "Public API Surface" constitution add-on in `rules/AGENTS_TEMPLATE.addons.md`, review-signal block in `templates/skills/code-review/CHECKLIST.md` (unshipped diff / `*REMOVED*` / new IVT entry → contract-change flag), T9 + P10 nodes and edges in `docs/relationships.mmd`.
- New trap `docs/traps/code-quality.md#silent-culture-footguns` — culture-dependent string/parsing defaults and timeout-less regex (ReDoS) that look idiomatic in review; defense: Meziantou.Analyzer (MA0002, MA0006, MA0009, MA0011, MA0074, verified against official rule docs) with selective-enable guidance + `BannedSymbols.txt` blacklist. Registered in the knowledge map (`docs/README.md`). Also: full `BannedSymbols.txt` symbol syntax reference (`T:`/`M:`/`#ctor`/`P:`/`F:`/`E:`, generic backtick arity) added to the Dependency Drift trap; `BannedApiAnalyzers` limitation cross-links layer rules to `LayerGuardAnalyzer` (SAE010-SAE012).
- `LayerGuardAnalyzer` (SAE010–SAE012) in `DemoProject.Analyzers` + `[Query]` attribute in `DemoProject.Domain` — layer violations caught at compile time, not only by NetArchTest: DbContext outside Infrastructure (SAE010, Error), domain entity leaked into the API layer (SAE011, Error), `[Query]` method that mutates state or calls SaveChanges/Add/Remove/Update (SAE012, Warning). Positive/negative analyzer tests added to `DemoProject.Tests` (61/61 green). Catalog: `tests/conventions/AnalyzerDiagnostics.md`.
- `docs/solutions/nuget-audit-as-error.md` — NuGet audit warnings (NU1901–NU1905: vulnerable/deprecated packages) escalated to build errors: `NuGetAuditMode=all` for transitive dependencies, `TreatWarningsAsErrors` composition in CI, suppression lifecycle with owner+expiry, honest Level-1 vs Level-4 boundary. `safe-ci.yml` Restore step now passes `/p:NuGetAuditMode=all /p:NuGetAuditLevel=low`; Level-1 rows of README / README.ru updated.
- `tests/patterns/VerifySnapshotTest.cs` + working adaptation in `DemoProject.Tests` — snapshot contract tests via Verify.TUnit (readable diffs, auto-scrub of `Guid`/`DateTime`). Documented traps: blind snapshot acceptance, TUnit/Verify version lockstep (`MissingFieldException` at runtime), Verify 33+ SponsorCheck license gate (SC021) — demo pinned to Verify.TUnit 32.0.1 + TUnit 1.66.27. Demo suite: 52/52 green.
- `tests/patterns/StormPetrelSnapshotTest.cs` + working adaptation in `DemoProject.Tests` — Storm Petrel baseline-in-code snapshot testing verified working with TUnit end-to-end (TUnit 1.66.27 / Generator 3.0.1). Recipe: `SCAND_STORM_PETREL_GENERATOR_CONFIG` env-var attribute declaration + `[assembly: TUnit.Core.ReflectionMode]` (TUnit's source generator cannot see other generators' test classes). Traps documented: copy not generated without env var, undiscovered without Reflection mode, `System.Globalization` using needed after rewrite, `*.backup*` artifacts gitignored. Upstream default-support PR: [storm-petrel#6](https://github.com/Scandltd/storm-petrel/pull/6) (issue [#5](https://github.com/Scandltd/storm-petrel/issues/5)). Demo suite green.
- Field report folded back into the Storm Petrel pattern (large TUnit suite, ~2200 tests): `Array.Empty<string>()` in a baseline breaks the generated copy (use `new string[0]`); a variable reused as `expected` twice in one method crashed the generator with CS8785 — two duplicate-key crash fixes contributed upstream in PR #6 (`VarHelper.GetExpectedIdentifierToInfo`, `AbstractValueRewriter.WithInvocationPathHandling`), upstream tests 81/81 green.
- `tests/patterns/DecisionGuardLinkTest.cs` — test pattern: decision registry verified in both directions (unique IDs, code links resolve, ID present in the linked file); generalized from a production adaptation.
- `docs/traps/testing.md#false-green-gate` — "False-Green Gate" trap: a gate over an external data source answers "clean" when the source stopped serving data; solution — canary (known-bad input) + tri-state exit codes, fail closed on "inconclusive".
- `rules/AGENTS_TEMPLATE.md` — Decision Guards section now prescribes the bidirectional registry↔code link check (`DecisionGuardLinkTest.cs`).
- `templates/skills/external-contract-verification/`, `templates/skills/load-test-ops/`, `templates/skills/perf-test-authoring/`, `templates/skills/guardrails-review/` — four skills restored from project-specific adaptations that survived in global agent skill directories after the original disk was lost; generalized to stack-neutral form.
- `docs/agents/STEP-BY-STEP-AGENTS.md` — Cursor IDE integration (`.cursorrules`, `.cursor/rules/`, Composer mode).
- `docs/obstacles/context-rot.md` — "Context Rot" obstacle and compensation via stateless guardrails.
- `docs/traps/agent-behavior.md#stale-stack` — "Stale Stack" trap: the agent uses a preview SDK or outdated NuGet packages due to its training cutoff.
- `docs/traps/runtime.md#log-leak` — "Log Leak" trap: the agent logs email, phone, password.
- `docs/relationships.mmd` — graph of guardrails, traps, and obstacles relationships (Mermaid).
- `templates/skills/version-audit/` — new skill for auditing stack currency (SDK, NuGet, frontend, CI actions).
- `tests/patterns/VersionAuditTest.cs` — test pattern: regex scanning of `global.json`, `*.csproj`, `package.json` for preview flags and version mismatches.
- `tests/patterns/PiiGuardTest.cs` — test pattern: `[SensitiveData]` attribute + ratchet + regex scanning of Log* calls for PII.
- `tests/patterns/ComplexityRatchetTest.cs` — test pattern: ratchet on the growth of `S3776`/`S1541` violations.
- `tests/patterns/AllocationBudgetTest.cs` — test pattern: allocations of `[HotPath]` methods stay within baseline + 10%.
- `tests/patterns/SpellcheckGuardTest.cs` — test pattern: CSpell + baseline for public symbols and documentation.
- `tests/patterns/ReleaseReadinessTest.cs` — test pattern: check for required artifacts before release.
- `tests/patterns/MutationGuardTest.cs` — test pattern: mutation score must not decrease (Stryker.NET).
- `tests/patterns/AnalyzerTests.cs` — test pattern: positive/negative tests for custom Roslyn analyzers.
- `templates/skills/complexity-audit/` — skill for auditing cognitive / cyclomatic complexity.
- `templates/skills/allocation-budget-audit/` — skill for auditing hot path allocations.
- `templates/skills/spellcheck-audit/` — skill for auditing spelling of public symbols and documentation.
- `templates/skills/release-readiness-audit/` — release readiness audit skill.
- `templates/skills/mutation-audit/` — mutation testing audit skill.
- `templates/skills/analyzer-tests-audit/` — skill for auditing tests of custom analyzers.
- `examples/DemoProject/src/DemoProject.Analyzers/HotPathAnalyzer.cs` — Roslyn analyzer SAE003/004/005 for `[HotPath]` methods.
- `docs/solutions/ai-patterns.md` — pattern #9: Attribute-driven PII redaction (compile-time + runtime).
- `rules/AGENTS_TEMPLATE.md` — translated to English, added Semantic Anchors, Permission to Push Back, Context Markers.
- `docs/TRANSLATION_PLAN.md` — plan for translating documentation into two languages.
- `LICENSE` — MIT license.
- `.gitignore` — standard .NET + JetBrains Rider + Serena ignore rules.
- `global.json` — pins .NET 10 SDK with `latestFeature` roll-forward.
- `CONTRIBUTING.md` — bilingual (RU/EN) contribution guide with pre-PR checklist.
- `examples/DemoProject/` — working .NET 10 solution demonstrating all patterns:
  - Clean Architecture (Domain → Application → Infrastructure)
  - NetArchTest layer dependency checks
  - Ratchet tests for test inventory (count must not decrease)
  - Snapshot tests for JSON contracts
  - NBomber load tests (read + write mix)
  - TUnit 1.x with `dotnet run --project`
- `README.en.md` (now the English `README.md`) — full English translation of the README.
- `.github/workflows/demo-project-ci.yml` — CI that builds DemoProject and runs all tests (the current count is tracked in `examples/DemoProject/TRAPS.md`).
- `SECURITY.md` — security policy and responsible disclosure process.
- `CODE_OF_CONDUCT.md` — Contributor Covenant Code of Conduct.
- `.github/ISSUE_TEMPLATE/` — issue templates for bug reports, feature requests, and proposals.
- `.github/pull_request_template.md` — pull request template with pre-PR checklist.

### Changed
- DateTime rule split into invariant + time source: `code-review` SKILL.md distinguishes "all backend dates are UTC instants" (the invariant) from "HOW the time is obtained" (the source) — direct `DateTime.Now`/`DateTime.UtcNow` in domain/application code is flagged for an injectable source (`TimeProvider.GetUtcNow()`), direct calls stay acceptable in the composition root or by documented decision; DB-side defaults (`now()`, `GETUTCDATE()`) satisfy the invariant. Synced everywhere the rule lives: `code-review/CHECKLIST.md` (two matching items) and the constitution (`rules/AGENTS_TEMPLATE.md` §Dates — previously prescribed bare `DateTime.UtcNow` without the source qualification, which the review skill would now flag).
- Clarity pass: canonical newcomer path stated identically in README (EN/RU) and the knowledge map (GLOSSARY → README "How it works" → map → ONBOARDING); README structure tree no longer enumerates individual skills (drift-prone; catalog lives in docs/README.md); GLOSSARY row added to both README navigation tables; "skill" defined on first use in README.
- Sync rules added: CONTRIBUTING pre-PR checklist now requires README.ru.md updates in the same commit as README.md and cross-checking the `.agents/`/`templates/` bootstrap split; SKILL-CONTRACT documents the same-pair review rule for the bootstrap bundle.
- `code-review` skill frontmatter normalized to the contract (`name` + `description` only; trigger info folded into `description`); INSTALL.md bootstrap-copy instruction fixed (executable SKILL.md comes from `.agents/`, not `templates/`) and mode durations aligned with ONBOARDING.md as canonical.
- Migration leftovers: orphaned "Outer Loop section below" pointer, "Acceptance" step name in AGENTS.md, "Decision Guards (ADR)" terminology conflict, "layer 0" in the bootstrap example report, GLOSSARY legacy-entry link.
- `PYRAMID.md` retired: the legacy pyramid document is removed, completing the migration to the Engineering Assurance Levels model. Unique quantitative material (what each level caught in the observed case, ROI tables, invisible-layer/invisible-decay paradoxes, evolution timeline, risk-justification principle) moved to `docs/EVIDENCE.md`; the "4 rules for Monday" moved to `docs/ONBOARDING.md` §Operating Rhythm; all references repointed (README, GLOSSARY, AGENTS.md, knowledge map, CONTRIBUTING, PR template, doc-hygiene scope).
- Consistency pass over the repo (review findings): AGENTS.md onboarding order now canonical per `docs/ONBOARDING.md` (agent setup last); the canonical control model is the Engineering Assurance Levels table in `README.md` (CONTRIBUTING and the knowledge map updated accordingly, `PYRAMID.md` marked as an archived reference and removed from the newcomer path); the legacy pyramid taxonomy replaced by level names throughout `docs/ONBOARDING.md`, the knowledge map's control table, `architecture-tests.md`, `AnalyzerDiagnostics.md`, and the bootstrap example report; removed talk remnants, the empty RU-README frontend note, the duplicate Bootstrap Protocol row, and stale `verify-tests.sh` / hardcoded test-count references; RU README resynced with EN (task-compliance level, navigation rows).
- `docs/solutions/roslyn-analyzers.md` merged into `architecture-tests.md` §11 (analyzer process, csproj hookup, repository rule preserved); references repointed.
- README (EN/RU) Navigation tables trimmed to core entries with a pointer to the full knowledge map in `docs/README.md` (single source for the map).
- `[ADAPT]` markers added for DemoProject-specific rules in `rules/AGENTS_TEMPLATE.md` (`[HotPath]`/allocation budget) and for ORM add-on template paths; a "Path profile" adaptation note added to 11 skills with hardcoded `src/*/Api`-style paths, documented in `templates/skills/ADAPTATION.md`.
- `tests/patterns/README.md` added declaring templates canonical and demo test files independent adaptations; provenance comments added where missing.
- Methodology revision 2026-07-14 (METH-001…METH-024): Engineering Assurance Levels model, normative glossary, unified skill contract (`SKILL-CONTRACT.md` + schema-lint), heuristic audits de-absolutized, single safe onboarding path, evidence model for quantitative claims, repo-quality CI checks, case studies. Plan document removed after full execution; outcomes live in the artifacts and git history.
- Self-checking tests guardrails (SV-001…SV-005 done; SV-006 in progress): constitution rule in `rules/AGENTS_TEMPLATE.md`, trap `docs/traps/testing.md#non-validating-tests`, Test Validity section in test-audit, mutation-audit cross-link, custom Roslyn analyzers SAE006-SAE009 (`DemoProject.Analyzers`) with positive/negative unit tests, (SV-005 was covered by `frontend-code-review`, since removed as out of the .NET stack scope). Remaining SV-006 blockers tracked in `docs/SELF-CHECKING-TESTS-WORKSTREAM.md`.
- `README.md` — restructured with language badges, DemoProject section, and links to CONTRIBUTING/LICENSE.
- `AGENTS.md` — updated navigation table with `examples/DemoProject/`, `CONTRIBUTING.md`, and `LICENSE`.
- `tests/conventions/TUnit_Guide.md` — added note about TUnit 1.x auto-generated entry point (no `Program.cs` required).
- `README.md` and `README.en.md` (now the English `README.md`) — added badges (.NET 10, License, CI), author section, and community contacts.

## [0.1.0] - 2026-05-29

### Added
- Initial release of defensive artifacts for .NET agentic engineering.
- 5-layer inner-loop pyramid documented in `PYRAMID.md`.
- `rules/AGENTS_TEMPLATE.md` — EF Core, PostgreSQL, API/DTO, caching, and commit conventions.
- `rules/CONVENTIONS.md` — naming, workflow, and CI guardrails.
- `templates/skills/` — 8 agent roles: code-review, task-compliance, security-audit, dba-audit, ux-audit, performance-audit, i18n-audit, skeptical-ai-bootstrap.
- `tests/patterns/` — template tests: ArchitectureRules, RatchetTest, SnapshotTest, LoadTest.
- `tests/conventions/` — BUG_TEMPLATE.cs, TUnit_Guide.md.
- `docs/traps/` — 6 documented agent traps: agent-circles, context-blindness, false-safety, p50-vs-max, silent-breakdown, vibe-refactoring.
- `docs/solutions/` — architecture-tests.md, ai-patterns.md.
- `docs/agents/` — integration guides for Kimi, Claude Code, Codex, OpenCode.
- `ci/github-actions/safe-ci.yml` — template CI workflow for consumer projects.
- `ci/scripts/run-and-verify-tests.sh` (originally `verify-tests.sh`) — verifies that `dotnet run` actually executed tests.
