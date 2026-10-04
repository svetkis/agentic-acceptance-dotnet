namespace DemoProject.Traps;

using System.Threading;

// TRAP: An agent "fixes" a race by taking a lock inside a [HotPath] method.
//       Single-threaded tests stay green; under parallel load the workers serialize
//       on the lock and throughput collapses.
// GUARDRAIL: LockContentionBudgetTests catch the contention under parallel load.
// NOTE: This method intentionally holds a monitor lock to demonstrate the guardrail failing.
// NOTE: The hold parks the holder (Thread.Sleep) on purpose — a busy-wait hold often
//       does NOT register as contention on modern .NET, because waiters can take the
//       lock through adaptive spin / direct handoff without ever parking.
public sealed class LockContentionHotspot
{
    private readonly object _gate = new();

    [HotPath]
    public int Process(int value)
    {
        lock (_gate)
        {
            // Hold long enough that all workers queue up on the monitor.
            // A real trap is usually shorter but hit far more often; the length
            // only makes the demo deterministic across CI agents.
            Thread.Sleep(40);
            return value;
        }
    }
}
