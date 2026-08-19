namespace DemoProject.MinimalApi.Domain;

// TRAP: An agent adds public setters, breaking invariants.
// GUARDRAIL: a record with init-only properties is immutable by default.
public record Payment
{
    public required Guid Id { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required PaymentStatus Status { get; init; }
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}
