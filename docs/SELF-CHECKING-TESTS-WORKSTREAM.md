# Self-Checking Tests — Behavior Checks hardening plan

> Follow-up of the completed methodology revision 2026-07-14 (METH-001…METH-024;
> the plan document was removed after full execution — see `CHANGELOG.md` and git history).
> **Status:** SV-001…SV-005 done; SV-006 — in progress with open blockers (see below).
> **Practice source:** a production .NET project under SAE audit (2026-07-14).
> The source revision is not yet fixed — **do not copy code, fixtures, or
> configs from it**; port analyzers from principles and re-implement.
> **Remove this document after SV-006 blockers are closed** (record in `CHANGELOG.md`).

## Terminology

Three distinct properties — do not merge them (see `GLOSSARY.md`):

| Term | Origin | Meaning |
|------|--------|---------|
| **Self-Checking Test** | industry-standard (xUnit Test Patterns, Meszaros) | The test determines pass/fail automatically, without manual interpretation |
| **Assertion Reachability** | SAE-specific | No successful execution path bypasses the assertions |
| **Fault Sensitivity** | borrowed (mutation testing) | The test fails when a relevant defect is present |

This workstream strengthens **assertion reachability** and **fault
sensitivity**; self-checking is the assumed baseline. **Non-validating tests**
remains the name of the trap (see [`docs/traps/non-validating-tests.md`](traps/non-validating-tests.md)).

## Why

A discovered, executed, green test run does not prove the test can detect a
regression. The audited project contained real classes of false safety:

- test method without assertions;
- `IsNotNull()` as the only behavior check;
- assertions that can be bypassed on the successful path (inside an `if` that
  may not execute);
- tautological assertions (`Assert.That(x).IsEqualTo(x)`);
- negative-only fixtures without a positive control;
- Playwright `expect(true)`, body-only checks, `waitForTimeout`;
- test name promises a postcondition that no assertion verifies.

## Goal

Make assertion reachability and fault sensitivity verifiable properties of
Behavior Checks: a constitution rule, a trap document, audit checklist items,
**compile-time analyzers that catch non-validating tests automatically**, and
a reproducible green/trap demonstration.

## Backlog

### SV-001 — Constitution rule (Control Foundation) ✅ done

- Added to `rules/AGENTS_TEMPLATE.md`, Tests section:
  - self-checking + assertion reachability + fault sensitivity required;
  - forbidden: zero-assert, `IsNotNull()`-only, assertions bypassed on the
    successful path, tautological, negative-only without positive control —
    **unless the weaker check *is* the contract and the reason is documented**;
  - assertions verify observable postconditions, not the fact of execution;
  - critical behavior requires explicit fault-sensitivity verification
    (mutation testing or deliberate fault injection).
- **Done when:** rule in template + link to the trap document. ✅

### SV-002 — Trap document ✅ done

- `docs/traps/non-validating-tests.md`: catalog of non-validating test forms,
  why they stay green, how to catch them; terminology table.
- **Done when:** document in the knowledge map, cross-link **from**
  `false-safety.md`. ✅

### SV-003 — Audit checklists (Reality Checks) ✅ done

- `templates/skills/test-audit/`: "Test Validity" section — zero-assert,
  `IsNotNull()`-only, tautology, assertion reachability (assert-in-`if` is an
  investigation signal; CONFIRMED only with control-flow evidence of bypass),
  negative-only.
- `templates/skills/mutation-audit/`: cross-link between mutation score and
  fault sensitivity of critical tests.
- **Done when:** items present, schema-lint passes. ✅

### SV-004 — Pattern + demonstration (Behavior Checks) ⏳ blocked by SV-006

- `tests/patterns/` — fault-injection check template (a test that breaks
  production code locally and proves suite sensitivity).
- Green: `examples/DemoProject/`; Red: `examples/DemoProject.Traps/`
  (a non-validating test **caught by the SV-006 analyzers** — without them the
  trap test would stay green and the demonstration proves nothing).
- **Done when:** CI green on DemoProject, Traps fails by design on the
  analyzer diagnostic (not only because other trap tests fail).
- **Open:** red demo must assert the exact SAE006–SAE009 diagnostic set.

### SV-005 — Frontend ✅ done

- `templates/skills/frontend-code-review/`: items on `expect(true)`,
  body-only checks, `waitForTimeout` as a replacement for condition waits.
- **Done when:** items present, schema-lint passes. ✅

### SV-006 — Compile-time analyzers (Change Checks) ⏳ in progress — core of the workstream

Port the analyzer family observed in the audited project (`SLK004`–`SLK007`)
to `examples/DemoProject/src/DemoProject.Analyzers/` (which already ships
SAE003–SAE005). **Re-implement from principles — do not copy code** until the
source project is stabilized and its revision fixed.

- **Semantic assertion model** (shared helper): recognize assertion APIs across
  frameworks — TUnit (`Assert.That(...)` + chained conditions), xUnit/NUnit
  (`Assert.*`), FluentAssertions (`.Should().*`), mocking verifications
  (`mock.Received()`, `.DidNotReceive()`), and custom assertion helpers by
  convention (configurable suffix/pattern). Without this model every analyzer
  below false-positives on unconventional assertion styles.
- **SAE00x — test method must assert** (analog of SLK004): a method marked as
  a test with no recognized assertion and no verification call.
- **SAE00x — null-only assertion** (SLK005): the only assertion is a null /
  not-null check while the test name promises a concrete postcondition.
- **SAE00x — bypassed assertion** (SLK006): control-flow analysis — an
  assertion is reachable on *some* path but a successful path exists that
  bypasses every assertion. Report only when the bypass is provable; otherwise
  stay silent (audit skill handles the NEEDS_REVIEW cases).
- **SAE00x — tautological assertion** (SLK007): assertion that cannot fail by
  construction (`x == x`, literal-vs-literal, `expect(true)` analogs).
- **Analyzer tests**: positive/negative fixtures per diagnostic, following the
  existing `tests/patterns/AnalyzerTests.cs` pattern and the
  `analyzer-tests-audit` skill; ratchet on diagnostic coverage.
- **Done when:** analyzers build with warnings-as-errors, analyzer tests green,
  SV-004 red demo fails on these diagnostics, diagnostics documented in
  `tests/conventions/` and the trap document's guardrail list.

#### Open blockers (from review 2026-07-14)

- [x] SAE006–SAE009 not wired to test projects via `OutputItemType="Analyzer"`.
- [x] Reachability uses `ControlFlowGraph` for normal flow and a statement-level fallback for `try/catch` exceptional paths.
- [x] Assertions inside uncalled lambda/local functions are still counted.
- [x] `Assert.That(1).IsEqualTo(2)` is still misclassified as tautology.
- [x] Assertion recognition is still name-based and not configurable.
- [x] Red demo does not assert the exact SAE006–SAE009 diagnostic set.

#### Known limitations

- Try/catch reachability uses the syntax fallback instead of CFG (Roslyn
  models exceptional edges differently across versions); catch blocks without
  assertions are still caught by the fallback.
- The try/catch fallback is a **documented heuristic**, not a proof of
  reachability for arbitrary control flow. Known false negatives/positives:
  - `if/else` with an early `return` in the `if` branch and assertions after
    the `if/else` block can be missed (the `else` branch continues to the
    assertion).
  - `throw` is always treated as an interruption, even when a `catch` block
    contains the mandatory assertion.
- SAE006–SAE009 are `Warning` severity by default; `DemoProject.Tests`
  promotes them to errors via `WarningsAsErrors` to keep the local Change
  Check mandatory.

## Non-goals

- No rewriting of existing DemoProject tests for mutation coverage.
- No copying of code, fixtures, or configs from the audited project — principles only.
- No mandatory mutation testing on every PR (cost — release-only or risk-trigger).
- No fault-sensitivity proof for every test — only critical behavior (cost).

## Related artifacts

- [`docs/traps/false-safety.md`](traps/false-safety.md) — green CI ≠ working code
- [`docs/traps/non-validating-tests.md`](traps/non-validating-tests.md) — the trap itself
- [`templates/skills/test-audit/`](../templates/skills/test-audit/)
- [`templates/skills/mutation-audit/`](../templates/skills/mutation-audit/)
- [`templates/skills/analyzer-tests-audit/`](../templates/skills/analyzer-tests-audit/)
- [`examples/DemoProject/src/DemoProject.Analyzers/`](../examples/DemoProject/src/DemoProject.Analyzers/)
- [`tests/conventions/`](../tests/conventions/)
