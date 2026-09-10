# Repository Settings as Guardrails — Enforcing the Gate at the Server

> Local hooks and CI files can be skipped, edited, or forgotten. Server-side
> repository settings cannot — they apply to everyone, including Maintainers
> and agents with push rights. This document maps the settings that make the
> merge gate real.

**Positioning in the model:** these settings belong to the *process* tier
(Control Foundation / Engineering Governance). They do not check code — they
guarantee that the checks themselves cannot be bypassed.

---

## 1. Merge Gate: No Green Pipeline — No Merge

The single most important pair of settings:

| Intent | GitLab | GitHub |
|--------|--------|--------|
| MR/PR cannot merge without a green pipeline | `Merge checks → Pipelines must succeed` (`only_allow_merge_if_pipeline_succeeds: true`) | Ruleset → `Require status checks to pass` |
| A skipped pipeline is **not** green | `allow_merge_on_skipped_pipeline: false` | A skipped required job counts as success on GitHub — make required jobs unconditional (`always()` only for the verdict step) |

**TRAP: skipped pipeline counts as green.** If the merge gate allows merges on
skipped pipelines, then any condition that makes the pipeline skip (path
filters, rules, `if:` guards) silently disables the gate. The second setting
is what makes the first one real.

**TRAP: green CI ≠ working code.** The gate only enforces that the pipeline
ran and passed. It says nothing about test quality — a pipeline that reports
`0 tests ran` is still green. Pair this gate with
[`ci/scripts/run-and-verify-tests.sh`](../../ci/scripts/run-and-verify-tests.sh),
which fails the build when no tests actually executed.

---

## 2. Default Branch: Direct Push Closed for Everyone

| Intent | GitLab | GitHub |
|--------|--------|--------|
| No direct pushes to the default branch — everyone merges through MR/PR | Protected branch `master` / `main`: `Allowed to push: No one`, merge via Maintainers | Ruleset: `Restrict who can push to matching branches` (empty / admins only), or `Require a pull request` |
| History cannot be rewritten | `Allowed to force push: disabled` | `Block force pushes` |

**Why "No one" and not "Maintainers":** a setting that exempts a privileged
group is a setting that gets used "just this once" under deadline pressure —
and the pipeline and review are bypassed exactly when they matter most.
Closing direct push for everyone turns the discipline into an invariant.

**TRAP: direct push bypasses pipeline and review.** This failure mode needs
no agent and no malice: a tired maintainer committing "a tiny hotfix" straight
to `master` ships unreviewed, untested code. Server-side push protection is
the only control that cannot be talked around.

---

## 3. Feature Branch Templates: Sent History Is Immutable

For `feat/*`, `fix/*`, `spike/*` style templates:

| Intent | GitLab | GitHub |
|--------|--------|--------|
| The executor cannot rewrite a branch already pushed for review | Protected branch wildcard `feat/*` etc.: `Allowed to push: Developers + Maintainers`, force push disabled | Ruleset on `feat/**` etc.: push allowed to developers, `Block force pushes` |

**Why:** before this, "don't force-push after requesting review" was pure
discipline. Reviewers could not rely on what they had already looked at — a
`push --force` silently replaced reviewed code. With force push disabled at
the server, the history sent for review is the history that gets merged.

**TRAP: force push replaces reviewed code.** The reviewer approved commit A;
the author force-pushes commit B with the same message; the MR shows
"approved" and merges B. Blocking force push on branch templates closes this
for everyone — including accidental rewrites from a local `rebase -i` gone
wrong.

---

## 4. Verify the Guardrail Itself

Guardrails rot. Verify the settings are actually in place — the same
fail-closed principle as [`guardrails-review`](../templates/skills/guardrails-review/SKILL.md).

**GitLab** (project id is an example):

```bash
# Merge gate
curl -s -H "PRIVATE-TOKEN: $TOKEN" \
  "https://gitlab.example.com/api/v4/projects/<PROJECT_ID>" \
  | grep -o '"only_allow_merge_if_pipeline_succeeds":[a-z]*'
# expect: true
curl -s -H "PRIVATE-TOKEN: $TOKEN" \
  "https://gitlab.example.com/api/v4/projects/<PROJECT_ID>" \
  | grep -o '"allow_merge_on_skipped_pipeline":[a-z]*'
# expect: false

# Protected branches
curl -s -H "PRIVATE-TOKEN: $TOKEN" \
  "https://gitlab.example.com/api/v4/projects/<PROJECT_ID>/protected_branches"
# expect: default branch with push_access_levels [], wildcards feat/* fix/* spike/*
```

**GitHub** (CLI):

```bash
gh api repos/<OWNER>/<REPO>/rulesets --jq '.[] | {name, enforcement}'
gh api repos/<OWNER>/<REPO>/branches/main/protection
```

A settings audit like this is a natural item for the
[`guardrails-review`](../templates/skills/guardrails-review/SKILL.md) skill:
any change to guardrail code or gate configuration should re-verify these
expectations.

---

## Checklist (copy into your project)

- [ ] MR/PR cannot merge without a green pipeline
- [ ] Skipped pipeline does not count as green
- [ ] Direct push to the default branch is closed for **everyone**
- [ ] Force push to the default branch is blocked
- [ ] Feature branch templates (`feat/*`, `fix/*`, `spike/*`) block force push
- [ ] CI fails when `0 tests ran` (gate enforces execution, not just passing)
- [ ] Settings verification is part of a recurring guardrails review
