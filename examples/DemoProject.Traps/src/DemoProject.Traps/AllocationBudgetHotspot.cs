namespace DemoProject.Traps;

public sealed class AllocationBudgetHotspot
{
    // TRAP: An agent added new inside a [HotPath] method.
    // GUARDRAIL: AllocationBudgetTests catch allocation regressions.
    // NOTE: This method intentionally allocates to demonstrate the guardrail failing.
    [HotPath]
    public int Process(int value)
    {
        var list = new List<int> { value };
        return list.Count;
    }
}
