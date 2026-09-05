// TRAP: The decision registry (DECISION-GUARDS.md) rots silently. An agent refactors code,
//        deletes or renames a file, or adds a decision ID in code without a registry entry —
//        nothing fails. Months later an agent meets a stale ID, opens the registry, and
//        either finds nothing or finds a link to a file that no longer exists. The registry
//        is the least-read artifact, so it decays first.
// GUARDRAIL: An architecture test parses the registry markdown and enforces the link in BOTH
//        directions: every registry entry's "Where in code" path must exist, and the decision
//        ID must literally appear in that file. Uniqueness of IDs is checked for free.
//
// This is an artifact scan: no C# semantic model is needed, a regex over markdown suffices.
// It runs as a normal test — the registry is verified on every `dotnet test`, not in an audit.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(condition).IsTrue()
// - xUnit:  [Fact] + Assert.True(condition, message)
// - NUnit:  [Test] + Assert.That(condition, Is.True, message)
// - MSTest: [TestMethod] + Assert.IsTrue(condition, message)

using System.Text.RegularExpressions;

namespace Tests.Patterns;

public class DecisionGuardLinkTests
{
    // Adapt to your ID prefixes and repository-root marker file.
    private const string IdPattern = @"^### ((?:PERF|DB|ARCH|AUD|COMPLEXITY)-\d{3}):";

    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "YourSolution.sln")))
            {
                dir = dir.Parent;
            }

            Assert.That(dir is not null).IsTrue(); // marker file not found above the test assembly
            return dir!.FullName;
        }
    }

    private static string RegistryText =>
        File.ReadAllText(Path.Combine(RepoRoot, "docs", "DECISION-GUARDS.md"));

    private static List<(string Id, string Block)> Entries()
    {
        // Entries are declared only by "### PERF-001: ..." headings — adapt to your registry format.
        return Regex.Split(RegistryText, @"^### ", RegexOptions.Multiline)
            .Skip(1)
            .Select(block => (Regex.Match(block, IdPattern).Groups[1].Value, block))
            .Where(t => t.Item1.Length > 0)
            .ToList();
    }

    // TRAP: Two decisions share an ID — the "read the registry" protocol becomes ambiguous.
    // GUARDRAIL: IDs parsed from headings must be unique, and at least one entry must parse
    //        (a registry whose format broke parses as zero entries and would otherwise pass vacuously).
    [Test]
    public void DecisionIds_ShouldBeUnique()
    {
        var ids = Entries().Select(e => e.Id).ToList();

        Assert.That(ids.Count > 0).IsTrue(); // zero entries = broken heading format, not a clean registry

        var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.That(duplicates.Count == 0).IsTrue(); // duplicate decision IDs
    }

    // TRAP: The agent renames/deletes a file referenced by the registry. The entry survives
    //        as a plausible-looking but dead link.
    // GUARDRAIL: Every "Where in code" path must resolve to an existing file.
    [Test]
    public void RegistryCodeLinks_ShouldPointToExistingFiles()
    {
        // "**Where in code:** `src/.../File.cs:12-34`" — take the path before the line numbers.
        var missing = Entries()
            .Select(e => Regex.Match(e.Block, @"\*\*Where in code:\*\*\s*`([^`:]+)"))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value.Trim().Split(' ')[0])
            .Distinct()
            .Where(relative => !File.Exists(Path.Combine(RepoRoot, relative)))
            .ToList();

        Assert.That(missing.Count == 0).IsTrue(); // registry links to non-existent files
    }

    // TRAP: The registry entry exists, the file exists, but the ID itself is gone from the code.
    //        This is the worst case: the registry looks maintained, yet nothing in the code ever
    //        stops an agent at the point of change. File-existence checks stay green.
    // GUARDRAIL: The ID must literally appear in the referenced file (doc comment next to the
    //        deviation). Checks both directions of the link; a non-existent file is reported by
    //        the test above and skipped here.
    [Test]
    public void DecisionId_ShouldAppearInLinkedFile()
    {
        var broken = new List<string>();
        var checkedLinks = 0;

        foreach (var (id, block) in Entries())
        {
            var where = Regex.Match(block, @"\*\*Where in code:\*\*\s*`([^`:]+)");
            if (!where.Success)
            {
                continue;
            }

            var path = Path.Combine(RepoRoot, where.Groups[1].Value.Trim().Split(' ')[0]);
            if (!File.Exists(path))
            {
                continue;
            }

            checkedLinks++;

            if (!File.ReadAllText(path).Contains(id, StringComparison.Ordinal))
            {
                broken.Add($"{id} → {where.Groups[1].Value}");
            }
        }

        Assert.That(checkedLinks > 0).IsTrue(); // zero parsed links = broken registry format
        Assert.That(broken.Count == 0).IsTrue(); // IDs missing from their linked files — nothing stops the agent at the point of change
    }
}
