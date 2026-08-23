---
name: dba-audit
description: >
  DBA audit for .NET projects with EF Core + PostgreSQL. Runs on schedule or when
  EF models change; finds N+1, missing indexes, migration problems, NoTracking
  traps, incorrect data types and weak schema.
---

# DBA Audit — Skill

> **[ADAPT] Path profile:** paths like `src/*/Api/`, `src/*/Infrastructure/` used below are the DemoProject layered-layout example. Replace them with your project real layout (single project, Vertical Slice, ...) before the first run. See `ADAPTATION.md` §Path profile.


## Purpose and Non-Goals

- Persona: DBA auditor. Finds database performance and correctness problems that an agent could have introduced while optimizing "by eye".
- Covers N+1, missing indexes, migration problems, NoTracking traps, incorrect data types and weak schema.
- **Non-goals:** query tuning of the whole production workload; auditing Dapper / ADO.NET code paths (use `dba-audit-dapper`); executing migrations or changing the schema itself.

## Applicability and Exclusions

Default stack: **.NET + EF Core + PostgreSQL**. Before auditing, define the stack:
- **Dapper / ADO.NET (no EF Core)** → skip EF-specific checks (AsNoTracking, Include, FindAsync, migrations); use `dba-audit-dapper` instead
- **SQL Server instead of PostgreSQL** → adapt data types (`datetimeoffset` instead of `timestamptz`, `nvarchar` instead of `varchar`) and migration syntax
- **NoSQL (Mongo)** → skip migrations and relational checks, focus on indexes and document schema

## Required Inputs

- Read access to the repository: DbContext, entity configurations, repositories, migrations.
- The staged diff or the change set under review (model / migration / query changes).
- Optionally: DB logs or integration test output for N+1 detection, and access to run `EXPLAIN ANALYZE` for new queries.

## Procedure

### EF Core
- [ ] Read-path without projection: `.AsNoTracking()` is present (optional, for read-only scenarios)
- [ ] Write-path does NOT use `.AsNoTracking()` (exception: raw SQL, bulk API)
- [ ] `.Include()` is justified: no redundant chains of 3+ navigations without explicit comment
- [ ] `.FindAsync()` is used reasonably: acceptable for reading by PK, flag if used for lists/filters
- [ ] Nested collections are moved to batch queries (if applicable)

### Performance
- [ ] Check execution plan of new queries (EXPLAIN ANALYZE)
- [ ] Check presence of indexes on FKs and frequently used filters
- [ ] **Composite indexes** follow filter order (equality → range → includes)
- [ ] **`INCLUDE` columns** for covering indexes where needed (avoid Key Lookup)
- [ ] Check for absence of N+1 (through logs or integration tests)
- [ ] Check that there is no `client evaluation`

### Data Structure (Schema)
- [ ] **Data types are adequate:**
  - Money → `decimal`/`numeric`, not `float`/`double`
  - Strings with limit → `varchar(N)`, not `text` without reason and not `varchar(max)` for everything
  - Dates → `timestamp with time zone` (timestamptz), not `timestamp without time zone`
  - JSON → `jsonb`, not `json` (if index or search is needed)
  - UUID/GUID → `uuid`, not `varchar(36)`
  - Enum → `smallint` + lookup table or `text` with CHECK constraint
- [ ] **Nullable / NOT NULL:** required fields are marked `IsRequired()` / `NOT NULL`. No situation where ALL columns are nullable by default
- [ ] **Constraints:**
  - `PRIMARY KEY` exists on every table
  - `UNIQUE` on natural keys (email, username, external_id)
  - `CHECK` constraints on business rules (positive sums, ranges)
- [ ] **Relations and cascades:** `ON DELETE` is set explicitly. No accidental cascade delete on important data. FKs used in joins/filters are indexed — verify with `EXPLAIN`; a missing index on an unused FK is not a defect
- [ ] **Soft delete:** if spec has soft delete → there is `IsDeleted` / `DeletedAt`. Unique indexes account for soft delete (partial unique index `WHERE IsDeleted = false`)
- [ ] **Audit fields:** `CreatedAt`, `UpdatedAt` are present (if adopted in project). `CreatedBy` / `UpdatedBy` — if audit is required
- [ ] **Schema naming:** tables, columns, constraints — `snake_case`. Indexes with prefix `ix_`, unique — `ux_`, PK — `pk_`
- [ ] **Schema sanity:** no "god tables" with 50+ columns. JSONB is used reasonably (not for everything). No excessive denormalization without justification
- [ ] **Partitioning:** considered for tables >10M records or time-series

### Migrations
- [ ] Check that migrations are reversible (down method is implemented or safe)
- [ ] Check that Raw SQL in migrations is PostgreSQL syntax
- [ ] Check that renaming column is done via Add + Drop, not Rename (locks)
- [ ] Check that indexes are created `CONCURRENTLY` if table is large

## Evidence Requirements

Every finding MUST include:
1. **Exact file and line:** `src/Infrastructure/OrderRepository.cs:42`
2. **Code / SQL quote:** exact query or EF chain (3-5 lines)
3. **Justification:** why this is a problem (from the rules above)
4. **Fix:** specific action or SQL code

**NEVER report:**
- "Missing index" without specifying exact table and column
- "N+1" without a code quote with a loop + query inside
- "Migration is dangerous" without a quote of the Up method
- Problems that you cannot confirm with code or query plan

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
- **BLOCKER** — breaks production: migration without `CONCURRENTLY` on a large table (lock), `ON DELETE CASCADE` on important data, data loss in migration
- **CRITICAL** — severe production risk short of data loss: blocking migration on a hot table, accidental cascade delete path to critical data
- **MAJOR** — performance or correctness degradation: N+1, missing index on FK, `varchar(max)` for email, `float` for money
- **MINOR** — suboptimal: redundant index, illogical composite index order, missing `INCLUDE`

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Proven by evidence: file:line, reproduction, command output |
| **NEEDS_REVIEW** | Investigation signal; requires human judgment before action |

Project-specific mapping: **CONFIRMED** — definitely a bug: query generates 100 SQLs
instead of 1, `text` instead of `varchar`, migration without down, `ON DELETE CASCADE`
on Order→OrderItem. **NEEDS_REVIEW** — requires verification: justification of
`.Include()` (is it needed?), composite index optimality, necessity of partitioning.

## Outputs and Downstream Consumer

```markdown
## DBA Audit — {date}

### BLOCKER
- [ ] [CONFIRMED] Migration `20260615_AddOrderIndex` creates index without `CONCURRENTLY` on a 50M record table
  → `src/Infrastructure/Migrations/20260615_AddOrderIndex.cs:12`
  → Code: `migrationBuilder.CreateIndex("ix_orders_status", "orders", "status")`
  → Fix: `CREATE INDEX CONCURRENTLY ix_orders_status ON orders (status)`

- [ ] [CONFIRMED] `ON DELETE CASCADE` on `OrderItems → Orders`
  → `src/Infrastructure/Configuration/OrderItemConfig.cs:15`
  → Code: `.OnDelete(DeleteBehavior.Cascade)`
  → Fix: `.OnDelete(DeleteBehavior.Restrict)` + soft delete

### CRITICAL
- [ ] {description} → {file:line / migration}

### MAJOR
- [ ] [CONFIRMED] N+1: `foreach` + `orderRepository.GetById()` generates 50 queries
  → `src/Application/Handlers/BulkUpdateHandler.cs:28`
  → Code: `foreach (var id in ids) { var order = await _repo.GetById(id); ... }`
  → Fix: `await _repo.GetByIds(ids)` + batch update

- [ ] [CONFIRMED] `Price` is stored as `float` instead of `decimal`
  → `src/Domain/Entities/Product.cs:12`
  → Code: `public float Price { get; set; }`
  → Fix: `public decimal Price { get; set; }` + migration `AlterColumn`

- [ ] [CONFIRMED] No index on FK `OrderItems.OrderId`
  → `src/Infrastructure/Configuration/OrderItemConfig.cs`
  → Evidence: `EXPLAIN ANALYZE SELECT * FROM order_items WHERE order_id = '...'` → Seq Scan
  → Fix: `CREATE INDEX ix_order_items_order_id ON order_items (order_id)`

### MINOR
- [ ] [NEEDS_REVIEW] Composite index `ix_orders_status_created` — column order may be suboptimal
  → `src/Infrastructure/Migrations/20260610_AddCompositeIndex.cs:8`
  → Code: `.HasIndex(["Status", "CreatedAt", "UserId"])`
  → Fix: if filter is only by `Status` — order is ok. If range by `CreatedAt` — `Status, CreatedAt` is correct.

### Data Structure
- [ ] {description} → {table:column / constraint}

### Performance
- [ ] {description} → {query / migration}

### Migrations
- [ ] {description} → {MigrationName}
```

**Output to:** the user / developer who made the model or migration change; BLOCKER/CRITICAL findings feed the backlog and must be fixed before release.

## Trigger or Schedule

Runs on schedule or when changes are made to:
- `src/*/Infrastructure/DbContext`
- `src/*/Domain/Entities`
- `src/*/Infrastructure/Migrations`
- New queries in repositories

## Limitations and Expected False Positives

- `.Include()` justification, composite index order, `INCLUDE` columns, and partitioning are context-dependent — without a query plan they are **NEEDS_REVIEW** signals, not defects.
- `.AsNoTracking()` is irrelevant for projections; flagging it there is a false positive.
- Schema conventions (naming, audit fields, soft delete) apply only if adopted by the project — verify against project rules first.

> Optional interaction convention (agent-specific): some agents add `🗄️` to their starter-character stack while this skill is active. Not required — the skill is fully usable without emoji.
