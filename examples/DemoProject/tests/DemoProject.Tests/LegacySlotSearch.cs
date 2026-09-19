// Fixture for AvailableSlotsCharacterizationTests: emulates spec-less legacy code.
// Its quirks (including a bug or two) are exactly what the golden master records.
using DemoProject.Domain;

namespace DemoProject.Tests;

/// <summary>
/// LEGACY — nobody knows why it works this way; the spec is lost.
/// Work day 9:00–21:00, 15-minute slots. Quirks discovered by reading the code:
/// - Sunday returns NO windows (working days Mon–Sat);
/// - gaps shorter than 30 minutes between bookings are not returned (dead zones);
/// - the 20:45 slot is NEVER returned even when free (an off-by-one, kept as-is).
/// </summary>
public static class LegacySlotSearch
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
        // Quirk: gaps < 30 minutes are silently dropped.
        if (to - from < new TimeSpan(0, 30, 0))
            yield break;

        // Quirk: the last start is to - 30min, so the 20:45 slot never appears.
        for (var t = from; t <= to.AddMinutes(-30); t = t.AddMinutes(15))
            yield return t;
    }
}
