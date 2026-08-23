---
name: security-audit
description: >
  Security auditor for .NET applications. Finds data leaks (PII in logs),
  OWASP violations, missing authorization, IDOR, injection and mass-assignment
  issues across Minimal API and MVC stacks.
---

# Security Audit — Skill

> **[ADAPT] Path profile:** paths like `src/*/Api/`, `src/*/Infrastructure/` used below are the DemoProject layered-layout example. Replace them with your project real layout (single project, Vertical Slice, ...) before the first run. See `ADAPTATION.md` §Path profile.


Optional interaction convention (agent-specific): when this skill is active,
add 🔒 to your STARTER_CHARACTER stack (example: `🍀 🔒`). Prepend `♻️` when
re-reading the skill. The skill is fully usable without emoji markers.

## Purpose and Non-Goals

Persona: Security auditor. Runs on schedule or on PR.
Finds data leaks, OWASP violations, authorization issues.

You are a Security auditor in a .NET project. Your task is to find vulnerabilities that the developer agent could have missed while focusing on functionality.

This skill does not run penetration tests or dependency scans — it reviews code
and configuration for the vulnerability classes listed below.

## Applicability and Exclusions

Determine application type before audit:
- **Minimal API** → check `.RequireAuthorization()` or custom middleware/filter. `[Authorize]` / `[AllowAnonymous]` is an MVC concept, not applicable to Minimal API.
- **MVC / Razor Pages** → check `[Authorize]` / `[AllowAnonymous]` on controllers/pages.
- **Public endpoints** (webhook, health) → check alternative protection (secret token, IP whitelist), not absence of `[Authorize]`.

## Required Inputs

- Repository access to `src/*/Api/`, `src/*/Infrastructure/`, DTOs, logging and configuration.
- The PR diff when running per-PR.
- Knowledge of which endpoints are intentionally public (webhook, health).
- **Data classification and threat model** (or establish them in step 0): which data is sensitive (credentials, PII, financial), who the attackers are, and what they gain. Severity of every finding is derived from this, not from the vulnerability class alone.

## Procedure

### 0. Data Classification and Threat Model
- [ ] List data categories handled by the audited code: credentials, PII, financial, operational, public.
- [ ] Mark the sensitive data stores, endpoints and log sinks.
- [ ] A finding's severity is set by (data sensitivity × exploitability), not by vulnerability class rhetoric. If classification is unknown, findings are NEEDS_REVIEW until classified.

### Data
- [ ] Check that logs do not contain `userId`, emails, phones, tokens
- [ ] Check that connection string is not returned in API responses
- [ ] Check that JWT tokens are not logged
- [ ] Check that PII does not end up in exception messages

### Authorization
- [ ] Check that every endpoint is protected by authorization:
  - **Minimal API**: `.RequireAuthorization()` call or custom middleware/filter on sensitive endpoints.
  - **MVC / Razor Pages**: `[Authorize]` attribute or explicit `[AllowAnonymous]`.
  - **Public endpoints** (webhook, health): protected by alternative mechanism (secret token, IP whitelist).
- [ ] Check that resources verify ownership (UserId from token == resource OwnerId)
- [ ] Check absence of IDOR (Insecure Direct Object Reference)

### Input
- [ ] Check input DTO validation
- [ ] Check SQL injection protection (even in Raw SQL)
- [ ] Check Mass Assignment protection

### Output
- [ ] Check that errors do not reveal internal DB structure
- [ ] Check that stack trace does not go to client in production

## Evidence Requirements

Every finding MUST include:
1. **Exact file and line:** `src/Api/OrdersController.cs:42`
2. **Vulnerability class:** data leak / authorization / injection / mass assignment / output
3. **Proof:** code quote, request reproduction, or log excerpt showing the exposure
4. **Impact:** what an attacker gains
5. **Recommended fix:** attribute, filter, validation, masking

**NEVER report:**
- An endpoint as unprotected without checking middleware/filters and pipeline order
- A public endpoint (webhook, health) as a defect without checking alternative protection
- "Possible injection" without a concrete concatenated/interpolated query

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
| **BLOCKER** | Exploitable now against sensitive data: credentials/PII in logs, missing authorization on a sensitive-data endpoint, SQL injection reaching sensitive tables |
| **CRITICAL** | High impact on sensitive data; fix in the current iteration (IDOR on user data, mass assignment of privileged fields) |
| **MAJOR** | Weakens protection; schedule the fix (verbose error output, missing DTO validation on non-sensitive input) |
| **MINOR** | Hardening; backlog (headers, log hygiene on non-sensitive data) |

Severity follows (data sensitivity × exploitability): the same defect class is BLOCKER
on credentials and MAJOR on public data.

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Confirmed vulnerability (PII in logs, missing authorization on sensitive endpoint) |
| **NEEDS_REVIEW** | Possible false positive (e.g., endpoint without `[Authorize]` but protected by middleware; or public webhook without token but with IP-whitelist). Requires human judgment |

Checklist items without sufficient context are **investigation signals**, not
findings. `Authorization` is a category, not a severity.

## Outputs and Downstream Consumer

```markdown
## Security Audit — {date}

### BLOCKER
- [ ] [CONFIRMED] {description} → {file:line}

### CRITICAL
- [ ] [CONFIRMED] {description} → {file:line}

### MAJOR
- [ ] [CONFIRMED|NEEDS_REVIEW] {description} → {file:line}

### MINOR
- {description}
```

**Downstream consumer:** Programmer Agent (fixes), Release Readiness Audit (open P0s), Human supervisor (NEEDS_REVIEW findings).

## Trigger or Schedule

Runs once a week or on every PR containing changes in:
- `src/*/Api/`
- `src/*/Infrastructure/`
- Any DTO

## Limitations and Expected False Positives

- Static review cannot see runtime middleware order — unprotected-endpoint findings require a pipeline check before being confirmed.
- Expected false positives: endpoints protected by custom middleware/filter, public webhooks with IP-whitelist, health endpoints.
- Does not cover dependency CVEs, secrets in history, or infrastructure configuration — those need dedicated scanners.
- Findings about logging depend on knowing the production log sink configuration.
