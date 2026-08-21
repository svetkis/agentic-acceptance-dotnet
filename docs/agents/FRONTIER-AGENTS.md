# Frontier Agents — Goal-Oriented Integration

> **For:** strong agentic tools that plan their own steps when given a well-formed goal —
> Kimi Code CLI, Claude Code, Codex (OpenAI). Also any future agent that reads `AGENTS.md`
> and supports skills.
>
> **Idea:** don't script the steps — define the **goal**, the **boundaries**, and the
> **acceptance criteria**. The agent figures out the path.
>
> `last_verified: 2026-08-21`

---

## 1. The Goal (give this to the agent verbatim)

```
Integrate Skeptical AI Engineering guardrails into this .NET project.

Desired end state:
1. A root AGENTS.md constitution adapted from rules/AGENTS_TEMPLATE.md
   (plus efcore/dapper add-ons if applicable) — never copied verbatim.
2. Guardrail skills the team will actually use (start with code-review,
   then audits per risk) installed in the agent-native format.
3. CI that builds with TreatWarningsAsErrors, runs tests via
   `dotnet run --project` (never `dotnet test`), and fails when
   "0 tests ran" or when tests fail.
4. An adapted backlog for the remaining Engineering Assurance Levels.

Boundaries:
- Do NOT create demo projects, examples/, or new .sln files.
- Do NOT migrate the test framework; adapt the verification script instead.
- Every not-applicable check must be crossed out with a reason, not silently dropped.

Acceptance criteria:
- `dotnet build` passes with TreatWarningsAsErrors=true.
- CI fails on a deliberately broken test AND on a "0 tests ran" run.
- A report listing: what was reused, what was adapted, what was created, what was skipped and why.
```

Start from the `skeptical-ai-bootstrap` skill — it performs the maturity scan and produces the backlog.

## 2. Shared Onboarding Prompt

```
Scan this .NET project. Evaluate guardrails against the Engineering Assurance Levels.
Output an implementation backlog. Consider that we use {stack}.
```

The onboarding must decide, per skill: **reuse** the template, **rewrite** it for the
project context (e.g. `code-review-dapper`), or mark **N/A** with a reason. The output
is an installed skill + an updated knowledge map, not a copy of this repository.

## 3. Agent-Native Formats (cheat sheet)

| Agent | Constitution | Skills | Launch |
|-------|--------------|--------|--------|
| **Kimi Code CLI** | root `AGENTS.md` | `.kimi/skills/{name}/SKILL.md` (project) / `~/.kimi/skills/` (user) | `kimi run {skill-name}` |
| **Claude Code** | `.claude/CLAUDE.md` (+ `settings.json` with `permissions.allow_bash/allow_edit/allow_read`) | `.claude/commands/{name}.md` (`## Description` / `## Instructions` / `## Severity`: BLOCKER/CRITICAL/MAJOR/MINOR) | `/{command-name}` |
| **Codex (OpenAI)** | layered `AGENTS.md` (closest file wins), `.codex/config.toml` (project — trusted repos only) | `.agents/skills/{name}/SKILL.md`, registered via `[[skills.config]]`; custom agents in `.codex/agents/*.toml` | interactive prompt, `codex exec` for CI |

Typical daily commands (Kimi syntax; flags are pseudo-commands — adapt):

```bash
kimi run code-review                       # reads git diff --cached itself
kimi run code-review --git-diff main...feature/my-branch
kimi run dba-audit --git-diff --paths "src/*/Infrastructure/Migrations/"
kimi run performance-audit --mode pre-release
```

Grooming once per sprint: `kimi run memory-hygiene | doc-hygiene | backlog-hygiene`.

## 4. Nuances That Matter

- **Kimi**: skills are Markdown + YAML frontmatter; they do not auto-launch on PR —
  wire `kimi run {skill}` into CI explicitly. Bulk-install by copying
  `templates/skills/{skill}/` into `.kimi/skills/`.
- **Claude Code**: no skill marketplace — files in `.claude/` just work; commands must
  be invoked explicitly; use `/add` and `/compact` to manage context.
- **Codex**: long audit checklists belong in skills, referenced from `AGENTS.md`;
  custom prompts were removed in CLI 0.117.0 — convert to skills; verify
  `[[skills.config]]` keys against current docs.
- **Any agent**: root `AGENTS.md` is the universal fallback — it is read even when the
  agent's native format differs.

## 5. Limitations

- Frontier agents still follow instructions literally at scale: keep the constitution
  short and the acceptance criteria testable.
- Never rely on the agent's self-report — CI is the source of truth for "done".
