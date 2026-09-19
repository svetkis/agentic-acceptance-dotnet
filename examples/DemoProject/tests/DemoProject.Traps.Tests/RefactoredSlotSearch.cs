// TRAP fixture: a "refactored" slot search where the legacy 20:45 off-by-one
// was FIXED and the sub-30-minute gap rule was "cleaned up". Behavior changed
// during a no-behavior-change refactor — the characterization guard must catch it.
using DemoProject.Traps;

namespace DemoProject.Traps.Tests;

public static class RefactoredSlotSearch
{
    public static readonly TimeOnly DayStart = new(9, 0);
    public static readonly TimeOnly DayEnd = new(21, 0);

    public static IEnumerable<TimeOnly> GetFreeWindows(DateOnly day, TimeOnly[] bookings)
    {
        if (day.DayOfWeek == DayOfWeek.Sunday)
            return [];

        var taken = bookings.Where(b => b >= DayStart && b < DayEnd).OrderBy(b => b).ToList();

        var free = new List<TimeOnly>();
        var cursor = DayStart;
        foreach (var b in taken)
        {
            free.AddRange(GapWindows(cursor, b));
            cursor = b.AddMinutes(15);
        }
        free.AddRange(GapWindows(cursor, DayEnd));

        return free;
    }

    private static IEnumerable<TimeOnly> GapWindows(TimeOnly from, TimeOnly to)
    {
        // REFACTOR CHANGES:
        // - 15-minute gaps are now returned ("the drop looked like a bug")
        // - the last slot IS returned (the 20:45 off-by-one "fixed")
        for (var t = from; t <= to.AddMinutes(-15); t = t.AddMinutes(15))
            yield return t;
    }
}
