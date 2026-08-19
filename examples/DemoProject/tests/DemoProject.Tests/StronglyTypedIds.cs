// GUARDRAIL: Domain entities use strongly typed IDs (BookingId, CustomerId),
// not raw Guid/string/int. This is free protection against substituting one entity's ID
// into a method expecting another's — caught at compile time.
// This file is a working adaptation of the template from tests/patterns/StronglyTypedIds.cs

using System.Reflection;
using DemoProject.Domain;
using TUnit;

namespace DemoProject.Tests;

public class StronglyTypedIds
{
    private static readonly Assembly DomainAssembly = typeof(Booking).Assembly;
    private static readonly HashSet<string> RawTypes = new() { "Guid", "String", "Int32", "Int64" };

    [Test]
    public async Task DomainEntities_ShouldNotUseRawPrimitivesForIds()
    {
        var violations = GetRawIdViolations(DomainAssembly);

        await Assert.That(violations).IsEmpty()
            .Because($"Domain entities must use strongly typed IDs. Violations: {string.Join(", ", violations)}");
    }

    [Test]
    public async Task StronglyTypedIdUsage_ShouldNotDecrease()
    {
        var stronglyTypedCount = CountStronglyTypedIds(DomainAssembly);

        const int baseline = 2; // BookingId + CustomerId
        await Assert.That(stronglyTypedCount).IsGreaterThanOrEqualTo(baseline)
            .Because($"Strongly typed IDs must not decrease. Current: {stronglyTypedCount}, baseline: {baseline}");
    }

    private static IEnumerable<string> GetRawIdViolations(Assembly domainAssembly)
    {
        var entityTypes = domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace?.Contains("Domain") == true);

        foreach (var type in entityTypes)
        {
            var idProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name.EndsWith("Id", StringComparison.Ordinal));

            foreach (var prop in idProperties)
            {
                var propTypeName = prop.PropertyType.Name;
                if (RawTypes.Contains(propTypeName) ||
                    (prop.PropertyType.IsGenericType && RawTypes.Contains(prop.PropertyType.GetGenericTypeDefinition().Name)))
                {
                    yield return $"{type.Name}.{prop.Name} : {propTypeName}";
                }
            }
        }
    }

    private static int CountStronglyTypedIds(Assembly domainAssembly)
    {
        return domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace?.Contains("Domain") == true)
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Count(p => p.Name.EndsWith("Id", StringComparison.Ordinal) &&
                        !RawTypes.Contains(p.PropertyType.Name) &&
                        !(p.PropertyType.IsGenericType && RawTypes.Contains(p.PropertyType.GetGenericTypeDefinition().Name)));
    }
}
