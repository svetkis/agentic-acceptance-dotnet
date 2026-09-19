// GUARDRAIL: before touching the spec-less LegacySlotSearch, its CURRENT behavior
// is recorded as a golden master. A refactor must reproduce it byte-for-byte —
// including the quirks (Sunday empty, sub-30-min gaps dropped, the 20:45 bug).
// This file is a working adaptation of the template from tests/patterns/CharacterizationTest.cs.

using System.Text.Json;
using TUnit;

namespace DemoProject.Tests;

public class AvailableSlotsCharacterizationTests
{
    // Anchored to the test project dir (three levels above the build output),
    // not to the runner's CWD.
    private static readonly string ProjectDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    private static readonly string GoldenMasterPath = Path.Combine(ProjectDir, "GoldenMasters", "available-slots.json");

    // Regenerate: true ONLY against the OLD implementation; commit the JSON, flip back.
    // The golden master is never updated in the same PR as the refactor it judges.
    // static readonly, not const: a const-false branch is compile-time unreachable
    // and fails the build under TreatWarningsAsErrors (CS0162).
    private static readonly bool Regenerate = false;

    [Test]
    public async Task AvailableSlots_Refactor_MustReproduceGoldenMaster()
    {
        var inputs = GenerateInputs();
        var actual = inputs
            .Select(i => new RecordedCase(i, Serialize(LegacySlotSearch.GetFreeWindows(i.Day, i.Bookings))))
            .ToList();

        if (Regenerate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GoldenMasterPath)!);
            await File.WriteAllTextAsync(GoldenMasterPath,
                JsonSerializer.Serialize(actual, new JsonSerializerOptions { WriteIndented = true }));
            // No early return: SAE008 (NonValidatingTestAnalyzer) forbids successful
            // paths without assertions. Falling through also proves the recording
            // round-trips through serialization.
        }

        var golden = JsonSerializer.Deserialize<List<RecordedCase>>(
            await File.ReadAllTextAsync(GoldenMasterPath))!;

        await Assert.That(actual.Count).IsEqualTo(golden.Count)
            .Because("A refactor must not change the number of cases produced.");

        // Output-only compare: record value-equality does not extend through the
        // TimeOnly[] array field (arrays compare by reference).
        var diffs = actual.Zip(golden)
            .Where(pair => pair.First.Output != pair.Second.Output)
            .Select(pair => $"{pair.First.Day:yyyy-MM-dd}: old={pair.Second.Output}, new={pair.First.Output}")
            .ToList();

        await Assert.That(diffs).IsEmpty()
            .Because("Behavior changed during a no-behavior-change refactor:\n" +
                     string.Join("\n", diffs));
    }

    // Boundary-first: the day edges and quirks are exactly what a refactor breaks.
    private static List<DayInput> GenerateInputs()
    {
        var monday = new DateOnly(2026, 3, 30);

        var boundaries = new List<DayInput>
        {
            new(monday, []),                                   // empty day
            new(monday, FullDay()),                            // fully booked
            new(monday, [At(9, 0)]),                           // first slot taken
            new(monday, [At(20, 45)]),                         // last slot taken (the never-returned one)
            new(monday, [At(9, 0), At(9, 30)]),                // adjacent pair
            new(monday, [At(12, 0)]),                          // single midday gap of 3h
            new(monday, [At(10, 0), At(10, 30)]),              // 15-min hole -> dead zone
            new(new DateOnly(2026, 3, 29), []),                // Sunday -> no windows
            new(monday.AddDays(1), []),                        // day after the EU DST shift
        };

        // Fixed seed: the sweep must be identical on every machine and every run.
        var rng = new Random(Seed: 42);
        var sweep = Enumerable.Range(0, 200).Select(_ => new DayInput(
            monday.AddDays(rng.Next(0, 30)),
            RandomBookings(rng)));

        return boundaries.Concat(sweep).ToList();
    }

    private static TimeOnly At(int h, int m) => new(h, m);

    private static TimeOnly[] FullDay() =>
        Enumerable.Range(0, 48).Select(i => new TimeOnly(9, 0).AddMinutes(i * 15)).ToArray();

    private static TimeOnly[] RandomBookings(Random rng) =>
        Enumerable.Range(0, rng.Next(1, 8))
            .Select(_ => new TimeOnly(9, 0).AddMinutes(rng.Next(0, 48) * 15))
            .Distinct()
            .OrderBy(t => t)
            .ToArray();

    private static string Serialize(IEnumerable<TimeOnly> windows) =>
        string.Join("|", windows.Select(w => w.ToString("t")));

    // Input kept for the failure message only (see the compare note above).
    private readonly record struct RecordedCase(DayInput Input, string Output)
    {
        public DateOnly Day => Input.Day;
        public TimeOnly[] Bookings => Input.Bookings;
    }

    private readonly record struct DayInput(DateOnly Day, TimeOnly[] Bookings);
}
