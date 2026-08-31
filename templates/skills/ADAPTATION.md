# Skill Adaptation Guide

> Copying skills is easy. Adapting them is important.
> This guide will help you strike out inapplicable checks before the first run.

---

## Portable core

- Every skill is a template of checks, not a law. Before running, define the project stack and strike out what doesn't apply.
- Mark items `N/A` in `CHECKLIST.md` before the first run.
- If the project does not use a technology mentioned in a check, that check is not a finding.
- After the first run, review `[REVIEW]` findings: false positives should be added as conditions in the project's `SKILL.md`.

## Requires adaptation

- Concrete technologies: ORM, web framework, architecture, test framework, runtime.
- Folder, project, attribute, and guardrail-test names in the project.
- Thresholds for complexity, coverage, size, and budgets adopted by the team.

### Path profile

Skills reference paths like `src/*/Api/`, `src/*/Infrastructure/`, `src/Domain/` —
these are the **DemoProject layered layout**, not a requirement. Before the first
run, replace them with your project's real layout (single project, Vertical Slice,
modular monolith, ...). Skills that carry a `[ADAPT] Path profile` note at the top
have such hardcoded paths.

## Not applicable when

- The project does not match any of the technological prerequisites of the skill (e.g., no HTTP — `api-design-audit` is not applicable).
- More than 50% of the skill's checks require adaptation — better create a new skill via `acceptance-bootstrap`.

---

## Quick Checklist (3 steps)

1. **Define the stack**
   - .NET/runtime version, app type, ORM, architecture, test framework
   - Check `.csproj`, `global.json`, folder structure

2. **Strike out what doesn't apply**
   - For each skill, check the table below
   - Mark items N/A in `CHECKLIST.md` before running

3. **Run and filter**
   - After the first run, review findings marked `[REVIEW]`
   - If a finding is a false positive, add a condition to the project's `SKILL.md`

---

## Table: if the project has… → skip these checks

| If the project has… | Skip in skill… | Why |
|---|---|---|
| No layered architecture (single-project MVP) | `code-review` → dependency checks between layers | No Domain / Infrastructure projects — rule not applicable |
| Minimal API / non-MVC framework | `security-audit` → MVC authorization attributes | Uses middleware or fluent endpoint configuration |
| Minimal API / non-MVC | `code-review` → MVC authorization attribute checks | See above |
| Dapper / ADO.NET / raw SQL (no EF Core) | `performance-audit`, `code-review` → ORM-specific checks (`AsNoTracking`, `Include`, `FindAsync`) | ORM rules don't apply to raw SQL |
| Projections to DTO everywhere | `performance-audit`, `code-review` → missing `AsNoTracking` on projections | ORM doesn't track projections, `AsNoTracking` not needed |
| Worker Service / Desktop / Game (no HTTP) | `security-audit` → rate limiting, XSS, authorization attributes | No endpoints in the classical sense |
| Worker Service / Desktop / Game | `code-review` → OpenAPI snapshot, HTTP DTO | No HTTP responses |
| Legacy runtime (.NET Framework, etc.) | All skills → NetArchTest, modern test frameworks, Minimal API | Stack differs radically; use `acceptance-bootstrap` |
| Vertical Slice Architecture / other module boundaries | `code-review` → standard layer rules | Boundaries by feature, not by layer; use custom architecture test |
| No hot path methods / not latency-sensitive | `allocation-budget-audit` | Nothing to measure |
| No public API / docs | `spellcheck-audit` → public API names | Check only markdown / comments |
| Not a release / beta | `release-readiness-audit` | Won't do, document it |
| No custom analyzers / Roslyn diagnostics | `analyzer-tests-audit` | Nothing to test |
| No mutation testing in CI | `mutation-audit` → CI gate | Run as periodic audit |
| Legacy with hundreds of complexity violations | `complexity-audit` → error severity | Use baseline + ratchet, not error |
| Non-Latin test method names (e.g., Russian `Читаемое_предложение`) + `AnalysisLevel=latest-recommended` | Level 1 setup → CA1707 as error | Naming convention of the team; disable CA1707 for `tests/**.cs` in `.editorconfig`, keep it for production code. Do NOT rename tests to satisfy the analyzer |

---

## Confidence Level: how to interpret

All audit skills since version 2026-06 mark findings with a confidence level:

| Marker | Meaning | Action |
|---|---|---|
| `[CERTAIN]` | Definitely a bug / vulnerability | Fix or create a task immediately |
| `[REVIEW]` | False positive possible | Check with a human before acting. Common reasons: projection without `AsNoTracking`, endpoint with middleware authorization, single-project without Clean Architecture |

**Rule:** if the agent is not sure — it marks `[REVIEW]`. This is not weakness, this is honesty.

---

## Skill Markers

| Skill | Marker |
|---|---|
| `allocation-budget-audit` | 💸 |
| `analyzer-tests-audit` | 🔬 |
| `api-design-audit` | 🎨 |
| `backlog-hygiene` | 📋 |
| `complexity-audit` | 🧠 |
| `mutation-audit` | 🧬 |
| `release-readiness-audit` | 📦 |
| `spellcheck-audit` | 🔤 |
| `bot-audit` | 🤖 |
| `business-risk-audit` | 🧩 |
| `code-review` | 🔍 |
| `dba-audit` | 🗄️ |
| `dba-audit-dapper` | 🧵 |
| `doc-hygiene` | 📝 |
| `i18n-audit` | 🌐 |
| `memory-hygiene` | 🧹 |
| `performance-audit` | ⚡ |
| `security-audit` | 🔒 |
| `simplicity-audit` | ✂️ |
| `acceptance-bootstrap` | 🚀 |
| `task-compliance` | 📌 |
| `tech-debt-audit` | 🔧 |
| `test-audit` | 🧪 |
| `type-safety` | 🏷️ |
| `ux-audit` | 🎯 |
| `version-audit` | 🔢 |

---

## Project-specific examples

> The examples below are taken from real adaptations of the methodology. Replace project names, stack, and guardrails with your own.

### Example 1: Minimal API + EF Core + single-project MVP

Adaptations:
1. `code-review` — struck out: layer checks (no Clean Architecture). Added: fluent endpoint authorization instead of MVC attributes.
2. `performance-audit` — struck out: `AsNoTracking` on `.Select()` projections. Added: exception for raw SQL queries.
3. `security-audit` — struck out: MVC authorization attributes. Added: middleware authorization check and webhook protection via secret token.

### Example 2: Custom Roslyn analyzers

If the project has custom analyzers with project-specific diagnostic IDs, replace the `AA###` examples with your own IDs and add coverage rules for positive/negative cases to `analyzer-tests-audit`.

---

## If nothing fits

If ready-made skills require more than 50% adaptation — don't adapt, create a new one:

1. Run `acceptance-bootstrap` — it provides a framework for creating a new skill
2. Use `SKILL-ARCHITECTURE.md` to design a guardrail from scratch
3. Use `NEW-SKILL-TEMPLATE.md` to generate files for the new skill

See `templates/skills/acceptance-bootstrap/SKILL-ARCHITECTURE.md`
