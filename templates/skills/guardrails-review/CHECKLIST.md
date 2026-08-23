# Guardrails Review — Checklist

## Entry Precondition
- [ ] Change is control-plane (guardrail/verifier/gate/safety logic/skill contract)
- [ ] Refused if invoked for ordinary feature code (reason stated)

## Inputs Received (anchoring control)
- [ ] Fail-closed contract (invariants) provided
- [ ] Diff (`+` lines) provided
- [ ] Input format specification provided
- [ ] NOT received: author's justification, green reports, prior rounds

## Invariants Verified
- [ ] Unproven state → BLOCKED (no warning/skip paths)
- [ ] Tool/infrastructure error → BLOCKED (no "assume clean" fallbacks)
- [ ] Unknown format → BLOCKED (no graceful degradation)
- [ ] Incomplete metadata → BLOCKED
- [ ] Unproven ownership → untrusted
- [ ] Partial data → unknown, not inferred

## Input-Class Matrix
- [ ] Valid grammar enumerated
- [ ] Negative variant for every production rule (remove/corrupt/duplicate/garbage)
- [ ] ALLOWED only on provable match to the single valid grammar
- [ ] Hybrid/partial inputs covered as BLOCKED classes

## Implicit Assumptions Hunt
- [ ] No default case falls through to ALLOWED
- [ ] No swallowed exceptions on safety paths
- [ ] No null/empty/whitespace bypass
- [ ] ALLOW conditions proven, not assumed
- [ ] Evidence chains complete (all keys, fingerprints, no unknown data)

## Adversarial Self-Test
- [ ] "If I hand-crafted a malicious input, would this code block it?" answered per class

## Report
- [ ] Findings in the canonical format with invariant names and quoted evidence
- [ ] Verdict: APPROVED only if 0 BLOCKER/CRITICAL/MAJOR and matrix complete
- [ ] Test gaps reported as BLOCKER with the missing class list
