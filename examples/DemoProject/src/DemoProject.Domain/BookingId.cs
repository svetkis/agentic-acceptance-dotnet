using System.Text.Json.Serialization;

namespace DemoProject.Domain;

// TRAP: An agent passes a customer Guid to a method expecting a booking Guid — the compiler stays silent.
// GUARDRAIL: BookingId is not a Guid. GetByIdAsync(BookingId) will not accept a CustomerId.
// The error is caught within seconds of typing, without running tests.
[JsonConverter(typeof(BookingIdJsonConverter))]
public readonly record struct BookingId(Guid Value)
{
    public static BookingId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
