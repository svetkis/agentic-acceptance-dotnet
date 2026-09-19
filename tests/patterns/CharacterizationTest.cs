// TRAP: There is a piece of code nobody dares to touch — the free-slot search,
//       the working-hours calculator, the discount engine, the legacy CSV import.
//       The spec is long lost; the code IS the spec. The agent refactors it
//       "for readability", silently changes a boundary case (a slot at the day
//       edge, DST-shifted working hours, a booking adjacent to another), and no
//       unit test fails because no test ever encoded the old behavior.
// GUARDRAIL: Before touching spec-less code, record its CURRENT behavior as a
//       golden master (characterization test): deterministic inputs in,
//       committed outputs out. The refactor must reproduce the golden master
//       byte-for-byte. Only THEN turn newly-discovered bugs into spec tests.
//
// When it applies (more often than "exotic algorithms only"):
// - any calculation whose spec is lost or lives in one person's head;
// - replacing an implementation (EF6 -> EF Core query rewrite, regex -> parser);
// - algorithms whose CORRECTNESS is statistical (scheduler quality): there is no
//   single right output, only average behavior — see the tolerance example below;
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

// ---------------------------------------------------------------------------
// Example A (byte-for-byte): the free-slot search — deterministic core
// ---------------------------------------------------------------------------
// The flagship case: "give me the free windows for a master". Deterministic:
// same day + same bookings => same slots, so the golden master is exact.
public class AvailableSlotsCharacterizationTests
{
    // Path to the committed golden master. It is DATA, checked into the repo.
    // NOTE: test runners' working directory varies — anchor the path to the
    // solution/test-project root (e.g. via TestContext / assembly location).
    private const string GoldenMasterPath = "golden/available-slots.json";

    // Regenerate switch: true only on step (1), against the OLD implementation.
    // TRAP: the agent flips this to true after a refactor to "fix" the red test —
    //       that deletes the only witness of the old behavior.
    // GUARDRAIL: regeneration is a deliberate human action; the golden master is
    //       never updated in the same PR as the refactor it is supposed to judge.
    private const bool Regenerate = false;

    // TRAP: golden master recorded only on hand-picked happy-path inputs —
    //       the boundary cases are the ones a refactor breaks, and they are
    //       exactly the ones nobody seeds.
    // GUARDRAIL: generate inputs systematically — boundaries first, then a
    //       fixed-seed sweep over the realistic domain.
    [Test]
    public void AvailableSlots_Refactor_MustReproduceGoldenMaster()
    {
        var inputs = GenerateInputs();
        var actual = inputs
            .Select(i => new RecordedCase(i, string.Join("|",
                YourSlotSearch.GetFreeWindows(i.Day, i.Bookings).Select(w => w.ToString("t"))))
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

        // Compare OUTPUT strings only: record value-equality does not extend
        // through array fields (TimeOnly[] compares by reference), so a full
        // record compare would flag every case after deserialization.
        var diffs = actual.Zip(golden)
            .Where(pair => pair.First.Output != pair.Second.Output)
            .Select(pair => $"input={pair.First.Input}: old={pair.Second.Output}, new={pair.First.Output}")
            .ToList();

        Assert.That(diffs).IsEmpty()
            .Because("Behavior changed during a no-behavior-change refactor:\n" +
                     string.Join("\n", diffs));
    }

    // --- Input generation: deterministic, boundary-first ---
    private static List<DayInput> GenerateInputs()
    {
        var workDay = new DateOnly(2026, 3, 30); // a regular Monday
        var boundaries = new[]
        {
            new DayInput(workDay, []),                                  // empty day
            new DayInput(workDay, BookedFullDay()),                     // fully booked
            new DayInput(workDay, [At(9, 0)]),                          // first slot taken
            new DayInput(workDay, [At(20, 45)]),                        // last slot taken
            new DayInput(workDay, [At(9, 0), At(9, 30)]),               // adjacent pair
            new DayInput(new DateOnly(2026, 3, 29), []),                // Sunday / non-working
            new DayInput(workDay.AddDays(1), []),                       // day after DST shift (EU)
        };

        // Fixed seed: the sweep must be IDENTICAL on every machine and every run,
        // otherwise the golden master is unreproducible garbage.
        var rng = new Random(Seed: 42);
        var sweep = Enumerable.Range(0, 200).Select(_ => new DayInput(
            workDay.AddDays(rng.Next(0, 30)),
            RandomBookings(rng)));

        return boundaries.Concat(sweep).ToList();
    }

    private static TimeOnly At(int h, int m) => new(h, m);

    private static TimeOnly[] BookedFullDay() =>
        Enumerable.Range(0, 48).Select(i => new TimeOnly(9, 0).AddMinutes(i * 15)).ToArray(); // 9:00-20:45

    private static TimeOnly[] RandomBookings(Random rng) =>
        Enumerable.Range(0, rng.Next(1, 8))
            .Select(_ => new TimeOnly(0, 0).AddMinutes(rng.Next(0, 96) * 15))
            .Distinct()
            .OrderBy(t => t)
            .ToArray();

    private static string Serialize(List<RecordedCase> cases) =>
        JsonSerializer.Serialize(cases, new JsonSerializerOptions { WriteIndented = true });

    // Input is kept for the failure message only (see the compare note above).
    private readonly record struct RecordedCase(DayInput Input, string Output);
    private readonly record struct DayInput(DateOnly Day, TimeOnly[] Bookings);
}

// ---------------------------------------------------------------------------
// Example B (bounded tolerance): smart slot selection — statistical correctness
// ---------------------------------------------------------------------------
// The other half of the same domain: the SMART selection algorithm that assigns
// clients to windows to minimize dead zones (gaps too short to be useful).
// There is no single correct output — only average behavior — so a byte-for-byte
// golden master is impossible. Instead: paired Monte Carlo runs, same seeds,
// smart vs naive baseline, and a threshold on the metric with a WIDE margin.
public class SmartSelectionCharacterizationTests
{
    // TRAP: asserting an exact quality number ("dead zones reduced by exactly 61%")
    //       — run-to-run variance makes it flaky, and the agent "fixes" the flake
    //       by widening the tolerance until the test is meaningless.
    // GUARDRAIL: paired runs (same seed => same clients), and the threshold sits
    //       FAR below the observed effect with headroom for run-to-run variance.
    //       Tolerance is justified by MEASURED variance, not chosen to pass.
    [Test]
    public void SmartSelection_MustKeepReducingDeadZones()
    {
        const int runs = 30;
        var rng = new Random(Seed: 42);

        var reductions = Enumerable.Range(0, runs)
            .Select(_ =>
            {
                var (baseline, smart) = ScheduleSimulator.RunPaired(rng.Next(), days: 28);
                return 100.0 * (baseline.DeadZoneShare - smart.DeadZoneShare) / baseline.DeadZoneShare;
            })
            .ToList();

        var avgReduction = reductions.Average();
        var observedSpread = reductions.Max() - reductions.Min();

        // Observed during recording: ~61% average reduction, spread ~8 points.
        // Threshold 20% keeps a margin wide enough that a REAL regression
        // (algorithm degraded to near-naive) fails loudly, while seed noise passes.
        Assert.That(avgReduction).IsGreaterThanOrEqualTo(20.0)
            .Because($"Smart selection must keep reducing dead zones. " +
                     $"Avg reduction={avgReduction:F1}%, observed spread={observedSpread:F1} points. " +
                     $"A refactor must not degrade the algorithm to near-naive behavior.");
    }
}

// --- Nondeterministic code: pin the environment first ---
// If the recorded behavior depends on time, culture, or randomness, the golden
// master is flaky by construction. Pin them (TimeProvider fake, CultureInfo.InvariantCulture,
// seeded Random) BEFORE recording — otherwise the agent will "fix" flakiness by
// widening a tolerance until the test is meaningless.

// --- Placeholder types: replace with your real legacy code ---

public static class YourSlotSearch
{
    // The spec-less legacy: given a day and bookings, return free windows.
    public static IEnumerable<TimeOnly> GetFreeWindows(DateOnly day, TimeOnly[] bookings) => [];
}

public static class ScheduleSimulator
{
    // Paired simulation: same clients (same seed), naive vs smart assignment.
    public static (Result Naive, Result Smart) RunPaired(int seed, int days) => default;

    public readonly record struct Result(double DeadZoneShare);
}
