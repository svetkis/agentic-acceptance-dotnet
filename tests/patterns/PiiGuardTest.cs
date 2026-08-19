// TRAP: The agent adds logging with email, phone, password —
// PII leaks into the logging system (Elastic, Kibana, Seq).
// GUARDRAIL: [SensitiveData] inventory + starter regex checks for Log* calls.
// NOTE: For stable checking of C# logging, prefer a Roslyn analyzer.

using System.Reflection;
using System.Text.RegularExpressions;
using TUnit;

namespace Tests.Patterns;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field)]
public class SensitiveDataAttribute : Attribute { }

public class PiiGuardTest
{
    // TRAP: The agent created an Email/Phone/Password property without [SensitiveData].
    // GUARDRAIL: All PII fields must have [SensitiveData].
    [Test]
    public void AllPiiProperties_ShouldHaveSensitiveDataAttribute()
    {
        var piiPatterns = new[] { @"Email", @"Phone", @"Password", @"Token", @"Secret", @"Card" };
        var violations = new List<string>();

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Namespace?.Contains("Domain") == true || t.Namespace?.Contains("Application") == true);

        foreach (var type in types)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (piiPatterns.Any(p => Regex.IsMatch(prop.Name, p, RegexOptions.IgnoreCase)))
                {
                    var hasAttr = prop.GetCustomAttribute<SensitiveDataAttribute>() != null;
                    if (!hasAttr)
                    {
                        violations.Add($"{type.FullName}.{prop.Name}");
                    }
                }
            }
        }

        Assert.That(violations).IsEmpty()
            .Because("Properties matching PII patterns must have [SensitiveData] attribute");
    }

    // TRAP: The agent uses string interpolation in Log* — data ends up in the log.
    // GUARDRAIL: Log* calls must not contain $"..." (interpolated strings).
    [Test]
    public void LogCalls_ShouldNotUseInterpolatedStrings()
    {
        var violations = ScanSourceFiles(
            rootDir: "..",
            fileGlob: "*.cs",
            pattern: @"\b(_logger|logger|Log)\.(LogInformation|LogDebug|LogWarning|LogError|LogTrace)\s*\(\s*\$""",
            whitelist: new[] { "PiiGuardTest.cs: test itself uses interpolation" });

        Assert.That(violations).IsEmpty()
            .Because("Logging must use structured templates, not interpolated strings. " +
                     "Use _logger.LogInformation(\"User {UserId} logged in\", user.Id) instead");
    }

    // TRAP: The agent passes email/phone/password to Log* as an argument.
    // GUARDRAIL: Log* calls must not contain variables with PII names.
    [Test]
    public void LogCalls_ShouldNotContainPiiVariables()
    {
        var piiVariables = new[] { @"email", @"phone", @"password", @"token", @"secret", @"cardLast4", @"ssn" };
        var pattern = $@"\b(_logger|logger|Log)\.(LogInformation|LogDebug|LogWarning|LogError|LogTrace)\s*\([^)]*\b({string.Join("|", piiVariables)})\b";

        var violations = ScanSourceFiles(
            rootDir: "..",
            fileGlob: "*.cs",
            pattern: pattern,
            whitelist: new[] { "PiiGuardTest.cs: test scans for patterns" });

        Assert.That(violations).IsEmpty()
            .Because("PII variables must not be passed to logging methods. " +
                     "Log identifiers (UserId) instead of sensitive data");
    }

    // TRAP: The agent added a PII field but did not grow the [SensitiveData] inventory.
    // GUARDRAIL: The number of [SensitiveData] properties does not decrease (ratchet).
    [Test]
    public void SensitiveDataAttributes_ShouldNotDecrease()
    {
        var currentCount = CountSensitiveDataAttributes("..");
        var baseline = GetBaselineOrSet(currentCount);

        Assert.That(currentCount).IsGreaterThanOrEqualTo(baseline)
            .Because("[SensitiveData] attributes must not decrease. " +
                     "Every new PII field requires the attribute. Current={0}, Baseline={1}",
                     currentCount, baseline);
    }

    // --- Helpers ---

    private static IEnumerable<string> ScanSourceFiles(string rootDir, string fileGlob, string pattern, string[] whitelist)
    {
        var violations = new List<string>();
        if (!Directory.Exists(rootDir))
            return violations;

        var files = Directory.GetFiles(rootDir, fileGlob, SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("/obj/"))
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("/bin/"));

        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    var location = $"{file}:{i + 1}";
                    if (whitelist.Any(w => location.Contains(w.Split(':')[0])))
                        continue;

                    violations.Add(location);
                }
            }
        }

        return violations;
    }

    private static int CountSensitiveDataAttributes(string rootDir)
    {
        if (!Directory.Exists(rootDir))
            return 0;

        var files = Directory.GetFiles(rootDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("/obj/"));

        var count = 0;
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            // Count [SensitiveData] occurrences
            count += Regex.Matches(text, @"\[\s*SensitiveData\s*\]").Count;
        }

        return count;
    }

    // NOTE: In CI, ensure pii-baseline.txt is committed or the ratchet becomes a no-op.
    // Consider using a path relative to the project root instead of CurrentDirectory.
    private static int GetBaselineOrSet(int current)
    {
        var baselineFile = "pii-baseline.txt";
        if (File.Exists(baselineFile) && int.TryParse(File.ReadAllText(baselineFile), out var baseline))
            return baseline;

        File.WriteAllText(baselineFile, current.ToString());
        return current;
    }
}
