// GUARDRAIL: ArchUnitNET catches circular dependencies between slices.
// TRAP: An agent created the cycle Orders -> Payments -> Shipping -> Orders.
// This file is a working adaptation of the template from tests/patterns/ArchUnitNetSliceTest.cs
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.Loader;
using TUnit;

namespace DemoProject.Traps.Tests;

public class ArchUnitNetSliceTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(typeof(Domain.MutableState).Assembly)
        .Build();

    [Test]
    public async Task Modules_ShouldBeFreeOfCycles()
    {
        IArchRule rule = SliceRuleDefinition.Slices()
            .Matching("DemoProject.Traps.Modules.(*)..")
            .Should()
            .BeFreeOfCycles();

        var violations = rule.Evaluate(Architecture).Where(v => !v.Passed).ToList();
        var message = violations.Any()
            ? "Cyclic dependencies detected between modules. " +
              "Expected: Modules should not depend on each other in a cycle. " +
              "Violations: " + string.Join(", ", violations.Select(v => v.ToString()))
            : string.Empty;

        await Assert.That(rule.HasNoViolations(Architecture)).IsTrue()
            .Because(message);
    }
}
