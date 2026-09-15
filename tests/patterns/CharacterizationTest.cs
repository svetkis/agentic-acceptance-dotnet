// TRAP: There is a piece of code nobody dares to touch — the discount engine,
//       the working-hours calculator, the legacy CSV import, the report builder.
//       The spec is long lost; the code IS the spec. The agent refactors it
//       "for readability", silently changes a boundary case (discount applied
//       at exactly 10 items, DST-shifted working hours), and no unit test fails
//       because no test ever encoded the old behavior.
// GUARDRAIL: Before touching spec-less code, record its CURRENT behavior as a
//       golden master (characterization test): deterministic inputs in,
//       committed outputs out. The refactor must reproduce the golden master
//       byte-for-byte. Only THEN turn newly-discovered bugs into spec tests.
//
// When it applies (more often than "exotic algorithms only"):
// - any calculation whose spec is lost or lives in one person's head;
// - replacing an implementation (EF6 -> EF Core query rewrite, regex -> parser);
// - anything where you cannot say WHAT the correct output is, only that it must
//   not change TODAY.
// When it does NOT apply: code with a living spec — write a normal spec test instead.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(...)
// - xUnit:  [Fact] + Assert.Equal(...)
// - NUnit:  [Test] + Assert.That(..., Is.EqualTo)
// - MSTest: [TestMethod] + Assert.AreEqual
//
// Workflow: (1) generate golden master with REGENERATE=true against the OLD code,
// commit the JSON; (2) refactor; (3) same suite must stay green against the NEW code.

using System.Text.Json;
using TUnit;

namespace Tests.Patterns;

public class DiscountEngineCharacterizationTests
{
    // Path to the committed golden master. It is DATA, checked into the repo.
    private const string GoldenMasterPath = "golden/discount-engine.json";

    // Regenerate switch: true only on step (1), against the OLD implementation.
    // TRAP: the agent flips this to true after a refactor to "fix" the red test —
    //       that deletes the only witness of the old behavior.
    // GUARDRAIL: regeneration is a deliberate human action; the golden master is
    //       never updated in the same PR as the refactor it is supposed to judge.
    private const bool Regenerate = false;

    // TRAP: golden master recorded only on hand-picked happy-path inputs —
    //       the boundary cases (0 items, 1 item, exactly-at-threshold) are the
    //       ones a refactor breaks, and they are exactly the ones nobody seeds.
    // GUARDRAIL: generate inputs systematically — boundaries first, then a fixed-seed
    //       sweep over the realistic domain.
    [Test]
    public void DiscountEngine_Refactor_MustReproduceGoldenMaster()
    {
        var inputs = GenerateInputs();                    // deterministic, see below
        var actual = inputs
            .Select(i => new RecordedCase(i, YourDiscountEngine.Apply(i)))
            .ToList();

        if (Regenerate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GoldenMasterPath)!);
            File.WriteAllText(GoldenMasterPath, Serialize(actual));
            return; // commit the file, flip Regenerate back to false
        }

        var golden = JsonSerializer.Deserialize<List<RecordedCase>>(
            File.ReadAllText(GoldenMasterPath))!;

        Assert.That(actual.Count).IsEqualTo(golden.Count)
            .Because("A refactor must not change the number of cases produced.");

        var diffs = actual.Zip(golden)
            .Where(pair => !pair.First.Equals(pair.Second))
            .Select(pair => $"input={pair.First.Input}: old={pair.Second.Output}, new={pair.First.Output}")
            .ToList();

        Assert.That(diffs).IsEmpty()
            .Because("Behavior changed during a no-behavior-change refactor:\n" +
                     string.Join("\n", diffs));
    }

    // --- Input generation: deterministic, boundary-first ---
    private static List<OrderInput> GenerateInputs()
    {
        var boundaries = new[]
        {
            new OrderInput(Total: 0m, Items: 0, IsVip: false),   // degenerate
            new OrderInput(Total: 0m, Items: 1, IsVip: true),    // zero total + flag
            new OrderInput(Total: 100m, Items: 9, IsVip: false), // one below threshold
            new OrderInput(Total: 100m, Items: 10, IsVip: false),// exactly at threshold
            new OrderInput(decimal.MaxValue, Items: 999, IsVip: true), // overflow-prone
        };

        // Fixed seed: the sweep must be IDENTICAL on every machine and every run,
        // otherwise the golden master is unreproducible garbage.
        var rng = new Random(seed: 42);
        var sweep = Enumerable.Range(0, 500).Select(_ => new OrderInput(
            Total: Math.Round((decimal)(rng.NextDouble() * 10_000), 2),
            Items: rng.Next(1, 100),
            IsVip: rng.Next(0, 2) == 1));

        return boundaries.Concat(sweep).ToList();
    }

    private static string Serialize(List<RecordedCase> cases) =>
        JsonSerializer.Serialize(cases, new JsonSerializerOptions { WriteIndented = true });

    // Records with value equality so Zip-compare works without custom comparers.
    private readonly record struct RecordedCase(OrderInput Input, decimal Output);
    private readonly record struct OrderInput(decimal Total, int Items, bool IsVip);
}

// --- Nondeterministic code: pin the environment first ---
// If the recorded behavior depends on time, culture, or randomness, the golden
// master is flaky by construction. Pin them (TimeProvider fake, CultureInfo.InvariantCulture,
// seeded Random) BEFORE recording — otherwise the agent will "fix" flakiness by
// widening a tolerance until the test is meaningless.

// --- Numeric/algorithms with variance: bounded tolerance, not exact match ---
// For averages/percentiles (e.g. scheduler quality metrics) exact equality is
// impossible: assert a range derived from observed variance (see docs on the
// Monte Carlo acceptance example: observed ~61%, threshold set at 20%).
// TRAP: tolerance wide enough to hide a real regression ("at least -1%").
// GUARDRAIL: tolerance is justified by measured run-to-run variance, not chosen
// to make the test pass.
