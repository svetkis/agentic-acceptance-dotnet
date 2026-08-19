using System.Text.Json.Serialization;

namespace DemoProject.Domain;

// TRAP: An agent mixes up identifiers of different entities because they are all Guid.
// GUARDRAIL: CustomerId and BookingId are distinct types. The compiler will not allow substituting one for the other.
[JsonConverter(typeof(CustomerIdJsonConverter))]
public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
