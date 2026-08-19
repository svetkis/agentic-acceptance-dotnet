// TRAP: The agent uses EF Core antipatterns that reflection cannot see,
// or breaks read/write path conventions.
// GUARDRAIL: NetArchTest + starter regex checks catch violations of EF-specific rules.
// NOTE: For stable C# semantic rules (FindAsync/Include/read-path) prefer a Roslyn analyzer.
// This file is only for projects using EF Core. For Dapper see DapperGuardRules.cs.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(result.IsSuccessful).IsTrue()
// - xUnit:  [Fact] + Assert.True(result.IsSuccessful)
// - NUnit:  [Test] + Assert.That(result.IsSuccessful, Is.True)
// - MSTest: [TestMethod] + Assert.IsTrue(result.IsSuccessful)

using NetArchTest.Rules;
using System.Text.RegularExpressions;
using TUnit;

namespace Tests.Patterns;

public class EfCoreGuardRules
{
    // TRAP: The agent added FindAsync to a query handler, "because it is shorter".
    // GUARDRAIL: Regex scanning catches FindAsync in the read path (QueryHandlers / QueryServices).
    // NOTE: It is also caught at compile time via BannedApiAnalyzers (RS0030) in BannedSymbols.txt.
    //       Regex here is a fallback / double-check.
    [Test]
    public void FindAsync_ShouldNotBeUsedInReadPath()
    {
        var violations = ScanSourceFiles(
            pattern: @"\.FindAsync\(",
            fileGlob: "*Query*.cs",
            whitelist: Array.Empty<string>());

        Assert.That(violations).IsEmpty()
            .Because("FindAsync is only allowed in write-path / command handlers.");
    }

    // TRAP: The agent added a DbContext to the Application layer.
    // GUARDRAIL: Application only knows about Ports (interfaces).
    [Test]
    public void Application_ShouldNotReferenceEfCore()
    {
        var result = Types.InCurrentDomain()
            .That().ResideInNamespace(".*Application.*")
            .Should().NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.That(result.IsSuccessful).IsTrue();
    }

    // TRAP: The agent used .Include() in a QueryService — N+1 and extra data.
    // GUARDRAIL: Regex scanning catches what reflection cannot see.
    [Test]
    public void QueryServices_ShouldNotUse_Include()
    {
        var violations = ScanSourceFiles(
            pattern: @"\.Include\(",
            fileGlob: "*QueryService*.cs",
            whitelist: Array.Empty<string>());

        Assert.That(violations).IsEmpty()
            .Because("QueryService must use .Select() projections, not .Include()");
    }

    // --- Helpers: regex source scanning ---

    private static IEnumerable<string> ScanSourceFiles(string pattern, string fileGlob, string[] whitelist)
    {
        var srcPath = Path.Combine("..", "..", "..", "..", "src");
        if (!Directory.Exists(srcPath))
            return Array.Empty<string>();

        var files = Directory.GetFiles(srcPath, fileGlob, SearchOption.AllDirectories);
        var violations = new List<string>();
        var regex = new Regex(pattern);

        foreach (var file in files)
        {
            if (file.Contains("obj") || file.Contains("bin") || file.Contains("Tests"))
                continue;

            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                    var location = $"{relativePath}:{i + 1}";

                    // Simple whitelist check: if any whitelist entry contains the filename, skip
                    if (whitelist.Any(w => location.Contains(w.Split(':')[0])))
                        continue;

                    violations.Add(location);
                }
            }
        }

        return violations;
    }
}
