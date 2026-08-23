---
name: dba-audit-dapper
description: >
  DBA audit for .NET projects using Dapper / ADO.NET / raw SQL. Runs on schedule
  or when repositories / SQL queries change; finds SQL injections, missing
  timeouts, unoptimized queries and schema problems.
---

# DBA Audit — Dapper / Raw SQL

> **[ADAPT] Path profile:** paths like `src/*/Api/`, `src/*/Infrastructure/` used below are the DemoProject layered-layout example. Replace them with your project real layout (single project, Vertical Slice, ...) before the first run. See `ADAPTATION.md` §Path profile.


## Purpose and Non-Goals

- Persona: DBA auditor. Finds database performance and correctness problems that an agent could have introduced while optimizing "by eye" or copying patterns from EF projects.
- Covers SQL injections, missing timeouts, unoptimized queries and schema problems.
- **Non-goals:** auditing EF Core code paths (use `dba-audit`); production-wide query tuning; executing migrations or changing the schema itself.

## Applicability and Exclusions

- **EF Core (no Dapper)** → use `templates/skills/dba-audit/` (EF-specific audit)
- **SQL Server instead of PostgreSQL** → adapt data types (`datetimeoffset` instead of `timestamptz`, `nvarchar` instead of `varchar`) and syntax
- **NoSQL (Mongo)** → skip relational checks, focus on indexes and document schema

## Required Inputs

- Read access to the repository: repositories, SQL files, migrations, DTOs used in DB filtering.
- The staged diff or the change set under review (repository / SQL / migration changes).
- Optionally: DB logs or integration test output for N+1 detection, and access to run `EXPLAIN ANALYZE` / `SET STATISTICS` for new queries.

## Procedure

### SQL Injection & Parameterization
- [ ] All SQL queries are parameterized (`@param`), no interpolation / concatenation of user input
- [ ] `string.Format`, `StringBuilder.Append(input)` in SQL — forbidden
- [ ] Dynamic `IN` — via TVP or temp table, not `string.Join`
- [ ] Dynamic `ORDER BY` — via whitelist mapping, not concatenation

### Dapper Hygiene
- [ ] `QueryAsync` / `ExecuteAsync` have `commandTimeout` (explicit or global default)
- [ ] `QueryMultiple` is used for batch queries instead of N separate round-trips
- [ ] `TransactionScope` — only with `TransactionScopeAsyncFlowOption.Enabled`
- [ ] Write operations are wrapped in `IDbTransaction`

### Performance
- [ ] Check execution plan of new queries (EXPLAIN ANALYZE / SET STATISTICS)
- [ ] Check presence of indexes on FKs and frequently used filters
- [ ] **Composite indexes** follow filter order (equality → range → includes)
- [ ] **`INCLUDE` columns** for covering indexes where needed (avoid Key Lookup / Bookmark Lookup)
- [ ] Check for N+1 via logs or integration tests
- [ ] No `SELECT *` in production queries — only needed columns

### Data Structure (Schema)
- [ ] **Data types are adequate:**
  - Money → `decimal`/`numeric`, not `float`/`double`
  - Strings with limit → `varchar(N)` / `nvarchar(N)`, not `text` / `nvarchar(max)` for everything
  - Dates → `datetimeoffset` (SQL Server) or `timestamptz` (PostgreSQL)
  - JSON → `jsonb` (PostgreSQL) or `NVARCHAR` with CHECK constraint (SQL Server)
  - UUID/GUID → `uniqueidentifier` (SQL Server) or `uuid` (PostgreSQL)
- [ ] **Nullable / NOT NULL:** required fields are marked `NOT NULL`
- [ ] **Constraints:**
  - `PRIMARY KEY` exists on every table
  - `UNIQUE` on natural keys (email, username, external_id)
  - `CHECK` constraints on business rules (positive amounts, ranges)
- [ ] **Relations and cascades:** `ON DELETE` is explicit. No accidental cascade delete on important data. FK is indexed
- [ ] **Soft delete:** if spec requires soft delete → `IsDeleted` / `DeletedAt` exists. Unique indexes account for soft delete
- [ ] **Audit fields:** `CreatedAt`, `UpdatedAt` are present (if adopted by project)
- [ ] **Schema naming:** tables, columns, constraints — per project convention. Indexes with `ix_` prefix, unique — `ux_`, PK — `pk_`
- [ ] **Schema sanity:** no "god tables" with 50+ columns. JSON used reasonably

## Evidence Requirements

Every finding MUST include:
1. **Exact file and line:** `src/Infrastructure/OrderRepository.cs:42`
2. **Code / SQL quote:** exact query (3-5 lines)
3. **Rationale:** why this is a problem (per rules above)
4. **Fix:** concrete action or SQL code

**NEVER report:**
- "Missing index" without specifying exact table and column
- "N+1" without code quote showing loop + query inside
- Problems you cannot confirm with code or query plan

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
| **BLOCKER** | Change/release must not proceed; immediate action required |
| **CRITICAL** | High impact; fix in the current iteration |
| **MAJOR** | Degradation or defect; schedule the fix |
| **MINOR** | Improvement; backlog |

Project-specific mapping:
- **BLOCKER** — breaks prod: SQL injection, migration without `CONCURRENTLY` on large table, `ON DELETE CASCADE` on important data, data loss
- **CRITICAL** — severe production risk short of data loss: missing transaction on multi-step write, dynamic SQL from user-controlled identifiers without whitelist
- **MAJOR** — perf or correctness degradation: missing index on FK, `float` for money, N+1, missing timeout on long-running query
- **MINOR** — suboptimal: redundant index, `SELECT *`, illogical composite index order

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Proven by evidence: file:line, reproduction, command output |
| **NEEDS_REVIEW** | Investigation signal; requires human judgment before action |

Project-specific mapping: **CONFIRMED** — definite bug: interpolation in SQL,
`string.Format` in query, `float` for money, missing index on FK. **NEEDS_REVIEW** —
requires verification: justification of composite index, necessity of `INCLUDE`
columns, batch query optimality.

## Outputs and Downstream Consumer

```markdown
## DBA Audit (Dapper) — {date}

### BLOCKER
- [ ] [CONFIRMED] SQL injection: string interpolation in `OrderRepository.cs`
  → `src/Infrastructure/OrderRepository.cs:15`
  → Code: `$"SELECT * FROM orders WHERE id = {orderId}"`
  → Fix: `"SELECT * FROM orders WHERE id = @orderId"` + `new { orderId }`

### CRITICAL
- [ ] {description} → {file:line}

### MAJOR
- [ ] [CONFIRMED] Missing index on FK `OrderItems.OrderId`
  → `src/Infrastructure/OrderItemRepository.cs`
  → Evidence: `EXPLAIN ANALYZE SELECT * FROM order_items WHERE order_id = '...'` → Seq Scan
  → Fix: `CREATE INDEX ix_order_items_order_id ON order_items (order_id)`

- [ ] [CONFIRMED] `Price` stored as `float` instead of `decimal`
  → `src/Domain/Entities/Product.cs:12`
  → Code: `public float Price { get; set; }`
  → Fix: `public decimal Price { get; set; }`

### MINOR
- [ ] [NEEDS_REVIEW] `SELECT *` in `GetAllOrders` — extra columns loaded into memory
  → `src/Infrastructure/OrderRepository.cs:28`
  → Fix: explicitly list needed columns
```

**Output to:** the user / developer who made the repository or SQL change; BLOCKER/CRITICAL findings feed the backlog and must be fixed before release.

## Trigger or Schedule

Runs on schedule or when changes are made to:
- `src/*/Infrastructure/*Repository.cs`
- `src/*/Infrastructure/Sql/`
- New stored procedures / views / migrations
- New DTOs with fields used in DB filtering

## Limitations and Expected False Positives

- Composite index justification, `INCLUDE` columns, and batch query optimality are context-dependent — without a query plan they are **NEEDS_REVIEW** signals, not defects.
- `SELECT *` in a one-off maintenance script or admin tool is usually noise, not a finding.
- Schema conventions (naming, audit fields, soft delete) apply only if adopted by the project — verify against project rules first.

> Optional interaction convention (agent-specific): some agents add `🧵` to their starter-character stack while this skill is active. Not required — the skill is fully usable without emoji.
