namespace DemoProject.Traps;

// TRAP: An agent adds heavy allocations to a method that is called frequently.
// GUARDRAIL: [HotPath] + AllocationBudgetTests catch allocation regressions.
[AttributeUsage(AttributeTargets.Method)]
public sealed class HotPathAttribute : Attribute { }
