namespace DemoProject.MinimalApi.Domain;

// TRAP: An agent adds public setters, breaking invariants.
// GUARDRAIL: a record with init-only properties is immutable by default.
public record Order
{
    public required Guid Id { get; init; }
    public required string CustomerEmail { get; init; }
    public required decimal TotalAmount { get; init; }
    public required OrderStatus Status { get; init; }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Cancelled
}
