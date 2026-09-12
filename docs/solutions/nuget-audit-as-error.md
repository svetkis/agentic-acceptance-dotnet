# NuGet Audit as a Build Error — Dependency Warnings Cannot Be Skipped

> NuGet audit warnings (NU1901–NU1905) appear in the restore output and are
> just as easily ignored as any other warning. An agent adding a dependency
> with a known CVE sees a yellow line, adds `--verbosity quiet`, and moves on.
> Turning those warnings into build errors moves dependency safety from
> "someone should have read the log" into the deterministic pipeline.

**Positioning in the model:** this is a **Level 1 Change Check** (build-time,
deterministic, fires on every build). It hardens what
[`version-audit`](../../templates/skills/version-audit/SKILL.md) and
[`VersionAuditTest.cs`](../../tests/patterns/VersionAuditTest.cs) do on
schedule at Level 4: the audit finds drift and plans upgrades; the build gate
makes sure a *new* vulnerable or deprecated dependency cannot land silently
between audits.

---

## 1. The Minimum: `TreatWarningsAsErrors` Already Does Half the Work

Since .NET 8, `dotnet restore` runs a vulnerability audit for **direct**
dependencies by default and emits NU1901–NU1903 as warnings. If the build
already runs with `TreatWarningsAsErrors=true` (as in
[`ci/github-actions/safe-ci.yml`](../../ci/github-actions/safe-ci.yml)),
those warnings fail the build — no new properties required.

**TRAP: "we have `TreatWarningsAsErrors`, we're covered."** Only if restore
and build run in the same pipeline. A workflow that restores in one job and
builds in another without carrying warnings, or a developer who restores
locally with default verbosity and never reads the output, keeps the gap.
The audit must fail the *build*, not the log.

## 2. Close the Transitive Gap: `NuGetAuditMode=all`

The default audit checks only direct references. The vulnerable package is
usually four hops down the transitive graph — exactly the place no one looks.

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <NuGetAuditMode>all</NuGetAuditMode>   <!-- direct + transitive -->
  <NuGetAuditLevel>low</NuGetAuditLevel> <!-- report low severity and above -->
</PropertyGroup>
```

Severity coverage per mode:

| Warning | Meaning | Default |
|---------|---------|---------|
| NU1901 | Critical severity vulnerability | on |
| NU1902 | High severity vulnerability | on |
| NU1903 | Moderate severity vulnerability | on |
| NU1904 | Low severity vulnerability | off (`NuGetAuditLevel=low` enables) |
| NU1905 | Package is deprecated (since .NET 10) | off unless audit level allows |

In CI, the same can be passed per-run without touching the repo file:

```yaml
# GUARDRAIL: vulnerable/deprecated dependencies (incl. transitive) fail the build
- run: dotnet restore /p:NuGetAuditMode=all /p:NuGetAuditLevel=low
- run: dotnet build --no-restore /p:TreatWarningsAsErrors=true
```

**TRAP: agent suppresses the warning instead of fixing the cause.** An agent
under deadline pressure will happily add `<NoWarn>NU1903</NoWarn>` or a
`NuGetAuditSuppress` entry. That is a Decision Guard, not a fix — see §3.

## 3. Suppressions Must Expire

A suppressed vulnerability is accepted risk, and accepted risk has an owner
and a deadline. Same lifecycle discipline as the existing
[`ci/scripts/check-guardrail-lifecycle.sh`](../../ci/scripts/check-guardrail-lifecycle.sh)
check for `NuGetAuditSuppress` entries:

```xml
<ItemGroup>
  <!-- AUD-014: transient CVE in Newtonsoft.Json 13.0.2 via LegacyClient 2.x;
       upgrade scheduled 2026-10; remove after. Owner: platform team. -->
  <NuGetAuditSuppress Include="https://github.com/advisories/GHSA-5crp-9r3c-p9vr" />
</ItemGroup>
```

Rules: one advisory URL per entry, an expiry date, an owner, and a reason.
An audit-suppress entry without an expiry comment should fail the
guardrails-review / lifecycle check.

## 4. Pinned and Central: Make the Graph Auditable

Two enabling practices that make the audit meaningful:

- **Central Package Management (`Directory.Packages.props`)** — one file where
  every package version lives. A vulnerable version is visible in a single
  diff instead of scattered across csproj files; upgrading is one line.
- **`RestoreLockedMode` + `packages.lock.json`** — CI restores exactly the
  graph that was reviewed. An agent cannot silently widen a transitive
  dependency: the lock file diff shows up in the PR.

## 5. What the Build Gate Does NOT Cover

Stay honest about the boundary — this is a Level 1 gate, not a substitute
for Level 4:

- It reacts to advisories **known at restore time** (the audit queries
  NuGet.org data). A newly published CVE between audits is caught on the next
  scheduled `version-audit` run — wire that as a scheduled pipeline, not only
  on PR.
- Deprecated *versions* of a healthy package (no advisory yet) — the domain
  of `dotnet list package --outdated` in `version-audit`.
- License and supply-chain provenance — needs dedicated scanning tooling.

---

## Checklist (copy into your project)

- [ ] Build fails on NU1901–NU1903 (either `TreatWarningsAsErrors` or audit as error) in **CI**, not just locally
- [ ] `NuGetAuditMode=all` — transitive dependencies are audited
- [ ] `NuGetAuditLevel` chosen deliberately (default `moderate`; `low` for strict)
- [ ] Deprecated packages (NU1905) fail the build or are tracked consciously
- [ ] `NuGetAuditSuppress` entries carry owner + expiry and are verified by a lifecycle check
- [ ] No `NoWarn>NU19xx` anywhere in the repo (grep is part of guardrails-review)
- [ ] Central Package Management (`Directory.Packages.props`) or a conscious decision recorded otherwise
- [ ] Scheduled `version-audit` run still exists — the build gate covers only *new* landings
