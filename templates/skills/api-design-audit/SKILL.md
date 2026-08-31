---
name: api-design-audit
description: >
  API design auditor. Checks HTTP statuses, pagination, sorting,
  OpenAPI contracts, DTO consistency, error handling, rate limiting.
  Runs when API endpoints, DTOs, or controllers change.
---

> **Repo-internal / for methodology archive.** This skill describes a methodological guardrail inside the `agentic-acceptance-dotnet` repository. The methodological core (HTTP statuses, pagination, DTO consistency, error handling, rate limiting) applies to any project with an HTTP API, but the examples are illustrations, not a universal template.

# API Design Audit Agent

> **[ADAPT] Path profile:** paths like `src/*/Api/`, `src/*/Infrastructure/` used below are the DemoProject layered-layout example. Replace them with your project real layout (single project, Vertical Slice, ...) before the first run. See `ADAPTATION.md` §Path profile.


> Optional interaction convention (agent-specific): some agents mark an active
> skill with an emoji in their status stack (e.g., `🎨` for this skill, prefixed
> with `♻️` on re-read). The skill is fully usable without it.

## Purpose and Non-Goals

You are an API design auditor. Your task is to find problems in contracts
that an agent could have introduced, focusing on functionality:
wrong HTTP statuses, broken pagination, unclear errors,
breaking changes in DTOs.

You don't check business logic, you check the **contract** between backend and consumer.

Non-goals:
- Business logic correctness (calculation results).
- Production performance (latency, throughput) — that's `performance-audit`.
- Security (authorization, SQL injection) — that's `security-audit`.

## Applicability and Exclusions

Before auditing, define the API type:
- **REST / Web API** → check HTTP statuses, pagination, OpenAPI, JSON DTO
- **gRPC** → check proto contracts, backward compatibility, status codes
- **GraphQL** → check schema, query depth, N+1 in resolvers
- **Webhook / Callback API** → check retry policy, idempotency keys, timeout handling
- **No HTTP (Worker, Desktop)** → this skill is not applicable (Won't do)

## Required Inputs

- Repo access: controllers / handlers, DTOs, OpenAPI spec, and the diff under review.
- The API type and conventions of the project (see adaptation above).
- A way to verify behavior: running app, tests, or curl/HTTP request examples.

## Procedure

### A. HTTP Statuses and Errors (REST / Web API)

#### Statuses
- [ ] `400` — invalid request (bad JSON, missing required field, malformed parameter)
- [ ] `401` — not authenticated (missing or invalid token)
- [ ] `403` — no permission (don't confuse with 401). Example: token is valid, but access to another user's resource is denied
- [ ] `404` — resource not found (don't confuse with 400). Example: `/orders/999` does not exist
- [ ] `409` — conflict (duplicate, concurrent modification, optimistic locking)
- [ ] `422` — business rule validation (don't confuse with 400). Example: "delivery date cannot be in the past"
- [ ] `429` — rate limit, with `Retry-After` or `X-RateLimit-*` headers
- [ ] `500` — only for unexpected errors. Never with stack trace or SQL in the body

#### Error responses
- [ ] Error body is structured (ProblemDetails / RFC 7807 or custom format), not plain text
- [ ] `message` is understandable to the end user (if displayed in UI), contains no internal details
- [ ] `code` or `type` is present for programmatic handling (i18n keys, retry logic)
- [ ] Validation errors are bound to fields (`fieldErrors: [{"field":"email","message":"..."}]`)
- [ ] No PII leak in error messages (email, phone in `message`)

### B. Pagination and Sorting

#### Pagination
- [ ] Default `page` / `limit` or `cursor` are set and documented
- [ ] `page=1` and `page=0` don't cause duplication or skipped records
- [ ] `limit` has a max cap (e.g., 100) to prevent self-DDoS
- [ ] Offset-based: response contains `total` + `page` + `limit`
- [ ] Cursor-based: response contains `nextCursor` / `hasMore` (for large datasets)
- [ ] `sort` / `orderBy` parameter supports a whitelist (not arbitrary field — SQL injection)

#### Sorting
- [ ] Default sort is logical (usually `createdAt desc` for lists)
- [ ] Default sort is **stable** — not `ORDER BY id` on UUID without a secondary key
- [ ] Direction is explicit: `asc` / `desc`, not dependent on implicit order
- [ ] Composite sort is documented (`sort=createdAt:desc,id:asc`)

### C. API Contracts and DTOs

#### OpenAPI / Swagger
- [ ] New endpoints have descriptions (`summary`, `description`)
- [ ] Parameters have `example` and `schema` (type, nullable, min/max)
- [ ] Response schemas are described for all statuses (200, 400, 404, 500)
- [ ] Breaking changes detected: field removal, type change, making previously optional field required

#### DTO consistency
- [ ] API returns DTOs/records, not Entities directly (security + encapsulation)
- [ ] Naming is consistent: `OrderResponse`, `CreateOrderRequest`, `OrderListItem` — not `OrderDto` everywhere
- [ ] `id` field in response matches the type in URL (`/orders/{id}` — `id: uuid`, not `id: int` in one place and `uuid` in another)
- [ ] Dates in ISO 8601 with timezone (`2024-01-15T10:30:00Z`), not local time, not unix timestamp without context
- [ ] Enums in JSON are strings (`"Status": "Confirmed"`), not numbers (except explicit numeric enum contract)

#### Versioning
- [ ] Breaking changes are versioned (URL path `/v2/...`, header `Accept: application/vnd.api.v2+json`, or query `?api-version=2`)
- [ ] Deprecated endpoints are marked in OpenAPI (`deprecated: true`) with an alternative specified

### D. Rate Limiting and Protection
- [ ] Rate limiting is configured on public endpoints (anonymous, registration, search)
- [ ] Headers `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `Retry-After` are present in 429 response
- [ ] Bulk endpoints (import, mass update) have separate limits from regular ones
- [ ] CORS policy does not allow `*` in production for sensitive endpoints

### E. Empty and error states (from API perspective)
- [ ] Empty list → `200 OK` + `[]` or `{ "items": [], "total": 0 }`, not `404 Not Found`
- [ ] Empty search result → `200 OK` + empty array, not an error
- [ ] On 500 — ProblemDetails with `traceId` / `requestId` for logs, but without stack trace

## Evidence Requirements

Every finding MUST include:
1. **Endpoint:** `GET /api/orders/{id}` or `POST /api/orders`
2. **Code quote:** 3-5 lines from controller / handler / DTO
3. **Actual response:** exact JSON or HTTP status returned by the API
4. **Expected:** what should be according to the rules
5. **How to verify:** curl / HTTP request example

**NEVER report:**
- "Pagination is unpredictable" without a specific example (`page=2` returned duplicates of the first page)
- "Status is wrong" without a code quote and expected vs actual
- "DTO is bad" without a specific field and explanation why
- Problems that you cannot confirm with code or a test request

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

Severity describes impact and urgency:

| Severity | Meaning |
|----------|---------|
| **BLOCKER** | Change/release must not proceed; immediate action required |
| **CRITICAL** | High impact; fix in the current iteration |
| **MAJOR** | Degradation or defect; schedule the fix |
| **MINOR** | Improvement; backlog |

Skill-specific calibration:
- **BLOCKER** — client cannot work with API (500 on critical endpoint, breaking change without versioning, missing CORS on public API)
- **CRITICAL** — contract violation affecting real clients now (500 with stack trace, breaking DTO change shipped unversioned)
- **MAJOR** — confusion, data loss, wrong handling (wrong HTTP status, pagination with duplicates, missing `total`)
- **MINOR** — inconvenience, convention mismatch (illogical DTO naming, missing example in OpenAPI)

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Proven by evidence: file:line, reproduction, command output |
| **NEEDS_REVIEW** | Investigation signal; requires human judgment before action |

Skill-specific calibration:
- **CONFIRMED** — specific bug found: 500 with stack trace, `page=2` returned duplicates, `401` instead of `403`, breaking change without versioning
- **NEEDS_REVIEW** — subjective assessment: "logic" of sorting, "clarity" of message. Requires human judgment.

## Outputs and Downstream Consumer

Report format:

```markdown
## API Design Audit — {date}

### BLOCKER
- [ ] [CONFIRMED] 500 on `POST /api/orders` returns stack trace to client
  → `src/Api/OrdersController.cs:88`
  → Code: `return StatusCode(500, ex.ToString());`
  → Evidence: `"detail": "System.NullReferenceException at..."`
  → Fix: `return Problem(title: "Internal error", statusCode: 500, instance: traceId)`

- [ ] [CONFIRMED] Breaking change: field `customerEmail` removed from `OrderResponse` without versioning
  → `src/Api/Dto/OrderResponse.cs:-15`
  → Evidence: field existed in v1, removed in current PR
  → Fix: return field with `[Obsolete]`, add `/v2/orders` with new DTO

### MAJOR
- [ ] [CONFIRMED] Pagination duplicates records: `page=1` and `page=2` contain the same `orderId`
  → `src/Infrastructure/OrderRepository.cs:42`
  → Code: `.OrderBy(o => o.CreatedAt)` without secondary key
  → Fix: `.OrderBy(o => o.CreatedAt).ThenBy(o => o.Id)`

- [ ] [CONFIRMED] `401` instead of `403` when accessing another user's order
  → `src/Api/OrdersController.cs:55`
  → Code: `if (order.OwnerId != userId) return Unauthorized();`
  → Fix: `return Forbid()` (403)

- [ ] [CONFIRMED] Empty order list returns `404 Not Found`
  → `src/Api/OrdersController.cs:30`
  → Code: `if (!orders.Any()) return NotFound();`
  → Fix: `return Ok(new PagedResponse { Items = [], Total = 0 });`

- [ ] [NEEDS_REVIEW] Default sort `CreatedAt desc` — client doesn't see old records
  → `src/Api/OrdersController.cs:42`
  → Code: `OrderByDescending(o => o.CreatedAt)`
  → Evidence: spec requires "oldest first", but code has `desc`
  → Fix: `OrderBy(o => o.CreatedAt)` or add `sort` parameter

### MINOR
- [ ] [NEEDS_REVIEW] {naming convention mismatch for DTO} → `{file}`
- [ ] [CONFIRMED] {missing example in API schema for field} → `{file}`
```

Consumers:
- **Input from:** Code Review Agent (diff with API changes), Task Compliance Agent (feature scope)
- **Output to:** Programmer Agent (contract fixes), Human supervisor (NEEDS_REVIEW findings)

## Trigger or Schedule

Runs when changes touch API endpoints, DTOs, controllers, or the OpenAPI spec.

## Limitations and Expected False Positives

- Does not check business logic (correctness of calculations).
- Does not check production performance (latency, throughput) — that's `performance-audit`.
- Does not check security (authorization, SQL injection) — that's `security-audit`.
- Convention mismatches (naming, sort "logic", message "clarity") are subjective
  and expected as NEEDS_REVIEW signals, not defects.
