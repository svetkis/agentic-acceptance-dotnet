// GUARDRAIL: ArchUnitNET catches circular dependencies between slices,
// which NetArchTest.NotHaveDependenciesBetweenSlices does not distinguish.
// TRAP: The agent adds cross-module calls via mediator / events / shared kernel,
// creating a cycle Orders -> Payments -> Shipping -> Orders.
// NetArchTest forbids ANY dependencies between slices (zero-tolerance).
// ArchUnitNET allows having a DAG (directed acyclic graph),
// but only catches cycles.

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.Loader;
using TUnit;

namespace Tests.Patterns;

public class ArchUnitNetSliceTests
{
    // TIP: load the architecture once into a static readonly for performance.
    // ArchUnitNET reads bytecode via Mono.Cecil — this is more expensive than NetArchTest.
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(typeof(ArchUnitNetSliceTests).Assembly)
        .Build();

    [Test]
    public async Task Modules_ShouldBeFreeOfCycles()
    {
        // GUARDRAIL: Circular dependencies between modules/features.
        // DAG allowed: Orders -> Payments -> Shipping.
        // Cycle forbidden: Shipping -> Orders.
        IArchRule rule = SliceRuleDefinition.Slices()
            .Matching("MyApp.Modules.(*)..")
            .Should()
            .BeFreeOfCycles();

        await Assert.That(rule.HasNoViolations(Architecture)).IsTrue();
    }

    [Test]
    public async Task Modules_ShouldNotDependOnEachOther()
    {
        // GUARDRAIL: An alternative to NetArchTest.NotHaveDependenciesBetweenSlices.
        // Zero-tolerance: any dependency between slices is forbidden.
        // Use it when modules must be fully isolated.
        // NOTE: This is the same guardrail as NetArchTest, but via the ArchUnitNET API.
        IArchRule rule = SliceRuleDefinition.Slices()
            .Matching("MyApp.Modules.(*)..")
            .Should()
            .NotDependOnEachOther();

        await Assert.That(rule.HasNoViolations(Architecture)).IsTrue();
    }
}
