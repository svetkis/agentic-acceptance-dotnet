// GUARDRAIL: smart slot selection must keep reducing dead zones — bounded tolerance,
// not exact match. There is no single "correct" output, only average behavior.
// This file is a working adaptation of Example B in tests/patterns/CharacterizationTest.cs.
using TUnit;

namespace DemoProject.Tests;

public class SmartSelectionCharacterizationTests
{
    // TRAP: asserting an exact quality number — run-to-run variance makes it flaky,
    //       and the agent "fixes" the flake by widening tolerance until meaningless.
    // GUARDRAIL: paired runs (same seed => same clients) and a threshold FAR below
    //       the observed effect, justified by measured spread.
    [Test]
    public async Task SmartSelection_MustKeepReducingDeadZones()
    {
        const int runs = 30;
        var rng = new Random(Seed: 42);

        var reductions = Enumerable.Range(0, runs)
            .Select(_ =>
            {
                var (naive, smart) = ScheduleSimulator.RunPaired(rng.Next(), days: 28);
                return 100.0 * (naive.DeadZoneShare - smart.DeadZoneShare) / naive.DeadZoneShare;
            })
            .ToList();

        var avg = reductions.Average();
        var spread = reductions.Max() - reductions.Min();

        await Assert.That(avg).IsGreaterThanOrEqualTo(10.0)
            .Because($"Smart selection must keep reducing dead zones. " +
                     $"Avg reduction={avg:F1}%, observed spread={spread:F1} points. " +
                     $"A refactor must not degrade the algorithm to near-naive behavior. " +
                     $"(Observed at recording: ~51% avg over 30 paired runs; if this fails, " +
                     $"the packing strategy degraded — investigate before lowering the gate.)");
    }
}
