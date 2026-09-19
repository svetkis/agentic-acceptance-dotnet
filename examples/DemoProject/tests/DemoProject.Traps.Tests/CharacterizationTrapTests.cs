// TRAP: the agent "fixed" the legacy quirks during a refactor that was supposed
//       to change nothing: the 20:45 off-by-one is gone and 15-minute gaps are
//       now returned. Without a golden master nobody can prove behavior changed.
// GUARDRAIL: the characterization test compares against the behavior RECORDED
//       from the legacy code (before the refactor). This test MUST FAIL —
//       the refactored search returns 20:45 and the 15-minute gap window.
using System.Globalization;
using TUnit;

namespace DemoProject.Traps.Tests;

public class CharacterizationTrapTests
{
    // Recorded from the LEGACY implementation (DemoProject.Tests/LegacySlotSearch):
    // invariant "HH:mm" format — culture-sensitive formats do not reproduce across OSes.
    private const string EmptyMondayLegacy =
        "09:00|09:15|09:30|09:45|10:00|10:15|10:30|10:45|11:00|11:15|11:30|11:45|12:00|12:15|12:30|12:45|" +
        "13:00|13:15|13:30|13:45|14:00|14:15|14:30|14:45|15:00|15:15|15:30|15:45|16:00|16:15|16:30|16:45|" +
        "17:00|17:15|17:30|17:45|18:00|18:15|18:30|18:45|19:00|19:15|19:30|19:45|20:00|20:15|20:30";

    // Legacy dropped the 15-minute hole between 10:00+15=10:15 and 10:30.
    private const string BookingWithHoleLegacy =
        "09:00|09:15|09:30|10:45|11:00|11:15|11:30|11:45|12:00|12:15|12:30|12:45|13:00|13:15|13:30|13:45|" +
        "14:00|14:15|14:30|14:45|15:00|15:15|15:30|15:45|16:00|16:15|16:30|16:45|17:00|17:15|17:30|17:45|" +
        "18:00|18:15|18:30|18:45|19:00|19:15|19:30|19:45|20:00|20:15|20:30";

    private static readonly DateOnly Monday = new(2026, 3, 30);

    [Test]
    public async Task Refactor_MustReproduceLegacyBehavior_EmptyDay()
    {
        var actual = string.Join("|", RefactoredSlotSearch
            .GetFreeWindows(Monday, [])
            .Select(w => w.ToString("HH:mm", CultureInfo.InvariantCulture)));

        await Assert.That(actual).IsEqualTo(EmptyMondayLegacy)
            .Because("the refactored search must reproduce the legacy golden master " +
                     "(including the 20:45 quirk the 'fix' removed)");
    }

    [Test]
    public async Task Refactor_MustReproduceLegacyBehavior_DeadZoneHole()
    {
        var actual = string.Join("|", RefactoredSlotSearch
            .GetFreeWindows(Monday, [new TimeOnly(10, 0), new TimeOnly(10, 30)])
            .Select(w => w.ToString("HH:mm", CultureInfo.InvariantCulture)));

        await Assert.That(actual).IsEqualTo(BookingWithHoleLegacy)
            .Because("the legacy search dropped sub-30-minute gaps; the refactor " +
                     "'cleaned that up' — behavior changed");
    }
}
