using DemoProject.Domain;

namespace DemoProject.Infrastructure;

public sealed class InfrastructureBookingService : IBookingService
{
    // TRAP: An agent might forget CancellationToken or use FindAsync in a read path.
    // GUARDRAIL: Code review agent + ArchitectureRules.FindAsync_ShouldNotBeUsedInReadPath

    public Task<Booking?> GetByIdAsync(BookingId id, CancellationToken ct = default)
    {
        return Task.FromResult<Booking?>(null);
    }

    public Task<IReadOnlyList<Booking>> GetPendingAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<Booking>>(Array.Empty<Booking>());
    }

    public Task ConfirmAsync(BookingId id, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
