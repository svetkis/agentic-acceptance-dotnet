namespace DemoProject.Traps.Domain;

// TRAP: An agent added mutable state to Domain via a public field.
// GUARDRAIL: BeImmutableExternally catches public fields.
public class MutableState
{
    public int Counter;
}
