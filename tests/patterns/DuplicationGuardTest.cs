// TRAP: The agent copied important business logic into a new service instead of reusing it.
// GUARDRAIL: A starter regex check catches literal duplication of critical business fragments.
// LIMIT: It only catches literal duplication. Semantic duplication
// (order.IsConfirmed() vs order.Status == Confirmed) is the job of the tech lead's code-review checklist.
// See templates/skills/code-review/CHECKLIST.md → "Business logic duplication (Semantic)".

using System.Text.RegularExpressions;
using TUnit;

namespace Tests.Patterns;

public class DuplicationGuardTest
{
    // TRAP: The agent added an order status check to a new service, although it already exists in Domain.
    // GUARDRAIL: A business rule pattern appears in only one production file.
    [Test]
    public void BusinessRule_ShouldNotBeDuplicatedAcrossServices()
    {
        // Configure: the list of regex patterns of critical business logic that must be unique
        var businessPatterns = new[]
        {
            @"Status\s*==\s*BookingStatus\.Confirmed", // Example: status check
            @"Total\s*\*\s*0\.\d+",                    // Example: discount calculation
            @"DateTime\.Now",                           // Antipattern: must be UtcNow or IClock
        };

        var srcPath = Path.Combine("..", "..", "..", "..", "src");
        if (!Directory.Exists(srcPath))
            Assert.Fail("Source directory not found");

        var violations = new List<string>();

        foreach (var pattern in businessPatterns)
        {
            var matches = ScanFiles(srcPath, pattern);
            var productionFiles = matches
                .Where(m => !m.Contains("Tests"))
                .Select(m => m.Split(':')[0])
                .Distinct()
                .ToList();

            if (productionFiles.Count > 1)
            {
                violations.Add($"Pattern '{pattern}' duplicated in {productionFiles.Count} files: {string.Join(", ", productionFiles)}");
            }
        }

        Assert.That(violations).IsEmpty()
            .Because("Critical business logic must live in one place (Domain or shared service)");
    }

    // TRAP: The agent hardcoded a magic string/number in several places.
    // GUARDRAIL: Domain constants must be declared once.
    [Test]
    public void MagicValues_ShouldBeCentralized()
    {
        // Configure: magic values that must not be scattered around
        var magicPatterns = new[]
        {
            @"\"Bearer \"",          // Must be in the AuthScheme constant
            @"MaxItems\s*=\s*50",    // Must be in a domain constant
        };

        var srcPath = Path.Combine("..", "..", "..", "..", "src");
        if (!Directory.Exists(srcPath))
            Assert.Fail("Source directory not found");

        var violations = new List<string>();

        foreach (var pattern in magicPatterns)
        {
            var matches = ScanFiles(srcPath, pattern);
            var productionFiles = matches
                .Where(m => !m.Contains("Tests"))
                .Select(m => m.Split(':')[0])
                .Distinct()
                .ToList();

            if (productionFiles.Count > 1)
            {
                violations.Add($"Magic value '{pattern}' found in {productionFiles.Count} files: {string.Join(", ", productionFiles)}");
            }
        }

        Assert.That(violations).IsEmpty()
            .Because("Magic values must be declared as constants in Domain/Constants");
    }

    private static List<string> ScanFiles(string rootDir, string pattern)
    {
        var files = Directory.GetFiles(rootDir, "*.cs", SearchOption.AllDirectories);
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
