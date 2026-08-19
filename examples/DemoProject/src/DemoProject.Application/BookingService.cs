using DemoProject.Domain;

namespace DemoProject.Application;

public sealed class BookingService : IBookingService
{
    // TRAP: An agent might add using DemoProject.Infrastructure and break the layers.
    // GUARDRAIL: ArchitectureRules.Api_ShouldNotReferenceInfrastructureDirectly

    public Task<Booking?> GetByIdAsync(BookingId id, CancellationToken ct = default)
    {
        // In real app: read from DB via Select() + AsNoTracking()
        var booking = new Booking
        {
            Id = id,
            CustomerId = CustomerId.New(),
            CustomerName = "Demo",
            ScheduledAt = DateTime.UtcNow,
            Status = BookingStatus.Pending
        };
        return Task.FromResult<Booking?>(booking);
    }

    public Task<IReadOnlyList<Booking>> GetPendingAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<Booking>>(Array.Empty<Booking>());
    }

    public Task ConfirmAsync(BookingId id, CancellationToken ct = default)
    {
        // In real app: load entity with tracking, modify, save
        return Task.CompletedTask;
    }

    // TRAP: An agent adds new/async/boxing to a method that is called frequently.
    // GUARDRAIL: [HotPath] + AllocationBudgetTests catch allocation regressions.
    [HotPath]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3400:Methods should not return constants",
        Justification = "Demo hot-path method for allocation budget testing.")]
    public int GetPendingCount()
    {
        return 0; // Demo implementation
    }
}
