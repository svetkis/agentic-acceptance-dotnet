# Onboarding Report Template

> Rigid structure of the final report. The agent must fill in all 6 sections.

---

```markdown
# Onboarding Report: {Project.Name}

**Date:** {YYYY-MM-DD}  
**Mode:** fast / standard / high-assurance  
**Agent:** acceptance-bootstrap  
**Scanned:** {path to .sln}

---

## 1. Check Structure (What we built for this project)

| Level | Mechanism | Files / Skills | Status |
|------|-----------|----------------|--------|
| Foundation (Constitution) | AGENTS.md | `AGENTS.md` (from `rules/AGENTS_TEMPLATE.md`) | ✅ Implemented |
| 1. Change Checks | `Directory.Build.props` + `.editorconfig` | `Directory.Build.props` | ✅ Implemented |
| 2. Behavior — tests | TUnit + `dotnet run` | `tests/*.cs` | ✅ Implemented |
| 2. Behavior — architecture | NetArchTest | `tests/ArchitectureRules.cs` | 🚧 Backlog |
| 2. (gate) Code review | Agent by diff, before PR | `.kimi/skills/code-review/` | ✅ Adapted |
| 3. System | E2E / smoke / load | `tests/E2ETests.cs` | ❌ Not applicable |
| 4. Reality | Batch audits, complexity ratchet, dependency check | `.kimi/skills/security-audit/` | 🚧 Backlog |

**Legend:** ✅ Implemented / 🚧 Backlog / ❌ Not applicable

---

## 2. What was created (New artifacts)

For each created skill / test / rule:

1. **`{skill-name}`** — created because:
   - Ready-made skill `{original-skill}` did not fit (reason: stack / architecture / specifics)
   - Replaced with: {description of the new approach}
   - Complexity: {low / medium / high}
   - Put in: `{path}`

---

## 3. What did not fit and why (Rejected artifacts)

For each rejected ready-made artifact:

| Artifact | Reason for rejection | Replaced with | Status |
|----------|---------------------|---------------|--------|
| `templates/skills/code-review/` (standard) | Project without Clean Architecture — layer checks give 100% false positives | Adapted: removed layer checks, added Minimal API rules | Adapted |
| `tests/patterns/SnapshotTest.cs` | Worker Service — no HTTP, OpenAPI does not exist | Created `e2e-worker` — check message processing from queue | Created new |
| `templates/skills/dba-audit/` | Dapper instead of EF Core — migrations and `Include()` are not applicable | Created `dba-audit-dapper` | Created new |

---

## 4. Adaptations of ready-made artifacts (Adapted artifacts)

For each adapted skill:

### `templates/skills/security-audit/`
- **What changed:** Replaced `[Authorize]` check with `.RequireAuthorization()` for Minimal API
- **What checks were removed:** MVC authorization attributes
- **What was added:** Webhook protection check via secret token; middleware authorization check

### `templates/skills/performance-audit/`
- **What changed:** Added exception for `.Select()` projections (do not require AsNoTracking)
- **What checks were removed:** AsNoTracking on read-path with projections
- **What was added:** Exception for raw SQL (`FromSqlRaw`) — Change Tracker does not track them

---

## 5. Skill Ecosystem (Ecosystem map)

### 2. Behavior Checks — gates (every PR, before merge)
| Skill | Status | Note |
|-------|--------|------|
| `code-review` | ✅ Active | Adapted for Minimal API |
| `task-compliance` | ✅ Active | No changes |

### 4. Reality Checks (schedule / trigger)
| Skill | Status | Note |
|-------|--------|------|
| `security-audit` | 🚧 WIP | Adapted for Minimal API |
| `performance-audit` | 🚧 WIP | Adapted for projections + raw SQL |
| `dba-audit` | ❌ Skipped | Project uses Dapper — create `dba-audit-dapper` |

### Project-specific (unique)
| Skill | Status | Note |
|-------|--------|------|
| `e2e-worker` | 📋 Backlog | Check RabbitMQ message processing |

---

## 6. Implementation Backlog

### Sprint 0 — Foundation + Change Checks (1 day)
- [ ] **Implement** `rules/AGENTS_TEMPLATE.md` → adapt to stack
- [ ] **Implement** `rules/CONVENTIONS.md`
- [ ] **Adapt** `Directory.Build.props`

### Sprint 1 — Behavior Checks: architecture tests (3 days)
- [ ] **Adapt** `ArchitectureRules.cs` → custom rules for project architecture

### Sprint 2 — Review gate + Reality Checks (2 days)
- [ ] **Adapt** `code-review` → remove layer checks, add Minimal API
- [ ] **Adapt** `security-audit` → `.RequireAuthorization()`, webhook protection

### Sprint 3 — New skills (5 days)
- [ ] **Create** `{skill-name}` → {description}

### Backlog
- [ ] **Create** `dba-audit-dapper` → low priority, no DB complaints yet

---

## Appendices

- `ECOSYSTEM-MAP.md` — live map of all project skills
- `NEW-SKILLS/` — drafts of created skills (if any)
```

---

## Filling Rules

1. **All 6 sections are mandatory.** If a section has nothing to fill — write "No data".
2. **Section 3 (Rejected artifacts)** — the most important. It documents why ready-made artifacts did not fit. This prevents re-copying unsuitable skills in the future.
3. **Section 4 (Adapted artifacts)** — must contain specifics: which lines of SKILL.md were changed, which checks were removed.
4. **Statuses:** use only ✅ / 🚧 / ❌ / 📋 — for consistency between reports.
