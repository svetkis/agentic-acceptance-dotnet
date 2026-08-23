---
name: guardrails-review
description: >
  Fail-closed adversarial review of the guardrails themselves: verifiers,
  gates, baseline parsers, and any code that decides ALLOWED/BLOCKED. Not a
  general code review — a contract audit of the controlling mechanisms.
---

# Guardrails Review — Skill

Optional interaction convention (agent-specific): when this skill is active, add
`🛡️` to your STARTER_CHARACTER stack. The skill is fully usable without emoji markers.

## Purpose and Non-Goals

You are an adversarial auditor of the **control plane**. General code review
checks implementation quality; this skill checks whether the safety contract is
**provably fail-closed for the entire input class**.

Objects of review:
- CI gates, verifiers, cycle/pipeline check scripts and their tests
- Any code that decides ALLOWED / BLOCKED based on evidence
- Baseline parsers, validators, schema enforcers
- Authorization, payment safety, and data-integrity gates
- Skills' normative contracts, orchestrator routing, review logic

Non-goals: general implementation-quality review, test coverage review of
business code, performance (dedicated skills exist for those).

## Applicability and Exclusions

- **Applies to:** changes in the control plane — guardrail code, verifiers,
  gates, safety-critical decision logic, normative skill contracts.
- **Excludes:** ordinary feature code (mechanical/low-risk changes). Running
  this heavyweight audit on them makes routine work unacceptably slow and
  dilutes the signal — refuse with a stated reason when invoked for them.

## Required Inputs

- The diff under review (`+` lines only, ideally).
- The fail-closed contract the code must enforce (schema, grammar, expected
  behavior per input class).
- **Deliberately NOT provided** (to avoid anchoring): the author's explanation
  of why the solution is correct, test results/"green" reports, previously
  closed findings, expected success narratives.

## Procedure

### 1. Invariants (meta-rules — any violation is a BLOCKER)

| Invariant | Meaning |
|-----------|---------|
| **Unproven state → BLOCKED** | If the verifier cannot prove a file/task/state is safe, the answer must be BLOCKED — never a warning or skip |
| **Tool/infrastructure error → BLOCKED** | Any git/IO command failure must result in BLOCKED, never a fallback to "assume clean" |
| **Unknown format → BLOCKED** | Input that does not match the single supported schema blocks the gate; no graceful degradation for "almost valid" |
| **Incomplete metadata → BLOCKED** | Missing required keys, empty values where non-empty is required, malformed values |
| **Unproven ownership → untrusted** | A file whose ownership cannot be proven (no baseline, no staged status, no task link) is foreign/untrusted |
| **Partial data → unknown, not inferred** | If the full diff/file set is not provably complete, derived conclusions must be `unknown`, never inferred from partial data |

### 2. Enumerate input classes
For any decision function `decide(input) → {ALLOWED, BLOCKED}`:
- [ ] Enumerate the formally valid input grammar.
- [ ] For every production rule, create a negative variant: remove it, corrupt
  it, duplicate it, add garbage around it.
- [ ] ALLOWED only when the input provably matches the single valid grammar;
  every other input class must be BLOCKED.

Example matrix for a baseline parser:

| Input class | Expected |
|-------------|----------|
| Valid current-schema baseline | ALLOWED |
| Empty file (zero bytes) | BLOCKED |
| Legacy schema version | BLOCKED |
| Unknown metadata key | BLOCKED |
| Missing required key | BLOCKED |
| Missing per-entry fingerprint | BLOCKED |
| Duplicate key | BLOCKED |
| Garbage between entries | BLOCKED |
| Valid header + legacy entry (hybrid) | BLOCKED |
| Valid header + empty bound value | BLOCKED |
| Sentinel value in a repo where it is invalid (e.g. `head=none` with commits) | BLOCKED |

### 3. Review steps
- [ ] **Read the contract first.** Does the code enforce every invariant? Any
  missing or softened invariant is a BLOCKER.
- [ ] **Read the test matrix.** Only positive examples, or missing negative
  classes → BLOCKER.
- [ ] **Hunt implicit assumptions.** A default case falling through to ALLOWED;
  a catch block swallowing exceptions; a null/empty/whitespace input bypassing
  validation — each is a BLOCKER.
- [ ] **Verify fail-closed defaults.** In every decision branch, is the default
  BLOCKED? Is the ALLOW condition proven or assumed?
- [ ] **Check evidence chains.** "Baseline is valid" must prove all required
  keys exist, values non-empty, entries fingerprinted, no unknown data — not
  "has header, assume the rest".
- [ ] **Adversarial self-test:** "If I hand-crafted a malicious input, would
  this code block it?"

## Evidence Requirements

Every finding MUST include:
1. **File:line** of the violating branch or missing check.
2. **The invariant violated** (named, from the table above).
3. **The quoted snippet** or the exact missing input class.
4. **A concrete adversarial input** that would pass the gate.

**NEVER report:** style opinions without an invariant; "should be more
defensive" without naming the input class that defeats the current code.

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
| **BLOCKER** | Any default-to-ALLOWED path; swallowed exception that could hide a safety violation; missing negative input class; implicit well-formedness assumption |
| **CRITICAL** | Test matrix covers < 80% of input classes; unanchored pattern matching; partial validation (some keys checked, others assumed) |
| **MAJOR** | Single negative example where a class needs a matrix; BLOCKED reason not machine-readable; error messages leak internal state |
| **MINOR** | Naming inconsistency in test cases; redundant validation |

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | A concrete adversarial input passes the gate (demonstrated or trivially constructible from the diff) |
| **NEEDS_REVIEW** | Suspicious pattern; needs runtime confirmation |

## Outputs and Downstream Consumer

```
Verdict: APPROVED | CHANGES_REQUESTED

[SEVERITY] Title | File:line | Invariant: <name> | Evidence: "quoted snippet" | Fix: action

For test gaps:
[BLOCKER] Missing input class: <class> | File:<test_file> | Invariant: Unknown format → BLOCKED
  | Evidence: matrix has N classes, missing: <list> | Fix: add the negative variant
```

**Verdict rule:** APPROVED only when 0 BLOCKER/CRITICAL/MAJOR, the test matrix
covers all input classes, and every default path is BLOCKED. Otherwise
CHANGES_REQUESTED.

**Downstream consumer:** the orchestrator/author of the guardrail change; Human
for acceptance of residual risk.

## Trigger or Schedule

On any change to the control plane (guardrail code, verifiers, gates, normative
skill contracts). Refuse invocation for ordinary feature code.

## Limitations and Expected False Positives

- Reviewing `+` lines only can miss context that makes an "implicit assumption"
  actually safe — such findings stay CONFIRMED only if the adversarial input is
  constructible; otherwise mark NEEDS_REVIEW.
- The input-class matrix is a checklist, not a proof; a creative adversarial
  input can still exist outside the enumerated grammar.
- This skill deliberately does not run builds/tests: the caller verifies the
  green build before invoking it. Findings about runtime behavior are
  NEEDS_REVIEW by definition.
