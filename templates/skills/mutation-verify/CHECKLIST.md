# Mutation Verify — Checklist

## Before Starting
- [ ] Worktree state recorded (`git status --porcelain`)
- [ ] Changed logic identified from the diff (production code + its tests)
- [ ] Narrow, fast test filter is known
- [ ] Mutant budget set (3–7; critical paths get the full 7)

## Mutant Selection (catalog — pick applicable)
- [ ] Boundary condition (`>` → `>=`, loop bound)
- [ ] Arithmetic / constant (`+` → `-`, `0` → `1`)
- [ ] Boolean logic (`&&` → `||`, remove `!`)
- [ ] Early return / guard clause removed
- [ ] Error path (`throw` → `return`)
- [ ] Ordering / string literal changed

## Run Loop (per mutant)
- [ ] Exactly one mutant applied at a time
- [ ] Filtered test run executed, outcome recorded (killed: test name + message)
- [ ] Filtered run executed > 0 tests — an empty filter match makes "survived" meaningless
- [ ] Mutant reverted before the next one

## Findings
- [ ] Every survivor has file:line + command output as evidence
- [ ] Test strengthened / added and the kill re-verified — or documented as equivalent
- [ ] No "tests are weak" claims without a concrete mutant

## Cleanup (fail-closed)
- [ ] All mutants reverted
- [ ] `git status --porcelain` matches the pre-run state
- [ ] Full suite green without mutants

## Report
- [ ] Mutant/result table included in the task report
