# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
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
