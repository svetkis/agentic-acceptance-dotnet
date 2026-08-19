// TRAP: The agent writes non-parameterized SQL, uses string interpolation in queries,
// or forgets timeouts in Dapper calls.
// GUARDRAIL: Starter regex checks catch obvious SQL injections and Dapper antipatterns.
// NOTE: For stable C# semantic rules prefer a Roslyn analyzer; for SQL — parser/DB audit.
// This file is only for projects using Dapper / Raw SQL. For EF Core see EfCoreGuardRules.cs.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(violations).IsEmpty()
// - xUnit:  [Fact] + Assert.Empty(violations)
// - NUnit:  [Test] + Assert.That(violations, Is.Empty)
// - MSTest: [TestMethod] + Assert.AreEqual(0, violations.Count())

using System.Text.RegularExpressions;
using TUnit;

namespace Tests.Patterns;

public class DapperGuardRules
{
    // TRAP: The agent used C# string interpolation ($"...") in an SQL query.
    // GUARDRAIL: Any string interpolation in SQL is a potential injection.
    [Test]
    public void RawSql_ShouldNotUseStringInterpolation()
    {
        var violations = ScanSourceFiles(
            pattern: @"\$""[^""]*\{[^}]+\}[^""]*""",
            fileGlob: "*.cs",
            whitelist: new[] { "Migration", "SeedData", "Comment" });

        Assert.That(violations).IsEmpty()
            .Because("SQL queries must use parameterized statements (@param), never C# string interpolation");
    }

    // TRAP: The agent concatenated user input into an SQL string.
    // GUARDRAIL: Concatenating strings with SQL keywords is forbidden.
    [Test]
    public void RawSql_ShouldNotUseStringConcatenation()
    {
        var violations = ScanSourceFiles(
            pattern: @"(SELECT|INSERT|UPDATE|DELETE|FROM|WHERE|JOIN)\s*[^""]*\+\s*",
            fileGlob: "*.cs",
            whitelist: new[] { "Migration", "SeedData" });

        Assert.That(violations).IsEmpty()
            .Because("SQL must be static or parameterized. Concatenation enables injection.");
    }

    // TRAP: The agent called QueryAsync / ExecuteAsync without commandTimeout — a risk of waiting forever.
    // GUARDRAIL: Every Dapper call must explicitly pass a timeout or use a global default.
    [Test]
    public void DapperCalls_ShouldHaveCommandTimeout()
    {
        // Look for Dapper calls without the third commandTimeout argument
        // Examples: connection.QueryAsync<Order>(sql, param) — violation
        //           connection.QueryAsync<Order>(sql, param, commandTimeout: 30) — ok
        var violations = ScanSourceFiles(
            pattern: @"\.(QueryAsync|ExecuteAsync|QueryFirstAsync|QuerySingleAsync)<.*?>\s*\([^,]+,[^,]+\)",
            fileGlob: "*.cs",
            whitelist: new[] { "GlobalCommandTimeout.cs: default timeout configured" });

        Assert.That(violations).IsEmpty()
            .Because("Dapper calls must specify commandTimeout to prevent hanging queries");
    }

    // TRAP: The agent built a dynamic IN clause via string.Join without a whitelist.
    // GUARDRAIL: IN with a dynamic list — only via TVP or ORM generation.
    [Test]
    public void DynamicInClause_ShouldBeParameterized()
    {
        var violations = ScanSourceFiles(
            pattern: @"string\.Join\s*\(\s*"",""\s*,\s*\w+\s*\).*?(IN|in)\s*\(",
            fileGlob: "*.cs",
            whitelist: Array.Empty<string>());

        Assert.That(violations).IsEmpty()
            .Because("Dynamic IN clauses must use Table-Valued Parameters (TVP) or parameterized ORM, not string.Join");
    }

    // TRAP: The agent used FromSqlRaw / ExecuteSqlRaw with interpolation in an EF project.
    // GUARDRAIL: Even in EF, raw SQL must be parameterized (FromSqlInterpolated).
    // NOTE: This is an EF + raw SQL overlap — placed in the Dapper file because it concerns raw SQL hygiene.
    [Test]
    public void EfRawSql_ShouldNotUseInterpolation()
    {
        var violations = ScanSourceFiles(
            pattern: @"FromSqlRaw\s*\(\s*\$",
            fileGlob: "*.cs",
            whitelist: Array.Empty<string>());

        Assert.That(violations).IsEmpty()
            .Because("Use FromSqlInterpolated for parameterized raw SQL. FromSqlRaw with $ is injection-prone.");
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
            // Skip generated and test files
            if (file.Contains("obj") || file.Contains("bin") || file.Contains("Tests"))
                continue;

            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                    var location = $"{relativePath}:{i + 1}";

                    // Simple whitelist check
                    if (whitelist.Any(w => location.Contains(w.Split(':')[0])))
                        continue;

                    violations.Add(location);
                }
            }
        }

        return violations;
    }
}
