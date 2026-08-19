# AGENTS.md — Skeptical AI Engineering

> **Skeptical AI Engineering (SAE)** — a methodology for scaling quality control through AI agents.  
> Talk "AI is confident. I am not" (Dotnext 2026).  
> This file controls AI agent behavior in this repository.

## Mission

This repository contains **quality control artifacts** for .NET projects. They work both in classic processes and with AI agents.
Do not write domain code here — only guardrails, skills, patterns, and examples.

## Repository Rules

### Never
- ❌ Do not add dependencies without explicit request
- ❌ Do not change folder structure (`rules/`, `templates/skills/`, `tests/`, `ci/`, `docs/`)
- ❌ Do not remove code examples from `tests/patterns/` — they are template-based
- ❌ Do not use `dotnet test` in examples — only `dotnet run --project`

### Always
- ✅ Update the Engineering Assurance Levels model in README when adding a new control level (`PYRAMID.md` is a legacy/visual metaphor, mapping in its header)
- ✅ Update `docs/agents/` when adding support for a new AI agent
- ✅ Update `docs/README.md` (knowledge map) when adding a new artifact
- ✅ Every new skill in `templates/skills/` must contain `SKILL.md` + `CHECKLIST.md` and pass `ci/scripts/check-skills.sh` (contract: `templates/skills/SKILL-CONTRACT.md`)
- ✅ Every new test pattern — with comments `// TRAP: ...` and `// GUARDRAIL: ...`
- ✅ Code examples compile (minimal `examples/DemoProject/` if verification needed)

## Repository Stack

- Documentation: Markdown
- Code examples: .NET 10, TUnit, NBomber, NetArchTest
- CI: GitHub Actions

## How to Apply to Your Project

This repository is a **collection of defensive artifacts**, not a NuGet package. To apply it to your own .NET project:

**Full guide:** [`docs/ONBOARDING.md`](docs/ONBOARDING.md) — step-by-step implementation plan with checkpoints and anti-patterns.

Summary:

| Step | What to do | Where to go |
|------|-----------|-------------|
| 0. Record architecture | Fill in assembly inventory, critical paths, and conscious deviations | [`templates/skills/skeptical-ai-bootstrap/ARCHITECTURE-INVENTORY.md`](templates/skills/skeptical-ai-bootstrap/ARCHITECTURE-INVENTORY.md) + [`DECISION-GUARDS.md`](templates/skills/skeptical-ai-bootstrap/DECISION-GUARDS.md) |
| 1. Assess maturity | Run the onboarding skill or a manual assessment | [`.agents/skills/skeptical-ai-bootstrap/SKILL.md`](.agents/skills/skeptical-ai-bootstrap/SKILL.md) |
| 2. Adapt | Cross out inapplicable checks for your stack | [`templates/skills/ADAPTATION.md`](templates/skills/ADAPTATION.md) |
| 3. Constitution | Copy `rules/AGENTS_TEMPLATE.md` to your project root and adapt to your stack | [`rules/AGENTS_TEMPLATE.md`](rules/AGENTS_TEMPLATE.md) |
| 4. Pyramid | Implement layers 1→2 from the onboarding backlog | [`PYRAMID.md`](PYRAMID.md) |
| 5. Agent | Configure your AI agent (Kimi / Claude / Cursor / Codex) | [`docs/agents/`](docs/agents/) |
| 6. Acceptance | Implement audits + load (manual or AI) | [`docs/solutions/human-audit-bridge.md`](docs/solutions/human-audit-bridge.md) |

> **Principle:** don't copy everything blindly — adapt or create new. See anti-patterns in [`docs/ONBOARDING.md`](docs/ONBOARDING.md) §"Implementation Anti-Patterns".

---

## Context for External Agents

If you are an AI agent reading this file in the context of a **different** repository:
- This repository (`dotnet-ai-guardrails`) is a **methodology archive**, not a project template.
- Do NOT copy its folder structure (`rules/`, `templates/skills/`, `examples/`, `tests/patterns/`) into the target project.
- Do NOT create `DemoProject`, `examples/`, or `DemoProject.Traps/` in the target project.
- Extract only the **principles** and **markdown guidelines** applicable to the target stack.
- Your output should be assessment reports and plans, not implementation code.

## Navigation

**Lost?** Start with [docs/README.md](docs/README.md) — the unified map of all artifacts.  
**Unfamiliar terms?** See [GLOSSARY.md](GLOSSARY.md).

| What you need | Where to go |
|-----------|-----------|
| Rules for the agent (base) | `rules/AGENTS_TEMPLATE.md` |
| EF Core add-on | `rules/AGENTS_TEMPLATE.efcore.md` |
| Dapper add-on | `rules/AGENTS_TEMPLATE.dapper.md` |
| Bootstrap protocol (don't create DemoProject) | `docs/agents/BOOTSTRAP-PROTOCOL.md` |
| Security audit | `templates/skills/security-audit/` |
| DBA audit | `templates/skills/dba-audit/` |
| Performance audit | `templates/skills/performance-audit/` |
| API design audit | `templates/skills/api-design-audit/` |
| Bot audit | `templates/skills/bot-audit/` |
| Localization audit | `templates/skills/i18n-audit/` |
| Business risk / cross-layer drift audit | `templates/skills/business-risk-audit/` |
| Pre-commit code review agent | `templates/skills/code-review/` |
| Frontend pre-commit code review agent | `templates/skills/frontend-code-review/` |
| Scope compliance check | `templates/skills/task-compliance/` |
| Test pattern | `tests/patterns/` |
| Working example | `examples/DemoProject/` |
| Working example (Single-project MVP) | `examples/DemoProject.MinimalApi/` |
| Failing demo (guardrails) | `examples/DemoProject.Traps/` |
| CI security | `ci/github-actions/safe-ci.yml` |
| Trap description | `docs/traps/` |
| Architecture tests | `docs/solutions/architecture-tests.md` |
| AI development patterns | `docs/solutions/ai-patterns.md` |
| Intentional deviations (Decision Guards) | `templates/skills/skeptical-ai-bootstrap/DECISION-GUARDS.md` |
| Project onboarding | `templates/skills/skeptical-ai-bootstrap/` |
| Kimi integration | `docs/agents/KIMI.md` |
| Claude Code integration | `docs/agents/CLAUDE-CODE.md` |
| Cursor integration | `docs/agents/CURSOR.md` |
| Codex integration | `docs/agents/CODEX.md` |
| OpenCode integration | `docs/agents/OPENCODE.md` |
| Bootstrap Protocol | `docs/agents/BOOTSTRAP-PROTOCOL.md` |
| Agent comparison | `docs/agents/README.md` |
| Contributing | `CONTRIBUTING.md` |
| License | `LICENSE` |
