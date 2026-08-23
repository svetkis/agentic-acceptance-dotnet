# Test Patterns (canonical templates)

Files here are **canonical templates** — framework-adaptation notes at the top of
each file show how to port a pattern to TUnit / xUnit / NUnit / MSTest.

The working examples in `examples/DemoProject/tests/` and
`examples/DemoProject.MinimalApi/tests/` are **adapted copies**, not linked files:
they reference concrete assemblies and contain project-specific rules. Each adapted
copy carries a provenance comment (`working adaptation of the template from
tests/patterns/...`).

**Do not sync blindly.** When you change a template here, treat the demo copies as
independent adaptations: review whether the change applies to each project's
stack and layout before porting it.
