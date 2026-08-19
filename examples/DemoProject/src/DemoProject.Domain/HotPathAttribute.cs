namespace DemoProject.Domain;

// TRAP: An agent adds heavy allocations or async to a method that is called 1000 times per second.
// GUARDRAIL: [HotPath] + a Roslyn analyzer catch new/async/boxing before tests run.
[AttributeUsage(AttributeTargets.Method)]
public sealed class HotPathAttribute : Attribute { }
