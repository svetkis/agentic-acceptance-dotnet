// TRAP: An agent uses EF Core antipatterns that reflection cannot see
// or violates read/write path conventions.
// GUARDRAIL: Regex source scanning catches violations of EF-specific rules.
// NOTE: This file is for EF Core projects only. For Dapper see DapperGuardRules.cs.

using System.Reflection;
using System.Text.RegularExpressions;
using DemoProject.Domain;
using NetArchTest.Rules;
using TUnit;

namespace DemoProject.Tests;

public class EfCoreGuardRules
{
    private static readonly Assembly ApplicationAssembly = typeof(Application.BookingService).Assembly;

    // TRAP: An agent added a DbContext to the Application layer.
    // GUARDRAIL: Application knows only about Ports (interfaces).
    [Test]
    public async Task Application_ShouldNotReferenceEfCore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore")
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue()
            .Because(FormatFailingTypes(result));
    }

    // TRAP: An agent uses FindAsync in a read path, violating layered architecture.
    // GUARDRAIL: Regex source scanning catches antipatterns that reflection cannot see.
    // NOTE: Also caught at compile time via BannedApiAnalyzers (RS0030) in BannedSymbols.txt.
    //       The regex here is a fallback / double-check for cases where the analyzer did not load.
    [Test]
    public async Task SourceCode_ShouldNotUse_FindAsync_InQueryServices()
    {
        var violations = ScanSourceFiles(
            pattern: @"\.FindAsync\(",
            fileGlob: "*.cs",
            whitelist: Array.Empty<string>());

        await Assert.That(violations).IsEmpty()
            .Because("FindAsync is only allowed in write-path / command handlers.");
    }

    // TRAP: An agent used .Include() in QueryService — N+1 and extra data.
    // GUARDRAIL: Regex source scanning catches what reflection cannot see.
    [Test]
    public async Task SourceCode_ShouldNotUse_Include_InQueryServices()
    {
        var violations = ScanSourceFiles(
            pattern: @"\.Include\(",
            fileGlob: "*QueryService*.cs",
            whitelist: Array.Empty<string>());

        await Assert.That(violations).IsEmpty()
            .Because("QueryService must use .Select() projections, not .Include()");
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
