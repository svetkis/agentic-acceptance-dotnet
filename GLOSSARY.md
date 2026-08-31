# Glossary

> Key terms of the repository. If you encounter an unfamiliar word in `AGENTS.md` or the docs — it is most likely here.
>

---

## Normative Terminology

> Term origin: `industry-standard` (widely accepted), `borrowed` (taken from another
> domain, used by analogy), `methodology-specific` (introduced by this methodology),
> `informal` (colloquial, not for normative documents).

| Term | Origin | Definition |
|------|--------|------------|
| **check** | industry-standard | Automated deterministic check: same input → same result, no engineering judgment. |
| **test** | industry-standard | A check that verifies code behavior through execution (unit, integration, E2E). |
| **Self-Checking Test** | industry-standard | A test that determines pass/fail automatically, without manual interpretation of results (xUnit Test Patterns, Meszaros). Baseline property; does not guarantee defect detection. |
| **Assertion Reachability** | methodology-specific | Test property: no successful execution path bypasses the assertions. Violation — an assert inside an `if` that may not execute. |
| **Fault Sensitivity** | borrowed | Test/suite property: fails when a relevant defect is present (a mutation or the original bug). From mutation testing; verified by fault injection or mutation score. |
| **Property-based test** | industry-standard | A test where a generator produces many inputs and an invariant (structural, idempotence, round-trip, bounds) must hold for all of them — instead of hand-picked examples. |
| **gate** | industry-standard | Mandatory passage point: a change does not proceed until the gate passes (`commit gate`, `PR gate`, `release gate`). |
| **audit** | industry-standard | Investigative review of a narrow area, requiring evidence and engineering judgment. Output is findings, not pass/fail. |
| **assurance** | industry-standard | The body of measures giving justified confidence that the system meets its requirements. |
| **validation** | industry-standard | Confirmation that the system solves the right problem ("built the right thing"); not to be confused with verification ("built it right"). |
| **governance** | industry-standard | Risk decision-making: residual risk acceptance, release decision, priorities. |
| **maintenance** | industry-standard | Keeping control artifacts up to date (see Control Maintenance). |
| **ratchet** | borrowed | A check on metric monotonicity: the value must not degrade (public type count, test count, complexity). From the mechanical ratchet. |
| **baseline** | industry-standard | The recorded current value of a metric from which a ratchet forbids degradation. |
| **guardrail** | methodology-specific | Any control artifact (check, gate, audit, instruction) that constrains the action space of an agent or developer. |
| **Decision Guard** | methodology-specific | See "Code Patterns". |
| **Zero Trust (analogy)** | borrowed | The "never trust, always verify" security model; in Agentic Acceptance — an analogy: no agent artifact is considered correct without a check. Not a claim of implementing Zero Trust Architecture. |
| **skill** | methodology-specific | Instruction + checklist for an agent role (audit, review, onboarding). |
| **persona** | informal | A named agent role inside a skill ("DBA Auditor"). Not a normative term. |

## Engineering Assurance Levels (control model)

| Term | Definition | Used in |
|------|------------|---------|
| **Control Foundation** | The control base: `AGENTS.md`, architecture boundaries, Decision Guards, policies. Applies before code changes. | [README.md](README.md#how-it-works), [rules/AGENTS_TEMPLATE.md](rules/AGENTS_TEMPLATE.md) |
| **1. Change Checks** | Change-level checks: compiler, nullable, analyzers, formatting, banned APIs. IDE, build, pre-commit. | [README.md](README.md#how-it-works) |
| **2. Behavior Checks** | Behavior checks: unit, regression, contract, architecture tests, ratchets; ends with agent code review (gate before PR). Local or CI test run. | [README.md](README.md#how-it-works), [tests/patterns/](tests/patterns/) |
| **3. System Checks** | Whole-system checks: integration, characterization, E2E, smoke, Testcontainers, load, deployment verification. PR, CI, release pipeline. | [README.md](README.md#how-it-works) |
| **4. Reality Checks** | Systemic drift, invisible to any single change: LLM audits (security, database, performance, UX, API, i18n, tech-debt), complexity drift (baseline + ratchet), outdated and vulnerable dependencies. Schedule or risk-trigger. | `templates/skills/*-audit/` |
| **Engineering Governance** | Process (not a level): residual risk acceptance, release decision, business and product decisions. | [docs/solutions/human-audit-bridge.md](docs/solutions/human-audit-bridge.md) |
| **Control Maintenance** | Process (not a level): keeping instructions, agent memory, backlog, baselines, suppressions, and guardrails up to date. | `templates/skills/memory-hygiene/`, `doc-hygiene/`, `backlog-hygiene/` |
| **AGENTS.md** | File with rules for AI agents. Read by the agent before every task. Can be hierarchical (root + per-module). | [rules/AGENTS_TEMPLATE.md](rules/AGENTS_TEMPLATE.md) |
| **Layers 0–2 / Inner-Outer loop** | *Legacy:* historical names of the visual metaphor that preceded the levels model. Wherever they appear, read them as the corresponding Engineering Assurance Levels. | [CHANGELOG.md](CHANGELOG.md) (history) |

## Test Patterns

| Term | Definition | Example |
|------|------------|---------|
| **Ratchet** | Test inventory: a metric (e.g., number of public types or tests) must **not decrease**. If an agent deletes types or tests — the test fails. | [tests/patterns/RatchetTest.cs](tests/patterns/RatchetTest.cs) |
| **BUG###** | Regression test naming convention: one bug = one file `BUG###_DescriptiveName.cs`. Covers all code paths where the bug could manifest. | [tests/conventions/BUG_TEMPLATE.cs](tests/conventions/BUG_TEMPLATE.cs) |
| **Snapshot test** | A test that captures and compares output (JSON, OpenAPI) with a reference file. If the DTO changes — the snapshot breaks. | [tests/patterns/SnapshotTest.cs](tests/patterns/SnapshotTest.cs) |
| **Characterization test** | A test that captures current system behavior without judging correctness. Needed so refactoring does not change behavior. | [docs/solutions/ai-patterns.md](docs/solutions/ai-patterns.md) |
| **"0 tests ran"** | Problem when the test runner found no tests, but exit code = 0. CI looks green though nothing was checked. | [ci/scripts/run-and-verify-tests.sh](ci/scripts/run-and-verify-tests.sh) |

## Code Patterns

| Term | Definition | Used in |
|------|------------|---------|
| **Read-path** | Data read path: read-only queries. `.Select()` + `.AsNoTracking()` are **mandatory**. `.Include()`, `.FindAsync()` are forbidden. | [rules/AGENTS_TEMPLATE.md](rules/AGENTS_TEMPLATE.md) |
| **Write-path** | Data write path: commands that change state. Change tracking is required, `.AsNoTracking()` is forbidden. | [rules/AGENTS_TEMPLATE.md](rules/AGENTS_TEMPLATE.md) |
| **Decision Guard** | methodology-specific: intentional deviation from standard, documented by an ID in a comment (`PERF-###`, `DB-###`, `AUD-###`) plus a short registry entry. **Not a synonym for ADR** — a lightweight reference to a decision; when full ADRs exist, the entry links to them. Checked by an architecture test for uniqueness. | [rules/AGENTS_TEMPLATE.md](rules/AGENTS_TEMPLATE.md), [templates/skills/acceptance-bootstrap/DECISION-GUARDS.md](templates/skills/acceptance-bootstrap/DECISION-GUARDS.md) |
| **Semantic Anchors** | Established terms instead of descriptions. Each term activates a specific methodology (e.g., "read-path" = `.Select()` + `.AsNoTracking()`). | [rules/AGENTS_TEMPLATE.md](rules/AGENTS_TEMPLATE.md) |

## Agents and Tools

| Term | Definition | Used in |
|------|------------|---------|
| **MCP (Model Context Protocol)** | Protocol for connecting external tools to an AI agent. Allows the agent to "touch" Telegram, browser, API. | [docs/EVIDENCE.md](docs/EVIDENCE.md) |
| **Code Review Agent** | A separate AI agent instance that reviews the diff **before** commit. Not the one that wrote the code. | [templates/skills/code-review/SKILL.md](templates/skills/code-review/SKILL.md) |
| **Skill** | Agent role: instruction + checklist for a specific task (audit, review, onboarding). Installed in `.kimi/skills/` or equivalent. | `templates/skills/` |
| **Context Marker** | Emoji marker at the beginning of an agent's reply showing active context: 🍀 (ground rules), 🔍 (review), ✅ (commit). | [rules/AGENTS_TEMPLATE.md](rules/AGENTS_TEMPLATE.md) |
| **Focused Agent** | Principle: one agent — one task. A review agent does not write code; a code agent does not review. | [templates/skills/code-review/SKILL.md](templates/skills/code-review/SKILL.md) |

## Processes and Metrics

| Term | Definition | Used in |
|------|------------|---------|
| **Audit** | Deep check of one narrow area (security, perf, DB). Runs once per sprint or on trigger, not on every PR. | `templates/skills/` |
| **Cross-pollination** | Exchange of findings between audits. For example, a security audit finds a log leak, while a UX audit finds the same endpoint as a dead-end. | [templates/skills/](templates/skills/) |
| **P50 / P95 / Max** | Latency percentiles: median, 95th percentile, maximum. Agents often optimize P50 while forgetting tail latency (Max). | [docs/traps/runtime.md#p50-vs-max](docs/traps/runtime.md#p50-vs-max) |
| **Scope creep** | Task expansion: an agent adds changes to a PR that go beyond the original request. | [templates/skills/task-compliance/SKILL.md](templates/skills/task-compliance/SKILL.md) |
| **Silent misalignment** | Silent error: the agent did not ask clarifying questions even though instructions were unclear or contradictory. | [rules/AGENTS_TEMPLATE.md](rules/AGENTS_TEMPLATE.md) |

## Technologies

| Term | Definition | Link |
|------|------------|------|
| **TUnit** | Modern test framework for .NET. Used in this repository instead of xUnit/NUnit. Run via `dotnet run --project`. | [tests/conventions/TUnit_Guide.md](tests/conventions/TUnit_Guide.md) |
| **NetArchTest** | Library for architecture tests based on reflection. Checks dependencies between layers, naming, interfaces. | [tests/patterns/ArchitectureRules.cs](tests/patterns/ArchitectureRules.cs) |
| **NBomber** | Load testing framework. Catches silent breakdown and weak points under mixed read+write load, not just "degradation at high load". | [tests/patterns/LoadTest.cs](tests/patterns/LoadTest.cs) |
| **Testcontainers** | Infrastructure for running real databases (PostgreSQL, Redis) in Docker containers during tests. Alternative to EF Core InMemory provider. | [docs/traps/testing.md#silent-breakdown](docs/traps/testing.md#silent-breakdown) |
