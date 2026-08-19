// GUARDRAIL: Regex source scanning catches duplication of critical business fragments.
// This file is a working adaptation of the template from tests/patterns/DuplicationGuardTest.cs

using System.Text.RegularExpressions;
using TUnit;

namespace DemoProject.Tests;

public class DuplicationGuardTest
{
    // TRAP: An agent added booking status validation to a new service instead of reusing the existing one.
    // GUARDRAIL: The business rule pattern appears in only one production file.
    [Test]
    public async Task BusinessRule_ShouldNotBeDuplicatedAcrossServices()
    {
        var businessPatterns = new[]
        {
            @"Status\s*==\s*BookingStatus\.Confirmed",
            @"DateTime\.Now",
        };

        var violations = new List<string>();

        foreach (var pattern in businessPatterns)
        {
            var matches = ScanFiles(pattern);
            var productionFiles = matches
                .Select(m => m.Split(':')[0])
                .Distinct()
                .ToList();

            if (productionFiles.Count > 1)
            {
                violations.Add($"Pattern '{pattern}' duplicated in {productionFiles.Count} files: {string.Join(", ", productionFiles)}");
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("Critical business logic must live in one place (Domain or shared service)");
    }

    private static List<string> ScanFiles(string pattern)
    {
        var srcPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src");
        srcPath = Path.GetFullPath(srcPath);

        if (!Directory.Exists(srcPath))
            return new List<string>();

        var files = Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories);
        var regex = new Regex(pattern);
        var matches = new List<string>();

        foreach (var file in files)
        {
            if (file.Contains("obj") || file.Contains("bin"))
                continue;

            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    var relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                    matches.Add($"{relative}:{i + 1}");
                }
            }
        }

        return matches;
    }
}
