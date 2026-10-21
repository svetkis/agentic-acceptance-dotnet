# Task Compliance — Checklist

## Phase 1: Intent and Contracts
- [ ] Spec / backlog item has been read
- [ ] Acceptance criteria (AC) extracted
- [ ] Scope boundaries defined (IN / OUT)

## Phase 2: Diff Analysis
- [ ] `git diff main...[branch]` obtained
- [ ] List of changed files compiled
- [ ] Auto-generated files filtered out

## Phase 3: Traceability
- [ ] Each AC is mapped to code in the diff
- [ ] Each AC has status: IMPLEMENTED / TESTED / MISSING / UNTESTED
- [ ] No removed functionality required by AC

## Phase 4: Scope Creep Detection
- [ ] No code outside the spec scope
- [ ] No new public methods outside use cases
- [ ] No changes in unrelated layers
- [ ] No verification-of-verification artifacts (gates over gates, tests over gates or tests) without a documented incident at the level below (Gates for Gates trap)

## Phase 5: Evidence
- [ ] Every finding includes: file, line, code quote, spec quote
- [ ] No hallucinated findings

## Quality Gates
- [ ] Every AC is mapped or marked MISSING
- [ ] Every IMPLEMENTED AC has status TESTED or UNTESTED
- [ ] No SCOPE_CREEP without a backlog boundaries quote
- [ ] No REGRESSION_RISK without showing removed code
