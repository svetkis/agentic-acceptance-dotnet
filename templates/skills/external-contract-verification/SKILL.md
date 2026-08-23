---
name: external-contract-verification
description: >
  Verify integrations against current external API contracts (payment providers,
  messengers, social platforms, LLM providers, webhooks, SDKs) before
  implementation. Prevents agents from inferring external behavior from training
  data or from existing project code.
---

# External Contract Verification — Skill

Optional interaction convention (agent-specific): when this skill is active, add
`🔗` to your STARTER_CHARACTER stack. The skill is fully usable without emoji markers.

## Purpose and Non-Goals

You are an integration verifier. External contracts (event names, field
semantics, signature formulas, retry and idempotency behavior) change without
notice. An agent writing integration code from memory reproduces an outdated or
invented contract that compiles and passes self-written tests — the classic
non-validating trap, applied to external systems.

This skill verifies contract-dependent code against authoritative sources,
captures evidence, builds canonical fixtures, and detects drift.

Non-goals: writing the integration itself, load-testing the provider, or
auditing internal business logic (see the audit skills for that).

## Applicability and Exclusions

- **Applies to:** webhook handlers, payment/messenger/social-platform
  integrations, third-party SDK usage, callback and signature verification,
  provider DTOs and dispatch constants, migration between provider API versions.
- **Excludes:** internal APIs fully owned by the project (their contract is the
  test suite), and purely cosmetic documentation work.

## Required Inputs

- The contract question: which provider and which concept (event, field,
  signature, retry policy, idempotency, version migration).
- Access to current provider documentation or official SDK sources.
- The code under verification: DTOs, dispatch constants, switch branches,
  stored fixtures, tests.

## Procedure

### 1. Define the contract question
- [ ] One concept per lookup: exact event names, signature verification,
  idempotency, field semantics, retry behavior, or version migration.

### 2. Capture evidence
- [ ] Provider and API/SDK version.
- [ ] Exact authoritative URL (official documentation or official SDK source);
  record when only a secondary source is available.
- [ ] Verification date.
- [ ] Required and optional fields, authenticity and freshness requirements.
- [ ] Retry, ordering, and idempotency behavior.
- [ ] Unresolved ambiguity — never silently resolved from memory.

**Hard rules:** never infer an event name, field, status, signature formula,
retry policy, or idempotency guarantee from existing project code — that code is
the thing under verification. Never send credentials, production payloads,
personal data, or secrets to documentation tools.

### 3. Create a canonical fixture
- [ ] Sanitize all identifiers and personal data; keep the provider's field
  names and nesting intact.
- [ ] Store under the test project's fixtures directory when it provides durable
  regression value.
- [ ] Do not generate the fixture from production DTOs or constants — that
  makes the test self-validating.

### 4. Define contract tests
At minimum verify:
- [ ] Canonical payload reaches the intended business path.
- [ ] Unknown/invalid event does not mutate state.
- [ ] Missing required field fails safely.
- [ ] Duplicate delivery is idempotent.
- [ ] Authenticity/freshness failure does not mutate state.
- [ ] Provider object identifiers resolve to the correct internal entity.
- [ ] For financial or security-sensitive events: server-to-server state
  verification where the provider supports it, plus a test of that path.

### 5. Detect drift
- [ ] Compare the verified contract with request/response DTOs, dispatch
  constants and switch branches, OpenAPI/callback schemas, stored fixtures,
  tests, and backlog wording. Any mismatch is a finding.
- [ ] Do not close the task until the canonical contract test is green.

## Evidence Requirements

Every finding MUST include:
1. **Exact file and line** of the drifting code: `src/Integrations/Payments/WebhookDto.cs:18`
2. **The authoritative source** contradicted: URL + version + verification date
3. **The exact contract point**: event name, field, signature formula, retry rule
4. **Action**: fix code, fix fixture, or re-verify source

**NEVER report:** "contract may be outdated" without naming the authoritative
source that proves it; findings based on training-data memory without a captured
source.

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
| **BLOCKER** | Signature/authenticity verification does not match the documented contract; money- or state-mutating path built on an unverified contract |
| **CRITICAL** | Event names, required fields, or idempotency behavior contradict the authoritative source |
| **MAJOR** | Fixture generated from production DTOs; retry/ordering assumptions undocumented |
| **MINOR** | Secondary (non-official) source used without recording that fact |

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Authoritative source captured and contradicts the code |
| **NEEDS_REVIEW** | Sources conflict or the contract remains ambiguous — needs human judgment |

## Outputs and Downstream Consumer

```markdown
## External Contract Verification — {provider}/{concept} — {date}

### Verdict
Status: VERIFIED | CHANGES_REQUESTED | NEEDS_REVIEW (sources conflict) | BLOCKED (no authoritative source)

### Evidence
- Authority: <official | secondary> <URL>
- Verified: <date/version>
- Canonical identifiers: <event/type names>

### Drift found
| ID | Severity | File:line | Contract point | Source | Action |
|----|----------|-----------|----------------|--------|--------|
| EC-001 | CRITICAL | ... | ... | ... | ... |

### Fixture and tests
- Fixture: <path | planned>
- Required tests: <list, with pass/fail>
```

**Downstream consumer:** Programmer Agent (fixes drift), Test Agent (contract
tests), Human (NEEDS_REVIEW conflicts).

## Trigger or Schedule

Before implementing contract-dependent code, and on any PR that touches
webhook handlers, provider DTOs, dispatch constants, or signature verification.

## Limitations and Expected False Positives

- Documentation itself can lag behind the real provider; a captured source
  lowers but does not eliminate risk. For financial events prefer
  server-to-server verification over trusting the webhook payload.
- Rate limits and non-deterministic sandbox behavior can block verification —
  report BLOCKED, do not fall back to memory.
- A passing contract test proves the code matches the captured contract, not
  that the provider never changes it again — schedule re-verification before
  releases.
