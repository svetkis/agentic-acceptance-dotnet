# Complexity Audit — Checklist

## Before Starting
- [ ] Project stack is known (.NET version, SonarAnalyzer version)
- [ ] Project type is defined: new / legacy
- [ ] Thresholds are known: cognitive ___ / cyclomatic ___
- [ ] Baseline is recorded (for legacy)

## Compile-time Guardrails (new projects)
- [ ] `SonarAnalyzer.CSharp` is connected
- [ ] `S3776` (cognitive) severity = error, threshold = ___
- [ ] `S1541` (cyclomatic) severity = error, threshold = ___
- [ ] `TreatWarningsAsErrors=true`
- [ ] API / endpoint layer has stricter thresholds

## Legacy Ratchet
- [ ] Baseline audit is completed
- [ ] `complexity-baseline.txt` is created and committed
- [ ] `ComplexityRatchetTest` is added to the test project
- [ ] Top 10 hotspots are documented
- [ ] Hotspot refactoring plan has deadlines (Q/N)

## Hotspot Analysis
- [ ] Methods with cognitive > 25 are reviewed
- [ ] Methods with cyclomatic > 15 are reviewed
- [ ] Logic duplication is checked (code-review semantic-duplication checklist)
- [ ] Every hotspot has a complexity reason

## Decision Guards
- [ ] Conscious deviations are documented as `COMPLEXITY-###`
- [ ] `COMPLEXITY-###` entries are added to `DECISION-GUARDS.md`

## Report Format
- [ ] Summary for `S3776` / `S1541` with trend
- [ ] BLOCKER / CRITICAL list with files and lines
- [ ] Refactoring backlog with IDs and deadlines
