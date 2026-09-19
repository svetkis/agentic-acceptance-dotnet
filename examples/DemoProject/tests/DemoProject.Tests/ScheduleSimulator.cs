// Fixture for SmartSelectionCharacterizationTests: a miniature schedule simulator.
// Two assignment strategies, same clients (paired runs), dead-zone metric.
namespace DemoProject.Tests;

public static class ScheduleSimulator
{
    public readonly record struct Result(double DeadZoneShare);

    /// A client that needs a window: earliest acceptable time and preferred length.
    public readonly record struct Client(int EarliestSlot, int LatestSlot, int SlotsNeeded);

    /// Assigns clients to windows. Returns booked slot indexes.
    public delegate List<int> Strategy(IReadOnlyList<Client> clients, int slotsPerDay);

    /// Paired run: identical clients for both strategies (same seed).
    public static (Result Naive, Result Smart) RunPaired(int seed, int days)
    {
        var rng = new Random(seed);
        double naiveShare = 0, smartShare = 0;
        const int slotsPerDay = 48; // 15-minute slots over a 12-hour day

        for (var d = 0; d < days; d++)
        {
            var clients = Enumerable.Range(0, rng.Next(4, 9))
                .Select(_ => new Client(
                    EarliestSlot: rng.Next(0, slotsPerDay - 6),
                    LatestSlot: 0,
                    SlotsNeeded: rng.Next(1, 4)))
                .Select(c => c with { LatestSlot = Math.Min(slotsPerDay, c.EarliestSlot + rng.Next(4, 20)) })
                .ToList();

            naiveShare += DeadZoneShare(Naive(clients, slotsPerDay), slotsPerDay);
            smartShare += DeadZoneShare(Smart(clients, slotsPerDay), slotsPerDay);
        }

        return (new Result(naiveShare / days), new Result(smartShare / days));
    }

    // First-fit from the start of the day: leaves fragmented useless gaps.
    private static List<int> Naive(IReadOnlyList<Client> clients, int slotsPerDay)
    {
        var booked = new List<int>();
        foreach (var c in clients)
        {
            for (var s = c.EarliestSlot; s + c.SlotsNeeded <= c.LatestSlot; s++)
            {
                if (Enumerable.Range(s, c.SlotsNeeded).All(x => !booked.Contains(x)))
                {
                    booked.AddRange(Enumerable.Range(s, c.SlotsNeeded));
                    break;
                }
            }
        }
        return booked;
    }

    // Best-fit: prefer the tightest packing around already booked slots.
    private static List<int> Smart(IReadOnlyList<Client> clients, int slotsPerDay)
    {
        var booked = new List<int>();
        foreach (var c in clients)
        {
            var best = -1;
            var bestWaste = int.MaxValue;
            for (var s = c.EarliestSlot; s + c.SlotsNeeded <= c.LatestSlot; s++)
            {
                var range = Enumerable.Range(s, c.SlotsNeeded);
                if (range.Any(x => booked.Contains(x)))
                    continue;
                // waste = distance to the nearest existing booking (prefer adjacency)
                var waste = booked.Count == 0
                    ? Math.Abs(s - slotsPerDay / 2)
                    : booked.Min(b => Math.Abs(b - s) + Math.Abs(b - (s + c.SlotsNeeded - 1)));
                if (waste < bestWaste)
                {
                    bestWaste = waste;
                    best = s;
                }
            }
            if (best >= 0)
                booked.AddRange(Enumerable.Range(best, c.SlotsNeeded));
        }
        return booked;
    }

    // Dead zone: unbooked fragments shorter than 30 minutes (2 slots).
    private static double DeadZoneShare(List<int> booked, int slotsPerDay)
    {
        var sorted = booked.Order().ToList();
        var dead = 0; var cursor = 0;
        foreach (var b in sorted)
        {
            if (b - cursor is > 0 and < 2)
                dead += b - cursor;
            cursor = Math.Max(cursor, b + 1);
        }
        if (slotsPerDay - cursor is > 0 and < 2)
            dead += slotsPerDay - cursor;
        return (double)dead / slotsPerDay;
    }
}
