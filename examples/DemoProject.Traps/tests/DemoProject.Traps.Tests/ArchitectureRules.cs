// GUARDRAIL: NetArchTest.eNhancedEdition catches architecture traps set by AI agents.
// This project is a failing demo: every test here MUST fail,
// because violations were intentionally created in src/DemoProject.Traps.

using System.Reflection;
using NetArchTest.Rules;
using TUnit;

namespace DemoProject.Traps.Tests;

public class ArchitectureRules
{
    private static readonly Assembly TrapsAssembly = typeof(Domain.MutableState).Assembly;

    // TRAP: An agent added mutable state to Domain via a public field.
    // GUARDRAIL: BeImmutableExternally catches public fields / a mutable surface.
    // NOTE: NetArchTest may not populate Explanation for this rule.
    //       So we add a human-readable message in Because.
    [Test]
    public async Task DomainTypes_ShouldBeImmutableExternally()
    {
        var result = Types.InAssembly(TrapsAssembly)
            .That().ResideInNamespace("DemoProject.Traps.Domain")
            .And().AreNotEnums()
            .Should()
            .BeImmutableExternally()
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue()
            .Because(result.IsSuccessful
                ? string.Empty
                : "Domain types must be immutable externally. " +
                  "Check for public fields, public setters, or mutable collections. " +
                  "Failing types: " + string.Join(", ", result.FailingTypes.Select(t => t.FullName)));
    }

    // TRAP: An agent added using System.Net.Http in Domain for "a single call".
    // GUARDRAIL: HaveDependencyOnAny catches an IL dependency on a forbidden namespace.
    [Test]
    public async Task Domain_ShouldNotDependOn_SystemNetHttp()
    {
        var result = Types.InAssembly(TrapsAssembly)
            .That().ResideInNamespace("DemoProject.Traps.Domain")
            .Should()
            .NotHaveDependencyOnAny("System.Net.Http")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue()
            .Because(FormatFailingTypes(result));
    }

    // TRAP: An agent added a using from a neighboring feature "for the sake of one DTO".
    // GUARDRAIL: Slice().NotHaveDependenciesBetweenSlices() catches a cross-module dependency.
    // NOTE: NetArchTest may not populate Explanation for slice rules.
    [Test]
    public async Task Features_ShouldNotDependOn_EachOther()
    {
        var result = Types.InAssembly(TrapsAssembly)
            .Slice()
            .ByNamespacePrefix("DemoProject.Traps.Features")
            .Should()
            .NotHaveDependenciesBetweenSlices()
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue()
            .Because(result.IsSuccessful
                ? string.Empty
                : "Features must not depend on each other. " +
                  "Each feature slice should be self-contained. " +
                  "Failing types: " + string.Join(", ", result.FailingTypes.Select(t => t.FullName)));
    }

    // TRAP: An agent used Guid instead of a strongly typed ID.
    // GUARDRAIL: Regex + architecture tests catch raw Guids in property names.
    [Test]
    public async Task Entities_ShouldNotUseRawGuidForIds()
    {
        var result = Types.InAssembly(TrapsAssembly)
            .That().ResideInNamespace("DemoProject.Traps.Domain")
            .And().HaveNameEndingWith("Entity")
            .Should()
            .NotHaveDependencyOnAny("System.Guid")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue()
            .Because(FormatFailingTypes(result));
    }

    private static string FormatFailingTypes(NetArchTest.Rules.TestResult result)
    {
        if (result.IsSuccessful)
            return string.Empty;

        var lines = result.FailingTypes
            .Select(t => $"- {t.FullName}: {t.Explanation}")
            .ToList();

        return "Failing types:\n" + string.Join("\n", lines);
    }
}
