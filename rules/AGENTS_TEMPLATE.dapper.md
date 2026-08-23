# AGENTS.md — Dapper / Raw SQL Add-on

> Add-on to `AGENTS_TEMPLATE.md` for projects using **Dapper, ADO.NET, or raw SQL**.
> Copy the contents of this file to the end of your `AGENTS.md`.
> Architecture guardrails: copy the template [`DapperGuardRules.cs`](../tests/patterns/DapperGuardRules.cs)
> from this repository into your test project and adapt it (the link is relative to
> this repo's root and will not resolve in your project).

## Semantic Anchors (Dapper)

| Term | Meaning |
|------|---------|
| **Read-path** | Query path: `QueryAsync` / `Query` projections, no tracking concerns |
| **Write-path** | Command path: `ExecuteAsync` / `Execute` inside explicit transaction |

## Raw SQL

- ❌ C# string interpolation (`$"..."`) in SQL string — **FORBIDDEN**
- ❌ Concatenation (`+`) of user input with SQL — **FORBIDDEN**
- ✅ Parameterization (`@param`) — **MANDATORY**
- ✅ `IN` with dynamic list — via Table-Valued Parameter (TVP) or temp table, not `string.Join`

## Dapper Conventions

- ✅ Every `QueryAsync` / `ExecuteAsync` call must have `commandTimeout` (explicit or global default)
- ✅ Write operations wrapped in `IDbTransaction` — no standalone `ExecuteAsync` for mutations
- ✅ `TransactionScope` only with `TransactionScopeAsyncFlowOption.Enabled`
- ✅ `QueryMultiple` for batch queries instead of N separate calls

## Raw SQL Hygiene in EF Projects

- ✅ Even in EF projects `FromSqlRaw` / `ExecuteSqlRaw` require parameterization
- ❌ `FromSqlRaw` with `$"..."` — **FORBIDDEN** (use `FromSqlInterpolated`)

## Hard Prohibitions (Dapper)

- ❌ `string.Format`, `StringBuilder.Append(userInput)` in SQL
- ❌ Dynamic `ORDER BY` via concatenation without whitelist
