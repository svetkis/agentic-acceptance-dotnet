# Dead Code Audit — Checklist

## Setup (first run in a repo)
- [ ] Scanner built (syntax-only Roslyn pass) or one-shot candidate source chosen
- [ ] Exclusion catalog calibrated: reflection roots, framework-invoked members,
      config-binding properties, source-generator markers, convention catalogs,
      analyzer projects, shipped contract assemblies
- [ ] Every exclusion carries a comment naming the activation mechanism
- [ ] src/test project split and DI-registration files known

## Scan
- [ ] Scanner run over the full solution; report written with file:line per row
- [ ] Categories A–F populated (E2 informational; F hygiene)
- [ ] Triage order: A/B first, then C/D, then E

## Verification (per candidate — before any deletion)
- [ ] `rg -w "SymbolName"` over ALL files (markup/scripts/configs, not only .cs);
      hits are the declaration itself + true same-name collateral; operational
      entry points (deploy guides, ops scripts, scheduler configs) count as alive
- [ ] Reflection/dispatch check: dictionary routing, `typeof(T)`, `GetService`,
      factory lambdas — any plausible reach ⇒ NEEDS_REVIEW, not deletable
- [ ] EF Core: last `DbSet` / `Set<T>()` / configuration reference checked —
      deleting it drops the entity from the model (next migration = DropTable);
      entity properties are schema: zero code refs ≠ dead column (DropColumn
      decision, not code cleanup)
- [ ] Interface chains: interface member + all implementations planned in one commit
- [ ] Contract assemblies: candidates there are out of scope (alive by contract)
- [ ] Parasitic tests: src code and its tests deleted together

## Cleanup Waves
- [ ] One wave = one commit; no feature work mixed in
- [ ] Scan rerun after each wave (dead chains peel in layers)
- [ ] Repeated until A–D stop shrinking
- [ ] Optional ratchet: summary counts committed and diffed each run

## Report
- [ ] Summary table with per-category counts (+ delta vs previous run)
- [ ] Every deletable row carries its `rg -w` evidence (command, hit count, hit locations)
- [ ] NEEDS_REVIEW rows list the unresolved check (dispatch / markup / contract)
- [ ] Wave commits listed; backlog items handed to tech-debt-audit (P3)
