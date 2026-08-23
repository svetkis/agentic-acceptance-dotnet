// TRAP: In a single-project app, an agent breaks naming conventions, uses banned APIs,
// or forgets CancellationToken in public async methods.
// GUARDRAIL: NetArchTest + regex scanning catch convention violations
// This file is a working adaptation of the template from tests/patterns/ArchitectureRules.cs
// even when there are no Clean Architecture layers.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(result.IsSuccessful).IsTrue()
// - xUnit:  [Fact] + Assert.True(result.IsSuccessful)
// - NUnit:  [Test] + Assert.That(result.IsSuccessful, Is.True)
// - MSTest: [TestMethod] + Assert.IsTrue(result.IsSuccessful)

using System.Reflection;
using System.Text.RegularExpressions;
using DemoProject.MinimalApi.Features.Orders;
using NetArchTest.Rules;
using TUnit;

namespace DemoProject.MinimalApi.Tests;

public class ArchitectureRules
{
    private static readonly Assembly AppAssembly = typeof(OrderService).Assembly;

    // TRAP: An agent created a service with a wrong name (e.g., OrderManager).
    // GUARDRAIL: All services must end with "Service".
    [Test]
    public async Task Services_ShouldHaveNameEndingWithService()
    {
        // Alternative check: find types that do NOT end with Service but contain business logic
        var violations = Types.InAssembly(AppAssembly)
            .That().DoNotHaveNameEndingWith("Service")
            .And().DoNotHaveNameEndingWith("Endpoints")
            .And().DoNotHaveNameEndingWith("Request")
            .And().DoNotHaveNameEndingWith("Response")
            .And().DoNotHaveNameEndingWith("Program")
            .And().DoNotResideInNamespace(".*Domain.*")
            .And().AreClasses()
            .And().AreNotAbstract()
            .GetTypes()
            .ToList();

        // NetArchTest.GetTypes() returns IType, which has no IsNested/IsPublic.
        // We filter via reflection after retrieving the names.
        var typeNames = violations.Select(v => v.FullName).ToList();
        var reflectedTypes = typeNames
            .Select(name => AppAssembly.GetType(name))
            .Where(t => t is not null && t.IsPublic && !t.IsNested && !t.Namespace!.StartsWith("Microsoft") && !t.Namespace!.Contains(".Domain"))
            .ToList();

        await Assert.That(reflectedTypes).IsEmpty()
            .Because($"Public classes outside Domain must end with Service, Endpoints, Request, or Response. Violations: {string.Join(", ", reflectedTypes.Select(v => v!.Name))}");
    }

    // TRAP: An agent used DateTime.Now instead of UtcNow.
    // GUARDRAIL: Regex scanning catches banned APIs.
    [Test]
    public async Task SourceCode_ShouldNotUse_DateTimeNow()
    {
        var violations = ScanSourceFiles(
            pattern: @"DateTime\.Now\b",
            fileGlob: "*.cs",
            whitelist: new[] { "OrderService.cs: comment only" });

        await Assert.That(violations).IsEmpty()
            .Because("Use DateTime.UtcNow or IClock abstraction. DateTime.Now causes timezone bugs.");
    }

    // TRAP: An agent added a public async method without CancellationToken.
    // GUARDRAIL: Reflection checks all public async methods.
    [Test]
    public async Task PublicAsyncMethods_ShouldAcceptCancellationToken()
    {
        var violations = AppAssembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(m => m.IsPublic && IsAsyncMethod(m))
            .Where(m => !m.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken)))
            .Select(m => $"{m.DeclaringType?.Name}.{m.Name}")
            .ToList();

        await Assert.That(violations).IsEmpty()
            .Because($"Every public async method must accept CancellationToken ct = default. Violations: {string.Join(", ", violations)}");
    }

    // TRAP: An agent added using System.Net.Http in Domain for "a single call".
    // GUARDRAIL: Even in a single-project app, the Domain namespace must not depend on infrastructure.
    [Test]
    public async Task DomainNamespace_ShouldNotReferenceInfrastructure()
    {
        var result = Types.InAssembly(AppAssembly)
            .That().ResideInNamespace(".*Domain.*")
            .Should()
            .NotHaveDependencyOnAny("System.Net.Http", "System.Data.SqlClient")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue()
            .Because("Domain must not depend on infrastructure namespaces like System.Net.Http or System.Data.SqlClient");
    }

    private static bool IsAsyncMethod(MethodInfo method)
    {
        return method.ReturnType == typeof(Task)
            || (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            || method.ReturnType == typeof(ValueTask)
            || (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>));
    }

    private static IEnumerable<string> ScanSourceFiles(string pattern, string fileGlob, string[] whitelist)
    {
        var srcPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src");
        srcPath = Path.GetFullPath(srcPath);

        if (!Directory.Exists(srcPath))
            return Array.Empty<string>();

        var files = Directory.GetFiles(srcPath, fileGlob, SearchOption.AllDirectories);
        var violations = new List<string>();
        var regex = new Regex(pattern);

        foreach (var file in files)
        {
            if (file.Contains("obj") || file.Contains("bin"))
                continue;

            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                    var location = $"{relativePath}:{i + 1}";

                    if (whitelist.Any(w => location.Contains(w.Split(':')[0])))
                        continue;

                    violations.Add(location);
                }
            }
        }

        return violations;
    }
}
