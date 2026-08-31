# Installing the Acceptance Bootstrap Skill

## Why

This skill is installed into **your** .NET project so that Kimi Code CLI can scan it and produce a guardrail implementation backlog.

> **Repo-internal / for methodology archive.** Some artifacts referenced by this skill (`docs/agents/`, `rules/AGENTS_TEMPLATE.md`, `templates/skills/`, `examples/`, `tests/patterns/`) are part of the `agentic-acceptance-dotnet` repository ecosystem. They are useful as internal self-audit of this repository, but should not be copied into another project as a mandatory ecosystem. Adapt or create your own guardrails based on the methodology, not the folder structure.

## Quick Install

### 1. Copy the skill into your project

From the `agentic-acceptance-dotnet` repository, copy the executable skill and supporting templates into your project:

```bash
# From the root of YOUR .NET project
mkdir -p ./.kimi/skills/acceptance-bootstrap
cp /path/to/agentic-acceptance-dotnet/.agents/skills/acceptance-bootstrap/SKILL.md ./.kimi/skills/acceptance-bootstrap/
cp -r /path/to/agentic-acceptance-dotnet/templates/skills/acceptance-bootstrap/* ./.kimi/skills/acceptance-bootstrap/
```

Or manually:
- Create `.kimi/skills/acceptance-bootstrap/` in your project
- Copy `.agents/skills/acceptance-bootstrap/SKILL.md`
- Copy the needed supporting templates from `templates/skills/acceptance-bootstrap/`

**Language:** all operational skill templates in `templates/skills/` are English-only. Copy the executable `SKILL.md` from `.agents/skills/acceptance-bootstrap/` and the supporting templates from `templates/skills/acceptance-bootstrap/` as-is, then translate examples and thresholds into the target project's working language if needed.

### 2. Make sure Kimi Code CLI sees the skill

```bash
kimi skills list
```

`acceptance-bootstrap` should appear in the list.

### 3. Run onboarding

```bash
kimi run acceptance-bootstrap
```

Or in chat with Kimi:
```
@acceptance-bootstrap scan this project in standard mode
```

### 4. Get the report

The agent will generate a `.backlog/onboarding-{date}.md` file in your project + output a summary in chat.

## Alternative: onboarding without installing the skill

If you don't want to install the skill, simply open your project in Kimi Code CLI and ask:

```
Scan this .NET project using the Engineering Assurance Levels methodology from agentic-acceptance-dotnet.
Produce a guardrail implementation backlog.
```

The agent will find `.csproj`, assess layers, and propose a plan.

## One-Shot Tool

`acceptance-bootstrap` is **not a permanent skill**. Run it once, take the
`.backlog/onboarding-{date}.md` report, then remove the skill from your project
(`.kimi/skills/acceptance-bootstrap/` or the equivalent folder). Keep it only
if you plan periodic maturity re-assessment — in that case, reference it from
your fork rather than an ad-hoc copy.

## After Onboarding

The report contains links to artifacts from `agentic-acceptance-dotnet`. These links are **guidelines and examples** for your project, not a mandatory set of files to copy:

| Artifact | Where to get |
|----------|--------------|
| `rules/AGENTS_TEMPLATE.md` | `agentic-acceptance-dotnet/rules/AGENTS_TEMPLATE.md` |
| `rules/CONVENTIONS.md` | `agentic-acceptance-dotnet/rules/CONVENTIONS.md` |
| Architecture tests | `agentic-acceptance-dotnet/tests/patterns/ArchitectureRules.cs` |
| Ratchet tests | `agentic-acceptance-dotnet/tests/patterns/RatchetTest.cs` |
| CI workflow | `agentic-acceptance-dotnet/ci/github-actions/safe-ci.yml` |
| Code review skill | `agentic-acceptance-dotnet/templates/skills/code-review/` (English-only; adapt examples to your project language) |
| Audits | `agentic-acceptance-dotnet/templates/skills/*-audit/` (English-only; adapt examples to your project language) |
| Grooming | `agentic-acceptance-dotnet/templates/skills/memory-hygiene/`, `doc-hygiene/`, `backlog-hygiene/` (English-only; adapt examples to your project language) |

**Recommendation:** fork `agentic-acceptance-dotnet` and reference artifacts from your fork — this way you control versions.

## Agent Selection

The skill automatically determines which AI agent is used in the project:
- **Kimi Code CLI** → `.kimi/skills/`
- **Claude Code** → `.claude/CLAUDE.md` + commands
- **Codex** → `AGENTS.md` + `~/.codex/config.toml` (+ `.agents/skills/`)
- **OpenCode** → `.opencode/`
- **Multiple** → universal `AGENTS.md` + specific configs

See `docs/agents/` for details on each agent:
- `docs/agents/FRONTIER-AGENTS.md` (Kimi, Claude Code, Codex)
- `docs/agents/STEP-BY-STEP-AGENTS.md` (Cursor, OpenCode, local models)

## For Agents

**If you are an AI agent executing this skill:**

1. **Do NOT create demo projects.** Do not create `examples/`, `DemoProject/`, or new `.csproj`/`.sln`.
2. **Do NOT copy the folder structure** of this repository (`rules/`, `templates/skills/`, `tests/patterns/`) into the target project.
3. **Your output is markdown only:** reports, checklists, `.backlog/*.md`, `AGENTS.md`, `CONVENTIONS.md`.
4. **Your task:** read target project → assess → plan. Do not write code "as an example" or "to demonstrate".

## Modes

- `fast` — only critical (1-2 days)
- `standard` — all control levels (2-4 weeks; see [ONBOARDING.md](../../../docs/ONBOARDING.md) "How Long It Takes" for the canonical estimates)
- `high-assurance` — everything + Reality Checks (1-2 months)
