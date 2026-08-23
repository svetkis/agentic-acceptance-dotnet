# .NET Skeptical AI Engineering

AI-accelerated quality control methodology for .NET teams. Audits, review, and guardrails that used to require expensive expertise now scale.

[🇷🇺 Русская версия](README.ru.md)

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![License MIT](https://img.shields.io/badge/License-MIT-green.svg)
![CI](https://github.com/svetkis/dotnet-ai-guardrails/workflows/Examples%20CI/badge.svg)

> Although examples and tests are implemented in .NET, the methodology itself — Decision Guards, Engineering Assurance Levels, and prompt hygiene — applies to any stack.

This repository contains ready-made artifacts for .NET projects: rules, **skills** (an agent-role instruction + checklist, e.g. security-audit), test patterns, and CI workflows.

> **New to the methodology?** The canonical path: [GLOSSARY.md](GLOSSARY.md) (terms) →
> [How it works](#how-it-works) (the levels model) → [docs/README.md](docs/README.md)
> (knowledge map) → [docs/ONBOARDING.md](docs/ONBOARDING.md) (apply it to your project).

## Problem

AI agents (Cursor, Claude, Copilot) speed up code writing, but generate hidden tech debt, violate architectural boundaries, and break security. Manual review of such code becomes a bottleneck.

**Skeptical AI** is a verification methodology for generated code, by analogy with Zero Trust: no agent artifact is considered correct without a deterministic check. Control moves from probabilistic prompts into deterministic pipelines.

## How it works

The control model is **Engineering Assurance Levels**. An artifact is classified by
what it verifies, not by where it runs: a unit test does not become a System Check
just because it runs in CI.

| Level | When it triggers | What it includes | Key question |
|-------|------------------|------------------|--------------|
| **Control Foundation** | Before code changes | `AGENTS.md`, architecture boundaries, Decision Guards, policies | Which constraints and decisions are already made? |
| **1. Change Checks** | IDE, build, pre-commit | Compiler, nullable, analyzers, formatting, banned APIs | Can the change technically exist? |
| **2. Behavior Checks** | Local or CI test run | Unit, regression, contract, architecture tests, ratchets; the level ends with **agent code review** (gate before PR) | Are expected properties and behavior preserved? |
| **3. System Checks** | PR, CI, release pipeline | Integration, characterization, E2E, smoke, Testcontainers, load (NBomber), deployment verification | Does the system work as a whole? |
| **4. Reality Checks** | On schedule or risk-trigger | LLM audits (security, database, performance, UX, API, i18n, tech-debt), complexity drift (cognitive/cyclomatic via baseline + ratchet), outdated and vulnerable dependencies | Which properties of the codebase drift over time, invisible to any single change? |

Separate processes, not levels:

- **Engineering Governance** — residual risk acceptance, release decision, business and product decisions.
- **Control Maintenance** — keeping instructions, agent memory, backlog, baselines, suppressions, and guardrails themselves up to date (skills `memory-hygiene`, `doc-hygiene`, `backlog-hygiene`).

> **Evidence:** effectiveness metrics and ROI of the levels —
> [`docs/EVIDENCE.md`](docs/EVIDENCE.md).

### Artifact map by level

| Level / process | Repository artifacts |
|-----------------|----------------------|
| Control Foundation | `rules/AGENTS_TEMPLATE.md` (+ efcore/dapper add-ons), `rules/CONVENTIONS.md`, Decision Guards (`PERF-###`/`DB-###`) |
| 1. Change Checks | Banned APIs, Roslyn analyzers (`examples/DemoProject/src/DemoProject.Analyzers/`), `ci/github-actions/safe-ci.yml` |
| 2. Behavior Checks | `tests/patterns/` (Ratchet, NetArchTest, Snapshot, Analyzer tests), `tests/conventions/`, `templates/skills/code-review/`, `templates/skills/task-compliance/` |
| 3. System Checks | E2E/smoke patterns, NBomber (`tests/patterns/LoadTest.cs`) |
| 4. Reality Checks | `templates/skills/*-audit/` (security, dba, performance, api-design, bot, i18n, tech-debt, simplicity, complexity, version, test, mutation, spellcheck, business-risk) |
| Control Maintenance | `templates/skills/memory-hygiene/`, `doc-hygiene/`, `backlog-hygiene/` |
| Engineering Governance | `docs/solutions/human-audit-bridge.md`, release decision |

`templates/skills/` — ready-made instructions for audits. Run on schedule or when code changes in their area.

## Quick start

```bash
# 1. Clone
git clone https://github.com/svetkis/dotnet-ai-guardrails.git

# 2. Run DemoProject
cd examples/DemoProject
dotnet build
dotnet run --project tests/DemoProject.Tests

# 3. Assess your project
# Open .agents/skills/skeptical-ai-bootstrap/SKILL.md and run the checklist —
# figure out what you already have and what to implement first.

# 4. Adapt skills to your stack
# See templates/skills/ADAPTATION.md — cross out irrelevant checks.

# 5. Copy ONLY selected artifacts (not everything)
# Path: inventory → risk profile → selected controls → validation.
# Constitution (Control Foundation):
cp rules/AGENTS_TEMPLATE.md /your/project/AGENTS.md   # then edit for your stack
# One control per sprint, e.g. pre-commit review:
cp -r templates/skills/code-review /your/project/.kimi/skills/
# Test patterns — take one at a time, when it covers a real risk
# (tests/patterns/*.cs are templates to read, not a bulk-copy package):
# cp tests/patterns/ArchitectureRules.cs /your/project/tests/
```

## Structure

```
.
├── AGENTS.md                     # Instructions for AI agents
├── rules/
│   ├── AGENTS_TEMPLATE.md        # Base constitution for agents (universal)
│   ├── AGENTS_TEMPLATE.efcore.md # Add-on: EF Core-specific rules
│   ├── AGENTS_TEMPLATE.dapper.md # Add-on: Dapper / Raw SQL-specific rules
│   └── CONVENTIONS.md            # Commits, workflow, tests
├── templates/skills/                        # 28 agent-role skills (full catalog: docs/README.md)
├── docs/
│   ├── traps/                     # Agent traps
│   └── solutions/
│       ├── architecture-tests.md  # Guide to arch tests
│       └── ai-patterns.md         # 10 AI-driven development patterns
├── tests/
│   ├── patterns/                  # Test templates (Ratchet, NetArchTest, NBomber)
│   └── conventions/               # Naming, TUnit guide
├── ci/                            # CI/CD guardrails
└── examples/
    ├── DemoProject/               # Working .NET 10 example (Clean Architecture + Traps)
    └── DemoProject.MinimalApi/    # Single-project MVP (Minimal API, no layers)
```

## DemoProject

`examples/DemoProject/` is a working .NET 10 example with all patterns:

- Clean Architecture (Domain → Application → Infrastructure)
- NetArchTest: layer dependency checks
- Ratchet tests: public type and test count control
- Snapshot tests: JSON serialization contracts
- NBomber: load tests (read + write mix)
- TUnit: run via `dotnet run --project`

```bash
cd examples/DemoProject
dotnet build
dotnet run --project tests/DemoProject.Tests
```

## DemoProject.Traps

`examples/DemoProject/traps-src/DemoProject.Traps/` (see [TRAPS.md](examples/DemoProject/TRAPS.md)) — intentionally broken code demonstrating guardrails in action. Every test here fails, showing what an architectural test catches when an agent violates the rules.

```bash
cd examples/DemoProject
dotnet run --project tests/DemoProject.Traps.Tests
```

**What breaks:**
- `MutableState` — mutable state in Domain
- `DomainLeakingToInfra` — Domain depends on `System.Net.Http`
- `PaymentService` — direct dependency between Features (Orders → Payments)
- `Modules/` — cyclic dependencies between modules (ArchUnitNET)
- `RawGuidEntity` — raw `Guid` instead of strongly typed ID

See also [`examples/DemoProject/TRAPS.md`](examples/DemoProject/TRAPS.md).

## DemoProject.MinimalApi

`examples/DemoProject.MinimalApi/` — a variant for **Minimal API without Clean Architecture**. Shows how to adapt guardrails when there are no Domain / Application / Infrastructure layers.

```bash
cd examples/DemoProject.MinimalApi
dotnet build
dotnet run --project tests/DemoProject.MinimalApi.Tests
```

**What's inside:**
- Naming conventions, banned APIs (`DateTime.Now`)
- `CancellationToken` guard
- Ratchet tests for public types
- Duplication guard for business logic

See also [`examples/DemoProject.MinimalApi/README.md`](examples/DemoProject.MinimalApi/README.md).

## Navigation

Lost? The full knowledge map — every artifact indexed by role — lives in
[docs/README.md](docs/README.md). The most common entries:

| What you need | Where to go |
|---------------|-------------|
| Unfamiliar term | [GLOSSARY.md](GLOSSARY.md) |
| Agent rules (base) | `rules/AGENTS_TEMPLATE.md` (+ [EF Core](rules/AGENTS_TEMPLATE.efcore.md) / [Dapper](rules/AGENTS_TEMPLATE.dapper.md) add-ons) |
| Test patterns | `tests/patterns/` |
| Agent traps | `docs/traps/` |
| Project onboarding | [docs/ONBOARDING.md](docs/ONBOARDING.md) |
| Working example (Clean Architecture) | `examples/DemoProject/` |
| Working example (Single-project MVP) | `examples/DemoProject.MinimalApi/` |
| Failing demo (guardrails) | `examples/DemoProject/TRAPS.md` |
| AI agent setup (Kimi, Claude, Cursor, Codex) | `docs/agents/` |

## Author

**Svetlana Meleshkina** — creator of the Skeptical AI Engineering methodology, speaker.

- 💬 Telegram channel: [@kot_review](https://t.me/kot_review)
- ✉️ Telegram: [@svetkis](https://t.me/svetkis)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). We accept new skills, test patterns, traps, and agent integrations.

## License

[MIT](LICENSE)
