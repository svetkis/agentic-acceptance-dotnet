namespace DemoProject.Domain;

// TRAP: An agent writes a query (read path) that mutates entities or calls SaveChanges —
// a "read" that changes state breaks CQRS separation and causes phantom writes.
// GUARDRAIL: [Query] + a Roslyn analyzer catch mutations in the read path at compile time.
[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryAttribute : Attribute { }
