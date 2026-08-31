# How to Contribute

Thank you for your interest! This repository contains defensive artifacts for .NET projects working with AI agents. We welcome improvements, new skills, test patterns, and documentation.

## Philosophy

- **Principles over artifacts.** Don't force-fit — adapt or create anew.
- **Minimal changes.** Each PR should solve one thing.
- **Documentation is code.** Changed a rule — update docs in the same commit.

## What you can add

| Type | Location | Requirements |
|------|----------|--------------|
| New skill | `templates/skills/{name}/` | `SKILL.md` + `CHECKLIST.md` |
| New test pattern | `tests/patterns/` | Comments `// TRAP: ...` and `// GUARDRAIL: ...` |
| New trap | `docs/traps/` | Scenario + consequences + solution + pattern link |
| New solution | `docs/solutions/` | Detailed guide with examples |
| Agent adaptation | `docs/agents/` | Integration instructions |
| CI improvement | `ci/` | Verified on GitHub Actions |

## What NOT to do

- ❌ Don't add dependencies without explicit request
- ❌ Don't change folder structure (`rules/`, `templates/skills/`, `tests/`, `ci/`, `docs/`)
- ❌ Don't remove code examples from `tests/patterns/` — they are templates
- ❌ Don't use `dotnet test` in examples — only `dotnet run --project`

## Process

1. **Fork** the repository
2. **Create a branch** following Conventional Commits: `feat/skill-name`, `fix/trap-description`
3. **Make changes** according to the checklist below
4. **Verify** `dotnet build` in `examples/DemoProject/` and `examples/DemoProject.MinimalApi/` (if you changed code); update `examples/DemoProject/TRAPS.md` if you changed the trap tests
5. **Open a PR** with description: what, why, how tested

## Pre-PR Checklist

- [ ] I have read `AGENTS.md` and `rules/AGENTS_TEMPLATE.md`
- [ ] If adding a skill — `SKILL.md` + `CHECKLIST.md` are present
- [ ] If adding a pattern — comments `// TRAP:` and `// GUARDRAIL:` are present
- [ ] If adding a new control level — the Engineering Assurance Levels model in root `README.md` is updated (and `docs/EVIDENCE.md` if you have effectiveness/ROI data)
- [ ] If adding an agent — `docs/agents/` is updated
- [ ] If adding an artifact (skill, pattern, trap) — `docs/README.md` (knowledge map), root `README.md`, and `docs/ONBOARDING.md` and `GLOSSARY.md` are updated when terms or steps change
- [ ] If changing `README.md` — `README.ru.md` is updated in the same commit (it is a full manual mirror)
- [ ] If changing `templates/skills/acceptance-bootstrap/` — the installable counterpart in `.agents/skills/acceptance-bootstrap/` and its relative links are checked in the same PR
- [ ] `dotnet build` passes without warnings (if applicable)
- [ ] Commits follow Conventional Commits (`feat:`, `fix:`, `docs:`, `test:`)

## Commit style

```
feat: add ux-audit skill for WPF applications
test: ratchet test for Job class count
docs: update agent comparison table
fix: correct path in SnapshotTest.cs
```

## Updating the control model

Every new control level requires updating the **Engineering Assurance Levels model in root `README.md`** — it is the canonical classifier. If you have effectiveness or ROI data for the new control, record it in [`docs/EVIDENCE.md`](docs/EVIDENCE.md) with an explicit evidence class.

## Questions?

Open an Issue with prefix `[question]` or `[proposal]`.
