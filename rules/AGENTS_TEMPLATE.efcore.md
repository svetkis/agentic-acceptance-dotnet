# AGENTS.md — EF Core Add-on

> Add-on to `AGENTS_TEMPLATE.md` for projects using **Entity Framework Core**.
> Copy the contents of this file to the end of your `AGENTS.md`.
> Architecture guardrails: copy the template [`EfCoreGuardRules.cs`](../tests/patterns/EfCoreGuardRules.cs)
> from this repository into your test project and adapt it (the link is relative to
> this repo's root and will not resolve in your project).

## Semantic Anchors (EF Core)

| Term | Meaning |
|------|---------|
| **Read-path** | Query path: `.Select()` + `.AsNoTracking()` mandatory, change tracking forbidden |
| **Write-path** | Command path: full entity + change tracking required, `.AsNoTracking()` forbidden |

## Entity Framework

### Read-path (no exceptions)
- ❌ EF queries without `.Select()` — **FORBIDDEN**
- ❌ `.Include()` in read-path — **FORBIDDEN**
- ❌ `.FindAsync()` in read-path — **FORBIDDEN**
- ✅ `.Select()` + `.AsNoTracking()` — **MANDATORY**
- ✅ Nested collections in `.Select()` — extract to separate batch query

### Write-path
- ✅ Full entity loading allowed (change tracking required)
- ✅ `.FindAsync()` allowed only in write/command scenarios
- ❌ `.AsNoTracking()` in write-path — **FORBIDDEN**

### Raw SQL in EF
- ✅ `FromSqlInterpolated` — **MANDATORY** for parameterized SQL
- ❌ `FromSqlRaw` with `$"..."` interpolation — **FORBIDDEN**
- ✅ `ExecuteSqlRaw` only with static strings or parameters

## Hard Prohibitions (EF Core)

- ❌ Global `QueryTrackingBehavior.NoTracking` — only explicit `.AsNoTracking()` in read methods
