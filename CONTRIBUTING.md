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
4. **Verify** `dotnet build` in `examples/DemoProject/`, `examples/DemoProject/TRAPS.md`, and `examples/DemoProject.MinimalApi/` (if you changed code)
5. **Open a PR** with description: what, why, how tested

## Pre-PR Checklist

- [ ] I have read `AGENTS.md` and `rules/AGENTS_TEMPLATE.md`
- [ ] If adding a skill — `SKILL.md` + `CHECKLIST.md` are present
- [ ] If adding a pattern — comments `// TRAP:` and `// GUARDRAIL:` are present
- [ ] If changing the pyramid — `PYRAMID.md` is updated
- [ ] If adding an agent — `docs/agents/` is updated
- [ ] If adding an artifact (skill, pattern, trap) — `docs/README.md` (knowledge map), root `README.md`, and `docs/ONBOARDING.md` and `GLOSSARY.md` are updated when terms or steps change
- [ ] `dotnet build` passes without warnings (if applicable)
- [ ] Commits follow Conventional Commits (`feat:`, `fix:`, `docs:`, `test:`)

## Commit style

```
feat: add ux-audit skill for WPF applications
test: ratchet test for Job class count
docs: update agent comparison table
fix: correct path in SnapshotTest.cs
```

## Updating PYRAMID.md

Every new layer or significant change to the feedback architecture requires updating `PYRAMID.md`:
- Add the layer to the table
- Update the synthesis diagram
- Add ROI assessment
- Update effectiveness metrics (if you have data)

## Questions?

Open an Issue with prefix `[question]` or `[proposal]`.
