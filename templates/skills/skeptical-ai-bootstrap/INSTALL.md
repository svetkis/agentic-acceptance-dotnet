# Installing the Skeptical AI Bootstrap Skill

## Why

This skill is installed into **your** .NET project so that Kimi Code CLI can scan it and produce a guardrail implementation backlog.

> **Repo-internal / for methodology archive.** Some artifacts referenced by this skill (`docs/agents/`, `rules/AGENTS_TEMPLATE.md`, `templates/skills/`, `examples/`, `tests/patterns/`) are part of the `dotnet-ai-guardrails` repository ecosystem. They are useful as internal self-audit of this repository, but should not be copied into another project as a mandatory ecosystem. Adapt or create your own guardrails based on the methodology, not the folder structure.

## Quick Install

### 1. Copy the skill into your project

From the `dotnet-ai-guardrails` repository, copy the executable skill and supporting templates into your project:

```bash
# From the root of YOUR .NET project
mkdir -p ./.kimi/skills/skeptical-ai-bootstrap
cp /path/to/dotnet-ai-guardrails/.agents/skills/skeptical-ai-bootstrap/SKILL.md ./.kimi/skills/skeptical-ai-bootstrap/
cp -r /path/to/dotnet-ai-guardrails/templates/skills/skeptical-ai-bootstrap/* ./.kimi/skills/skeptical-ai-bootstrap/
```

Or manually:
- Create `.kimi/skills/skeptical-ai-bootstrap/` in your project
- Copy `.agents/skills/skeptical-ai-bootstrap/SKILL.md`
- Copy the needed supporting templates from `templates/skills/skeptical-ai-bootstrap/`

**Language:** all operational skill templates in `templates/skills/` are English-only. Copy `SKILL.md` and `CHECKLIST.md` as-is, then translate examples and thresholds into the target project's working language if needed.

### 2. Make sure Kimi Code CLI sees the skill

```bash
kimi skills list
```

`skeptical-ai-bootstrap` should appear in the list.

### 3. Run onboarding

```bash
kimi run skeptical-ai-bootstrap
```

Or in chat with Kimi:
```
@skeptical-ai-bootstrap scan this project in standard mode
```

### 4. Get the report

The agent will generate a `.backlog/onboarding-{date}.md` file in your project + output a summary in chat.

## Alternative: onboarding without installing the skill

If you don't want to install the skill, simply open your project in Kimi Code CLI and ask:

```
Scan this .NET project using the Engineering Assurance Levels methodology from dotnet-ai-guardrails.
Produce a guardrail implementation backlog.
```

The agent will find `.csproj`, assess layers, and propose a plan.

## After Onboarding

The report contains links to artifacts from `dotnet-ai-guardrails`. These links are **guidelines and examples** for your project, not a mandatory set of files to copy:

| Artifact | Where to get |
|----------|--------------|
| `rules/AGENTS_TEMPLATE.md` | `dotnet-ai-guardrails/rules/AGENTS_TEMPLATE.md` |
| `rules/CONVENTIONS.md` | `dotnet-ai-guardrails/rules/CONVENTIONS.md` |
| Architecture tests | `dotnet-ai-guardrails/tests/patterns/ArchitectureRules.cs` |
| Ratchet tests | `dotnet-ai-guardrails/tests/patterns/RatchetTest.cs` |
| CI workflow | `dotnet-ai-guardrails/ci/github-actions/safe-ci.yml` |
| Code review skill | `dotnet-ai-guardrails/templates/skills/code-review/` (English-only; adapt examples to your project language) |
| Audits | `dotnet-ai-guardrails/templates/skills/*-audit/` (English-only; adapt examples to your project language) |
| Grooming | `dotnet-ai-guardrails/templates/skills/memory-hygiene/`, `doc-hygiene/`, `backlog-hygiene/` (English-only; adapt examples to your project language) |

**Recommendation:** fork `dotnet-ai-guardrails` and reference artifacts from your fork — this way you control versions.

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
- `standard` — all control levels (1-2 weeks)
- `high-assurance` — everything + Reality Checks (1 month)
