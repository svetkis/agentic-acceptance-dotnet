# Code Review — Checklist

## Before Start
- [ ] Staged diff obtained (`git diff --cached`)
- [ ] Task context known (backlog item / spec)
- [ ] Skill activated automatically before `git commit` or explicitly via `/skill:code-review`

## Pre-commit / Trigger
- [ ] Staged changes include backend source files
- [ ] Frontend-only changes are skipped (out of scope; stack-specific skill per ADAPTATION.md)
- [ ] When staged diff is empty, agent reports nothing and does not block commit
- [ ] Agent does NOT run `git commit` itself

## Security
- [ ] XSS: query params, returnUrl stripped (if applicable)
- [ ] ID leak: internal ClientId/OwnerId not returned in API
- [ ] JWT / credentials not logged
- [ ] Rate limiting on public endpoints (if applicable)
- [ ] Constant-time comparison for hashes

## ORM / Data Access
- [ ] If project uses ORM with change tracking: read-path without projection has read-only mode; projections to DTO — not required.
- [ ] Write-path: no read-only mode. Exception: raw SQL / bulk API.
- [ ] Eager loading justified; no over-fetched chains without projection
- [ ] Raw SQL parameterized; no interpolation/concatenation in SQL

## Architecture
- [ ] If project uses layered architecture — Domain/core does not depend on Infrastructure
- [ ] API returns DTO/records, not Entities
- [ ] Async methods accept CancellationToken (if project uses async)

## Tests
- [ ] Every `fix:` commit has a regression test per project convention
- [ ] New functionality covered by tests
- [ ] No empty placeholder tests

## Code Quality
- [ ] No `async void` (if applicable)
- [ ] No empty `catch { }`
- [ ] Dates in agreed format (UTC / ISO 8601 / project convention)
- [ ] Consistent naming for DB entities / columns

## Business Logic Duplication (Semantic)
> Automated tests catch only literal copying. This block is for humans.
- [ ] New validation/status calculation — does similar logic exist in other services?
- [ ] Can the rule be extracted into a Domain Service / Value Object / numbered business rule ID?
- [ ] No divergence: in one place `>= 100`, in another `> 100`

## Cross-Layer Drift Checks
- [ ] DTO / domain event change updated for all consumers
- [ ] Permission check change in API does not leave bypass in domain/job
- [ ] Write operation invalidates cache on all levels
- [ ] Date format change agreed between UI, API, DB, jobs
- [ ] Domain model change accompanied by migration / backward compatibility
- [ ] Business rule not removed/smeared with contradictory semantics after refactor
- [ ] Every finding evaluated against: "which end-to-end invariant breaks though unit tests are green?"

## Report Format

```markdown
## Code Review — {date}

### BLOCKER
- [ ] {description} → {file:line}

### CRITICAL
- [ ] {description} → {file:line}

### MAJOR
- [ ] {description} → {file:line}

### Verdict
- [ ] APPROVED
- [ ] APPROVED_WITH_NITS
- [ ] CHANGES_REQUESTED
```
