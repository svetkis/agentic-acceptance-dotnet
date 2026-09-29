---
name: mutation-verify
description: >
  Diff-scoped mutation spot-check without Stryker: right after code and its
  tests are written, apply a few hand-made mutants to the changed lines and
  prove the suite goes red. Catches non-validating tests per change; works
  where Stryker.NET cannot run (TUnit / Microsoft Testing Platform).
---

# Mutation Verify — Skill

> **[ADAPT] Commands:** filter syntax is runner-specific — current TUnit /
> Microsoft Testing Platform: `--treenode-filter '/*/*/*Order*/*'` (4-segment
> tree path `/Assembly/Namespace/Class/Test`, `*` wildcards; there is no
> `--filter`); xUnit/NUnit/MSTest via `dotnet test`: `--filter
> 'FullyQualifiedName~Order'`. Discover yours with `-- --help` before the
> first mutant. See `ADAPTATION.md`.

Optional interaction convention (agent-specific): when this skill is active,
add `🦠` to your STARTER_CHARACTER stack (example: `🍀 🦠`). The skill is fully
usable without emoji markers.

## Purpose and Non-Goals

Code and its tests were just written or changed, and the suite is green.
Green proves nothing until at least one test has been seen failing on this
behavior. Your task: apply 3–7 hand-made mutants (minimal one-line behavior
changes) to the **changed code only**, run the narrow test subset, and confirm
every mutant is killed. A surviving mutant is a non-validating test — the trap
documented in [`docs/traps/testing.md#non-validating-tests`](../../../docs/traps/testing.md).

Non-goals: no mutation score and no whole-assembly coverage (that is the
periodic `mutation-audit` with Stryker.NET); no golden masters for spec-less
code (that is the `CharacterizationTest` pattern); the skill does not prove the
code matches the spec — only that the tests can fail.

## Applicability and Exclusions

- **Any stack with a runnable test command** — no Stryker dependency; this is
  the fallback when Stryker.NET does not support the project's test platform
  (TUnit / Microsoft Testing Platform).
- **Scoped to the change diff** (new/changed logic and its tests), not the
  whole assembly — that is what keeps it to minutes.
- **Skip for:** docs/config-only changes, generated code, pure renames, and
  trivial code where mutants are equivalent by construction.
- Where Stryker.NET does run, it stays the deeper periodic control
  (`mutation-audit`); this skill remains the fast per-change gate.

## Required Inputs

- A clean worktree (or a recorded pre-run `git status --porcelain`) — mutants
  are applied and reverted, so "all reverted" must be verifiable.
- The change diff: changed production code plus the added/changed tests.
- A narrow, fast test filter covering the change (seconds, not minutes).
- A mutant budget agreed for the change: 3–7, critical paths get the full 7.

## Procedure

### 1. Scope
- [ ] Pre-run worktree state recorded (`git status --porcelain`).
- [ ] 3–7 mutation points selected in changed logic — boundaries and error paths first.

### 2. Apply and run (one mutant at a time)
- [ ] Mutant applied as a minimal one-line change from the catalog below.
- [ ] Narrow test subset run with the project's own runner (current TUnit: `dotnet run --project tests/... -- --treenode-filter '/*/*/*Order*/*'`); the run must execute > 0 tests before the verdict is read.
- [ ] Outcome recorded: **killed** (failing test name + assertion message) or **survived**.
- [ ] Mutant reverted before the next one — never two mutants at once (they mask each other's kills).

### 3. Bookkeeping
- [ ] Every surviving mutant → the test is strengthened or a test is added, the mutant is re-applied, and the kill is re-verified.
- [ ] Suspected equivalent mutant → documented with a reason, not chased.
- [ ] Final cleanup check: all mutants reverted, `git status --porcelain` matches the pre-run state, full suite green again.

### Mutant catalog (pick per changed code)

| Kind | Example |
|------|---------|
| Boundary | `>` → `>=`; loop bound `Count` → `Count - 1` |
| Arithmetic / constant | `+` → `-`; `0` → `1`; `* 2` → `/ 2` |
| Boolean | `&&` → `||`; remove `!` |
| Early return | delete a guard clause |
| Error path | `throw` → `return` (swallow) |
| Ordering / literal | `OrderBy` → `OrderByDescending`; `""` → `"x"` |

## Evidence Requirements

Every finding MUST include:
1. **Mutant:** `src/Domain/Order.cs:42`, before/after snippet (`>` → `>=`)
2. **Command + filter actually run** and its output with the mutant applied
3. **For a kill:** failing test name and the assertion message
4. **For a fixed survivor:** the strengthened test and its re-run output

**NEVER report:**
- "Tests are weak" without a concrete surviving mutant and a test-run output
- "All mutants reverted" without the worktree check output
- A mutation score or a percentage — this skill produces kill/no-kill facts only
- "Survived" verdicts from a filtered run that executed 0 tests (empty filter match)

## Finding Schema

```text
ID
Severity: BLOCKER | CRITICAL | MAJOR | MINOR
Confidence: CONFIRMED | NEEDS_REVIEW
Category / Control
Evidence: file:line, command output, trace or reproduction
Impact
Recommended action
Owner / disposition
```

## Severity and Confidence

| Severity | Meaning |
|----------|---------|
| **BLOCKER** | Mutant survives in changed critical-path logic and no test was strengthened; or mutants left applied / worktree dirty after the check |
| **CRITICAL** | Surviving mutant in changed error/edge handling |
| **MAJOR** | Multiple surviving mutants in trivial changed code — test-quality smell |
| **MINOR** | Equivalent mutant documented, or a single survivor in trivial changed code |

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Filtered run output shows the suite stayed green with the mutant applied |
| **NEEDS_REVIEW** | Mutant suspected equivalent — requires maintainer judgment |

## Outputs and Downstream Consumer

```markdown
## Mutation Verify — {date}, change: {task/branch}

| # | Mutant | Change | Run | Result |
|---|--------|--------|-----|--------|
| 1 | src/.../Order.cs:42 | `>` → `>=` | dotnet run --project tests/... -- --treenode-filter '/*/*/*Order*/*' | 🟢 killed by OrderTests.Boundary |
| 2 | src/.../Order.cs:57 | `throw` → `return` | (same) | 🔴 survived → test strengthened, re-run: killed |

Cleanup: `git status --porcelain` before = after; full suite green without mutants.
```

**Downstream consumer:** the Programmer Agent of the same task (strengthen
tests in-task); recurring survivors feed the periodic `mutation-audit` /
`test-audit` backlog.

## Trigger or Schedule

Immediately after the code and tests for a change are written and green —
before the completion report or commit. Per change, not per sprint. Must for
critical paths; recommended wherever behavior (not just structure) changed.

## Limitations and Expected False Positives

- 3–7 hand-picked mutants are a spot-check, not a score: no survivors is not
  proof of test strength — that claim belongs to `mutation-audit`.
- Equivalent mutants are unavoidable — document them, do not chase them.
- Mutant choice is biased toward what the author already thought about; the
  catalog order (boundaries, error paths first) mitigates but does not remove it.
- If a filtered run takes minutes, reduce the mutant count — never the cleanup check.
- Verifies that tests can fail, not that the code is correct: killed mutants +
  green tests still say nothing about matching the spec.
