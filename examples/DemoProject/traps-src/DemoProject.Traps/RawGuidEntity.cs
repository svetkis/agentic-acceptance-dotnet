namespace DemoProject.Traps.Domain;

// TRAP: An agent used Guid instead of a strongly typed ID.
// GUARDRAIL: Regex + architecture tests catch raw Guids in property names.
public class RawGuidEntity
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
}
