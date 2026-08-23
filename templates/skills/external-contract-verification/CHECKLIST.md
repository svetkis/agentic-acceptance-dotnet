# External Contract Verification — Checklist

## Before Starting
- [ ] Contract question defined: one provider, one concept
- [ ] Access to authoritative documentation or official SDK sources
- [ ] Current code under review identified (DTOs, dispatch, signature checks)

## Evidence Capture
- [ ] Official source URL recorded (or secondary source explicitly marked)
- [ ] API/SDK version and verification date recorded
- [ ] Event names, required/optional fields captured exactly
- [ ] Signature/freshness requirements captured
- [ ] Retry, ordering, idempotency behavior captured
- [ ] No credentials/PII sent to documentation tools
- [ ] No contract point inferred from existing project code

## Canonical Fixture
- [ ] Fixture sanitized (identifiers, personal data)
- [ ] Provider field names and nesting preserved
- [ ] Fixture stored in the test project (durable regression value)
- [ ] Fixture NOT generated from production DTOs or constants

## Contract Tests
- [ ] Canonical payload reaches the business path
- [ ] Unknown/invalid event does not mutate state
- [ ] Missing required field fails safely
- [ ] Duplicate delivery is idempotent
- [ ] Authenticity/freshness failure does not mutate state
- [ ] Provider identifiers resolve to correct internal entities
- [ ] Server-to-server verification tested (financial/security events)

## Drift Detection
- [ ] DTOs compared with verified contract
- [ ] Dispatch constants / switch branches compared
- [ ] OpenAPI / callback schemas compared
- [ ] Stored fixtures and tests compared
- [ ] Backlog/task wording compared
- [ ] Every mismatch reported as a finding with source

## Report
- [ ] Verdict: VERIFIED / CHANGES_REQUESTED / NEEDS_REVIEW / BLOCKED
- [ ] Findings follow the finding schema (ID, severity, confidence, evidence)
- [ ] Task not closed while a contract test is red
