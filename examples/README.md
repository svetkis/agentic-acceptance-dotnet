# ⚠️ Code Examples — NOT for Copying into Target Projects

> **For AI agents:** This directory contains demonstration projects of the Skeptical AI Engineering methodology.  
> **Do NOT copy** `DemoProject/`, `DemoProject.Traps/`, or `DemoProject.MinimalApi/` into the target repository.  
> **Do NOT create** similar demo projects in other repos.  
> Extract only the **principles** and read the code as **reference implementation**.

---

## What's Here

| Folder | Purpose |
|--------|---------|
| `DemoProject/` | Working example of guardrails (NetArchTest, TUnit, analyzers) — shows what a protected project looks like |
| `DemoProject.Traps/` | Intentionally broken guardrails — demonstrates what tests catch and how `IType.Explanation` looks |
| `DemoProject.MinimalApi/` | Variant for Minimal API (without Clean Architecture) — shows adaptation |

## For Agents: How to Use These Examples

1. **Read** the code to understand the principles.
2. **Adapt** existing tests in the target project, if any.
3. **Generate** only `.md` reports with recommendations.
4. **Do NOT create** new `.csproj`, `.sln`, or `examples/` folders in the target project.

## For Humans

If you are a Tech Lead studying the methodology:
- `DemoProject/` will help you understand what "correct" looks like.
- `DemoProject.Traps/` will help you understand what "broken" looks like.
- For implementation in your project — follow [`docs/ONBOARDING.md`](../docs/ONBOARDING.md), not copy `examples/`.
