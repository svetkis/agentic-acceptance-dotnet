# Bootstrap Protocol — Agent Mode Boundary

> **Purpose:** Prevent situations where an agent, having received context from `agentic-acceptance-dotnet`, starts creating a `DemoProject` in the target repo.
> **Audience:** AI agent (Kimi / Claude / Codex / OpenCode).

---

## 1. Two Modes — Pick Yours

When you see files from `agentic-acceptance-dotnet`, determine the mode **BEFORE** any actions.

```
If the current directory path contains "agentic-acceptance-dotnet":
    → SOURCE MODE
Else if there is a .sln or .csproj in the current directory:
    → TARGET MODE
Else:
    → ASK the user: "Please provide the path to the .NET project to assess."
```

### SOURCE MODE
- You are inside the methodology repository.
- Your task: **read artifacts**, explain them to the user, help adapt.
- **FORBIDDEN:** modify this repo without explicit request.
- **FORBIDDEN:** copy the structure of `examples/`, `templates/skills/`, `tests/` anywhere.

### TARGET MODE
- You are inside a third-party .NET project.
- Your task: **assess the codebase** and produce a guardrail implementation plan.
- **FORBIDDEN:** create `examples/`, `DemoProject/` (including `traps-src/`), `DemoProject.MinimalApi/`, new `.sln`/`.csproj`.
- **FORBIDDEN:** write code "for demonstration" or "as an example".
- **ALLOWED:** generate `.md` reports, checklists, `AGENTS.md`, `CONVENTIONS.md`.

---

## 2. Why Agents Break

| Trap | What the agent sees | What it does wrong | Correct behavior |
|------|---------------------|--------------------|------------------|
| `examples/DemoProject.sln` | A full .NET project with tests | "I need to create a similar project for the user" | This is a DEMONSTRATION of the methodology, not a template to copy |
| `tests/patterns/ArchitectureRules.cs` | Ready-made C# code | "I need to create `tests/patterns/` and write code" | Adapt existing tests in the target repo, or create a report with recommendations |
| `templates/skills/*/SKILL.md` | Skill instructions | "I need to create `.kimi/skills/` with all skills" | Copy only markdown skill files, adapting to the stack. Do NOT create `.cs` skill files. |
| `AGENTS.md` with `[ADAPT]` | A template with placeholders | "I need to fill it and create a full project" | Replace `[ADAPT]` with target project specifics, save as `AGENTS.md`. |

---

## 3. Output Contract (what you can create)

### ✅ ALLOWED Output Artifacts

| Type | Example | When |
|------|---------|------|
| Markdown reports | `.backlog/onboarding-2026-06-05.md` | Always |
| Agent rules | `AGENTS.md`, `CONVENTIONS.md` | After adaptation |
| CI configs | `.github/workflows/safe-ci.yml` | If the project uses GitHub Actions |
| EditorConfig | `.editorconfig` | If missing or needs update |
| Build props | `Directory.Build.props` (adaptation) | For nullable/warnings configuration |
| Skills (markdown only) | `.kimi/skills/code-review/SKILL.md` | During adaptation |

### ❌ FORBIDDEN Output Artifacts

| Type | Example | Why forbidden |
|------|---------|---------------|
| Demo projects | `examples/DemoProject/` (incl. `traps-src/`), `DemoProject.MinimalApi/`, `DemoProject.sln` | These are methodology examples, not target code |
| New projects | `MyApp.Tests.csproj` (created from scratch) | Assessment agent does not write production code |
| Methodology folders in root | `rules/`, `templates/skills/`, `tests/patterns/` in target repo root | Target project has its own structure. Exception: `.kimi/skills/` for markdown skills — normal |
| Code "for example" | `// Here's how it should look:` + 20 lines of C# | Agent must not generate unimplemented code in a working codebase |

---

## 4. Decision Tree

```
User asks: "Assess my project using SAE methodology"
    │
    ▼
Are you in agentic-acceptance-dotnet?
    ├── YES → "I cannot assess a project from inside the methodology repository. Please provide the path to the target project."
    └── NO → Is there a .sln in cwd?
            ├── YES → Run skeptical-ai-bootstrap skill (assess existing codebase)
            └── NO → "Please provide the path to the .NET project (where .sln is located)"

User asks: "Show me an example of an architecture test"
    │
    ▼
Are you in agentic-acceptance-dotnet?
    ├── YES → Read examples/DemoProject/tests/... and explain
    └── NO → "Examples are in the methodology repository: examples/DemoProject/tests/..."
              Do NOT create these files in the target project.

User asks: "Adapt the code-review skill for my stack"
    │
    ▼
Are you in the target project?
    ├── YES → Read templates/skills/code-review/SKILL.md from agentic-acceptance-dotnet (if available) →
    │        Adapt text → Save to .kimi/skills/code-review/SKILL.md (markdown only!)
    └── NO → "Install me in the target project or provide its path."
```

---

## 5. Evidence Checklist for the Agent

Before creating ANY file, ask yourself:

- [ ] Is it `.md`, `.yml`, `.editorconfig`, or `.props`? (If `.cs`/`.csproj`/`.sln` — stop)
- [ ] Does this file already exist in the target repo? (If not — is it really needed?)
- [ ] Am I creating `examples/`, `DemoProject/`, or any demo project (`Traps`, `MinimalApi`)? (If yes — stop, this is a trap)
- [ ] Am I copying the structure of `agentic-acceptance-dotnet`? (If yes — stop)
- [ ] Is my task to assess or to create? (Assess only → reports only)

---

## 6. Kimi Code CLI Integration

If the skill is installed in `.kimi/skills/skeptical-ai-bootstrap/`:

```bash
# Correct launch — from the root of the TARGET project
$ cd /path/to/target-project
$ kimi run skeptical-ai-bootstrap

# Incorrect launch — from inside agentic-acceptance-dotnet
$ cd /path/to/agentic-acceptance-dotnet
$ kimi run skeptical-ai-bootstrap  # ← agent must refuse and ask for path
```

---

> **Principle:** SAE methodology is stretched over a project like an **assessment mesh**, not a **winch that pulls the structure over**.
