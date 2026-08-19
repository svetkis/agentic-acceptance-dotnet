// TRAP: The agent breaks layered architecture, adds antipatterns, or creates duplicate decision IDs.
// GUARDRAIL: NetArchTest catches architectural dependencies.
// NOTE: The regex checks below are a starter/fallback for artifacts and temporary C# spikes.
// For stable C# semantic rules, prefer a Roslyn analyzer (Layer 1.1).
// NOTE: For EF Core-specific rules see EfCoreGuardRules.cs.
//       For Dapper-specific rules see DapperGuardRules.cs.
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

public class ArchitectureRules
{
    // TRAP: The agent referenced Infrastructure from Api directly.
    // GUARDRAIL: Api → Application → Domain. Infrastructure only via DI.
    [Test]
    public void Api_ShouldNotReferenceInfrastructureDirectly()
    {
        var result = Types.InCurrentDomain()
            .That().ResideInNamespace(".*Api.*")
            .Should().NotHaveDependencyOnAny(".*Infrastructure.*")
            .GetResult();

        Assert.That(result.IsSuccessful).IsTrue();
    }

    // TRAP: The agent created a service without an interface in Application.
    // GUARDRAIL: All services must have an interface (Port).
    [Test]
    public void Services_ShouldHaveInterfaces()
    {
        var result = Types.InCurrentDomain()
            .That().ResideInNamespace(".*Infrastructure.*")
            .And().HaveNameEndingWith("Service")
            .Should().ImplementInterface(typeof(IService))
            .GetResult();

        Assert.That(result.IsSuccessful).IsTrue();
    }

    // TRAP: The agent added mutable state to Domain via a public field/setter.
    // GUARDRAIL: BeImmutableExternally catches mutable public API (eNhancedEdition 1.4.5+).
    // NOTE: Auto-properties may not be detected — use Roslyn analyzers for precise checking.
    [Test]
    public void DomainTypes_ShouldBeImmutableExternally()
    {
        var result = Types.InCurrentDomain()
            .That().ResideInNamespace(".*Domain.*")
            .And().AreNotEnums()
            .Should().BeImmutableExternally()
            .GetResult();

        Assert.That(result.IsSuccessful).IsTrue();
    }

    // TRAP: The agent added caching without specifying a size — OOM in production.
    // GUARDRAIL: Every bare cache.Set() is caught by scanning.
    // NOTE: A universal rule, independent of the ORM.
    [Test]
    public void CacheSet_ShouldAlwaysSpecifySize()
    {
        var violations = ScanSourceFiles(
            pattern: @"(?<!Sized)\b_cache\.Set\(",
            fileGlob: "*.cs",
            whitelist: new[] { "CacheSetup.cs: explicit SizeLimit config" });

        Assert.That(violations).IsEmpty()
            .Because("MemoryCache SizeLimit requires every entry to specify .Size");
    }

    // TRAP: The agent created a duplicate ID for a documented decision.
    // GUARDRAIL: PERF-###, DB-###, AUD-### must be unique across the entire codebase.
    [Test]
    public void PerfAndDbDecisions_ShouldHaveUniqueIds()
    {
        var ids = ExtractDecisionIds("src", @"(PERF|DB|AUD)-\d{3}");
        var duplicates = ids.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);

        Assert.That(duplicates).IsEmpty()
            .Because("Decision guards must be unique to prevent collision in documentation");
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

    private static IEnumerable<string> ExtractDecisionIds(string rootDir, string regexPattern)
    {
        var path = Path.Combine("..", "..", "..", "..", rootDir);
        if (!Directory.Exists(path))
            return Array.Empty<string>();

        var regex = new Regex(regexPattern);
        var ids = new List<string>();

        foreach (var file in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in regex.Matches(text))
                ids.Add(match.Value);
        }

        return ids;
    }
}
