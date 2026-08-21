# AI Agents Integration

> This directory explains how to wire guardrails into AI agents and development tools.
> There are **two integration styles** — pick by how much the agent can be trusted to
> plan its own steps.

## Bootstrap Protocol (read this first)

Before configuring any agent — read [BOOTSTRAP-PROTOCOL.md](BOOTSTRAP-PROTOCOL.md).  
It prevents situations where an agent tries to create a `DemoProject` in the target repo instead of assessing the existing codebase.

## Two Integration Guides

| Guide | For | Idea |
|-------|-----|------|
| [FRONTIER-AGENTS.md](FRONTIER-AGENTS.md) | Kimi Code CLI, Claude Code, Codex, and future strong agents | Define the **goal**, boundaries, and acceptance criteria — the agent plans the steps |
| [STEP-BY-STEP-AGENTS.md](STEP-BY-STEP-AGENTS.md) | Cursor, OpenCode, smaller/local models (Ollama, LM Studio) | Write out **every step**, exact file layouts, and paste-ready prompts |

## Which Style for Which Tool

| Tool | Style | Native format |
|------|-------|---------------|
| Kimi Code CLI | Frontier | `.kimi/skills/{name}/SKILL.md`, `kimi run {name}` |
| Claude Code | Frontier | `.claude/CLAUDE.md` + `.claude/commands/*.md` |
| Codex (OpenAI) | Frontier | layered `AGENTS.md` + `.agents/skills/` |
| Cursor | Step-by-step | `.cursorrules` / `.cursor/rules/*.md` (glob-activated) |
| OpenCode | Step-by-step | `.opencode/instructions.md` + `prompts/*.md` |

The split is a heuristic, not a law: a strong model inside Cursor can work
frontier-style; a weak CLI model needs explicit steps.

## Universal Approach

If multiple agents are used in the project or the agent is unknown —
use the universal `AGENTS.md` in the project root (adapted from
[`rules/AGENTS_TEMPLATE.md`](../../rules/AGENTS_TEMPLATE.md)). Any agent reads it.

Use the `skeptical-ai-bootstrap` skill for automatic scanning: it determines the
agent type and generates configuration in the correct format.

## Recommendation

- **CLI-first workflow, CI integration** — Kimi / Claude Code / Codex (frontier style).
- **IDE-first workflow, inline edits** — Cursor (step-by-step style).
- **Privacy / self-hosting** — OpenCode with local models (step-by-step style).

## Guide Freshness (drift control)

Agent configuration formats change fast. Every guide in this directory must carry a
`last_verified:` header with the date and a link to the vendor's primary documentation.

- A guide with `last_verified` older than **6 months** is stale: re-verify its
  configuration format against primary docs before relying on it, then bump the date.
- Claims about context windows, model names, and pricing do not belong here — they
  drift weekly; link to primary docs instead.
- Freshness of integration guides is checked by the `doc-hygiene` skill
  (Control Maintenance).
