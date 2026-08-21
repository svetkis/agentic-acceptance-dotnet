# Step-by-Step Agents — Explicit Instructions Integration

> **For:** tools and models that need the steps written out rather than just the goal —
> Cursor, OpenCode, and any setup running on smaller/local models (Ollama, LM Studio).
>
> **Idea:** give exact file layouts, exact file contents, and numbered steps. Don't rely
> on the model to invent intermediate structure.
>
> `last_verified: 2026-08-21`

---

## 1. Universal 6-Step Plan

1. **Constitution.** Copy `rules/AGENTS_TEMPLATE.md` to the project root as `AGENTS.md`,
   adapt Mission / Architecture / Stack / Conventions to your project. Add
   `AGENTS_TEMPLATE.efcore.md` / `AGENTS_TEMPLATE.dapper.md` content if applicable.
2. **Assess.** Run the onboarding prompt (§3) in your tool. Get a backlog, not code.
3. **Install the review check** in your tool's native format (§2) — start with
   code-review, add audits later.
4. **CI.** Copy [`ci/github-actions/safe-ci.yml`](../../ci/github-actions/safe-ci.yml)
   and [`ci/scripts/run-and-verify-tests.sh`](../../ci/scripts/run-and-verify-tests.sh);
   adapt paths and `TEST_DIRS`.
5. **Verify the guardrails themselves.** Break one test on purpose — CI must go red.
6. **Record N/A.** Cross out inapplicable checklist items with a reason.

## 2. Tool-Native Formats

### Cursor (IDE-integrated)

- `.cursorrules` (legacy) or `.cursor/rules/*.md` (new) — a numbered set works well:
  `001-general.md`, `002-domain.md`, `003-infrastructure.md`, `004-tests.md`, `005-security.md`, `006-audits.md`.
- Rule files use glob-activated frontmatter — the rule loads automatically when a matching file is open:

  ```yaml
  ---
  description: Domain layer rules
  glob: src/**/Domain/**/*.cs
  alwaysApply: false
  ---
  ```

- Onboarding/review prompts: paste them in Chat (Ctrl+L) for single questions, or
  Composer (Ctrl+I) for multi-step work. Notepads can substitute for skills.
- Limitations: no CLI for CI launch; rules need the matching file open to activate.

### OpenCode (open-source, self-hosted)

- Recommended layout: `.opencode/instructions.md` (behavior rules) + `.opencode/prompts/{onboarding,code-review,security-audit,...}.md` (one file per check).
- Do **not** duplicate the constitution in `instructions.md` — root `AGENTS.md` stays
  the single source; `instructions.md` only adds behavior specifics.
- Launch: `opencode --prompt .opencode/prompts/onboarding.md`, or the VS Code
  extension command palette ("OpenCode: Run Prompt").
- Format varies across forks: keep files plain Markdown and document the chosen
  layout in the project README.

## 3. Prompts to Paste (verbatim)

**Onboarding:**

```
Scan this .NET project. Evaluate guardrails against the Engineering Assurance Levels.
Output an implementation backlog. Consider that we use {stack}.
Do not create any files yet — report first.
```

**Pre-commit review:**

```
Review the staged diff (git diff --cached) against the project AGENTS.md.
For each violation: file, line, severity (BLOCKER/CRITICAL/MAJOR/MINOR), and a fix.
Verdict: APPROVED or CHANGES_REQUESTED. Do not refactor beyond the diff.
```

## 4. Smaller / Local Models

- Long instructions degrade quickly: keep each rule file under ~1 screen and prefer
  few-shot examples over abstract rules.
- Few-shot example: show one bad snippet + the expected finding, then let the model
  apply the pattern to the diff.
- Expect literal compliance: the numbered plan in §1 exists so the model never has to
  invent intermediate steps. If output quality drops, split a prompt into smaller ones.
- Verify results mechanically (CI, `run-and-verify-tests.sh`) — never trust the
  model's self-assessment.

## 5. Limitations

- Neither tool runs bash freely from rules; tests and CI remain the enforcement layer.
- Cursor rule activation is glob-based — a rule silently inactive on a file outside
  its glob is a known blind spot; keep `001-general.md` with `alwaysApply: true`.
