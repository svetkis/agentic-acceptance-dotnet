# Public API Surface as a Declared Artifact — Nothing Leaks Out by Accident

> An agent that "improves" a helper renames a public method, adds a parameter,
> deletes the overload it considers dead, or widens a class to `public` so
> another project can use it. Inside the repository every test stays green —
> the consumers of the contract break silently, at their next build. `public`
> is the path of least resistance for an agent: it always compiles, it always
> works locally, and nothing in the build asks whether the surface was supposed
> to change. This solution makes the public surface a **declared artifact**:
> every change to it is a conscious act recorded in a file, and that file's
> diff is the report.

**Positioning in the model:** this is a **Level 1 Change Check** (build-time,
deterministic, fires on every build). It is the contract-surface sibling of
[`nuget-audit-as-error.md`](nuget-audit-as-error.md): that gate stops a
vulnerable dependency from landing silently; this gate stops a contract change
from landing silently. Whether the surface is *well designed* is a Level 4
question — [`api-design-audit`](../../templates/skills/api-design-audit/SKILL.md).

**Applicability (read this first):** libraries, SDKs, shared internal packages,
plugin hosts — anywhere code **outside your repository** compiles against your
types. A single-deployment application (`DemoProject.MinimalApi` style) has no
external consumers: every controller and DTO would land in the tracking files
as noise. Cross this one out in
[`ADAPTATION.md`](../../templates/skills/ADAPTATION.md) unless you ship a
contract. Working demo: [`examples/DemoProject/src/DemoProject.Domain/`](../../examples/DemoProject/src/DemoProject.Domain/)
(green — Domain plays the role of a library consumed outside the repository) +
[`examples/DemoProject/traps-src/DemoProject.Traps.PublicApi/`](../../examples/DemoProject/traps-src/DemoProject.Traps.PublicApi/)
(red — see §7).

---

## 1. The Trap: `public` Is the Path of Least Resistance

The leak has three faces, and all of them look like diligence in review:

```csharp
// Face 1 — widening for convenience:
// "Another project needs this helper, I'll just make it public"
public static class OrderNumberFormatter  // was internal

// Face 2 — "improvement" that breaks the contract:
// "CalculateTotal is a misleading name, renaming to CalculateGrandTotal"
public decimal CalculateGrandTotal(Guid orderId)  // consumers: CS1061

// Face 3 — dead code cleanup:
// "This overload has no callers in the repo — removing"
// (no callers *in the repo*; 40 callers in consuming repos)
```

**TRAP: repo tests stay green.** Every check inside the repository keeps
passing — the compiler, the tests, the architecture rules. The failure happens
in someone else's build, days later, with no link back to the commit that
caused it. A guardrail that lives inside the repo cannot see this class of
breakage *after the fact*; it has to make the change itself visible.

**TRAP: the analyzer is not enough by itself.** `PublicApiAnalyzers` enforces
*declaration*, not *intent*. If the test-motivated leak already happened (face 1),
the analyzer will dutifully ask the agent to declare the leaked type — the agent
adds the line, the gate passes, and the surface grew anyway. That is why this
solution has three layers, not one.

## 2. Layer 1 — Internal by Default (Fail-Closed Visibility)

Everything is `internal` unless it is *deliberately* part of the contract. This
is the fail-closed default: a new type or member that nobody decided to export
is invisible outside the assembly — even if the agent forgot every rule, an
accidental consumer cannot see it.

Practical effect for agents: renaming an internal method is free — no contract
consequences, no tracking-file churn. The "expensive to change" area shrinks to
the consciously designed contract, and everything else stays refactorable.

## 3. Layer 2 — `InternalsVisibleTo` as a Narrow Allowlist

Internal-by-default collapses at the first test that needs access — unless the
test assembly can see internals. `InternalsVisibleTo` removes the last
*legitimate* excuse to write `public`:

```xml
<!-- Library .csproj — allowlist, not an open door -->
<ItemGroup>
  <InternalsVisibleTo Include="MyCompany.MyLib.Tests" />
</ItemGroup>
```

With this in place, "make it public so I can test it" is never a reason to
change visibility. The only remaining reason to write `public` is "this is part
of the contract" — which is precisely the decision the guardrail wants to force.

**TRAP: IVT is an allowlist, or it is nothing.** One entry (the test assembly;
at most also a plugin host). Five entries turn `internal` into de-facto `public`
with worse tooling. Treat a new `InternalsVisibleTo` line in a diff as the same
class of review signal as the unshipped-file diff below.

**TRAP: IVT is not a security boundary.** Another assembly can claim the
friend name; only strong-name signing makes the grant verifiable. This is
encapsulation inside a team, not protection from an adversary.

**Not a license to test internals.** Prefer testing through the public
contract; IVT is the escape hatch for behavior that cannot be observed any
other way. Tests welded to internals break on every refactor — the opposite of
what Layer 1 buys you.

## 4. Layer 3 — PublicApiAnalyzers: The Declared Surface

[`Microsoft.CodeAnalysis.PublicApiAnalyzers`](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md)
(the analyzer family the .NET team uses on its own assemblies) turns the public
surface into two text files next to the project:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.PublicApiAnalyzers" Version="3.3.4" />
  <AdditionalFiles Include="PublicAPI.Shipped.txt" />   <!-- frozen: what already shipped -->
  <AdditionalFiles Include="PublicAPI.Unshipped.txt" /> <!-- accumulating: what changed since -->
</ItemGroup>
```

```xml
<!-- RS0017 is reported ON the declaration-file line, where .editorconfig diagnostic
     severities do not reach — escalate both IDs in the csproj, not .editorconfig -->
<PropertyGroup>
  <WarningsAsErrors>RS0016;RS0017</WarningsAsErrors>
</PropertyGroup>
```

The diagnostics that matter (IDs verified against the official
[rule docs](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.md)):

| Diagnostic | Fires when | Direction |
|------------|------------|-----------|
| **RS0016** | A public symbol is not listed in the API files | Added / changed surface |
| **RS0017** | A listed symbol no longer exists | Removed / renamed surface |
| RS0025 | The same symbol is declared twice in the files | File hygiene |
| RS0026 / RS0027 | Optional-parameter overload shapes that break overload resolution for existing callers | Compatible-looking additions |
| RS0036 / RS0037 / RS0041 | Nullability of declared public members is not tracked / annotated | Contract precision |
| RS0048 / RS0050 | A tracking file is missing; a `*REMOVED*`-marked symbol still exists | File hygiene |

The workflow this forces:

1. **Add or change** a public symbol → RS0016 → the *only* fix is adding the
   exact line to `PublicAPI.Unshipped.txt`. The line is printed in the
   diagnostic message itself — no IDE code fix needed, an agent pastes it
   (the code fix is unreliable in VS Code / Rider anyway:
   [dotnet/roslyn-analyzers#3192](https://github.com/dotnet/roslyn-analyzers/issues/3192)).
2. **Remove or rename** → RS0017 → declare the removal with the `*REMOVED*`
   prefix in `PublicAPI.Unshipped.txt`. Deleting a public symbol can no longer
   be an accident: it is a written, reviewable statement.
3. **Release** → move unshipped entries into `PublicAPI.Shipped.txt`. The pair
   becomes a built-in changelog of the API surface per release
   (Control Maintenance — [`release-readiness-audit`](../../templates/skills/release-readiness-audit/SKILL.md)
   can own this step).

Bootstrap: on day one, generate the initial file contents (code fix or
diagnostic messages), then **review the list itself** — anything on it that is
not supposed to be a contract gets demoted to `internal` right away. After
bootstrap, a 50-line unshipped diff is not a rubber stamp; it is a design
review request.

## 5. The Unshipped Diff Is the Report

This is the part that makes the whole thing an *agentic* guardrail rather than
a library-author nicety. The declaration files give review a compressed,
lossless signal of what left the assembly boundary:

| Signal in the diff | Meaning |
|--------------------|---------|
| `PublicAPI.Unshipped.txt` non-empty | This PR changes the contract — review the *design*, not just the code |
| `*REMOVED*` / renamed lines | Breaking change — a versioning decision (major bump?), not a refactor |
| New `InternalsVisibleTo` line | Internal surface silently widening into de-facto public |
| Large "add everything" diff after bootstrap | Agent sprayed `public` across the change — reject, demote to internal |

One file to read instead of a 2000-line code diff. This is wired into the
[`code-review`](../../templates/skills/code-review/CHECKLIST.md) checklist
("Public API Surface" section): *diff of `PublicAPI.Unshipped.txt` or
`InternalsVisibleTo` non-empty → mark PR as contract-changing.*

Relations to neighboring artifacts:

- [`RatchetTest.cs`](../../tests/patterns/RatchetTest.cs) guards the **count**
  of public types (no uncontrolled growth); the analyzer guards their
  **identity**. A rename is invisible to the ratchet and fatal to the analyzer —
  they compose.
- Layer rules (NetArchTest, `LayerGuardAnalyzer` SAE010–SAE012) govern *who may
  reference whom*; they say nothing about surface stability. Orthogonal.
- The `*REMOVED*` flow is the same philosophy as
  [Decision Guards](../../templates/skills/acceptance-bootstrap/DECISION-GUARDS.md):
  an intentional deviation is a written record, never a silent side effect.

## 6. What This Gate Does NOT Cover

- **Design quality.** The files say the surface *changed*, not that it is good.
  That is [`api-design-audit`](../../templates/skills/api-design-audit/SKILL.md), Level 4.
- **All binary-compat edge cases.** The analyzer models source-level surface
  drift; subtle ABI nuances and behavioral changes are outside its net.
- **Internal surface churn.** Opt-in `InternalAPI.*.txt` tracking exists
  (RS0051–RS0061, disabled by default) — heavy; for most teams
  internal-by-default + the IVT allowlist is the 90% solution at 10% of the cost.
- **File rot.** If nobody migrates unshipped → shipped at releases, the report
  degrades into noise and reviewers stop reading it. The guardrail dies of
  pollution, not of failure — assign the migration step an owner.
- **Applications without external consumers.** Every public type of an app is
  "surface" to the tracker; none of it is a contract. Do not install noise.

## 7. Working Demo (DemoProject)

Both sides of the guardrail run in [`examples/DemoProject/`](../../examples/DemoProject/) and are verified by the
`traps-guardrails` CI job:

- **Green:** [`src/DemoProject.Domain/`](../../examples/DemoProject/src/DemoProject.Domain/) — Domain plays the role
  of a library consumed outside the repository. The package is wired in, the current surface is bootstrapped into
  `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt` is empty. The solution builds green with
  `TreatWarningsAsErrors`; any future surface change lands in the unshipped file or fails the build.
- **Red:** [`traps-src/DemoProject.Traps.PublicApi/`](../../examples/DemoProject/traps-src/DemoProject.Traps.PublicApi/) —
  a small "library" whose declared contract says one thing and whose source says another: a leaked public helper
  (RS0016), a renamed method and a deleted method (RS0017 × 2). The project is deliberately **not** part of
  `DemoProject.sln` — the trap must fail its own build, not the solution. Verified locally with 6×RS0016 + 4×RS0017
  build errors; see [`TRAPS.md`](../../examples/DemoProject/TRAPS.md) for how to run it.

Two field findings from wiring the demo (both cost real debugging time — do not rediscover them):

1. **Bootstrap without an IDE.** The RS0016 code fix is unreliable outside Visual Studio, and the declaration line
   format has no single canonical write-up (properties are declared as separate `.get`/`.init` accessor lines,
   enum members as `Member = value -> EnumType`, record structs get both a parameterless and a primary
   constructor line, nullable reference types get a trailing `!`). Do not hand-write the bootstrap: empty the
   files, then let the analyzer generate every line itself —

   ```bash
   dotnet format analyzers <project.csproj> --diagnostics RS0016 --severity error
   ```

   writes the exact canonical lines into `PublicAPI.Unshipped.txt`; review the list, then move it to
   `PublicAPI.Shipped.txt` (that move *is* the "what do we admit has shipped" review).

2. **RS0017 severity needs the csproj.** `.editorconfig` `dotnet_diagnostic` escalation works for RS0016 (reported
   on the `.cs` location) but is silently ignored for RS0017 (reported on the `.txt` declaration line) — a
   `[*.txt]` or `[PublicAPI.*.txt]` section does not help. Use `<WarningsAsErrors>RS0016;RS0017</WarningsAsErrors>`
   in the csproj.

## Checklist (copy into your project)

- [ ] The project ships a contract (library / shared package / plugin host) — otherwise stop here
- [ ] Types and members are `internal` by default; `public` requires a reason
- [ ] `InternalsVisibleTo` = exactly the test assembly, kept as an allowlist
- [ ] `Microsoft.CodeAnalysis.PublicApiAnalyzers` package + `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` as `AdditionalFiles`
- [ ] RS0016 / RS0017 escalated to build errors via `<WarningsAsErrors>` in the csproj
- [ ] Initial surface generated, then *reviewed* — non-contract types demoted to `internal`
- [ ] Code review rule: non-empty `PublicAPI.Unshipped.txt` diff, `*REMOVED*` lines, or new `InternalsVisibleTo` ⇒ PR flagged as contract-changing
- [ ] Release step: unshipped entries move to shipped, with an owner
