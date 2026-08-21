using System.Net.Http;

namespace DemoProject.Traps.Domain;

// TRAP: An agent added using System.Net.Http in Domain for "a single call".
// GUARDRAIL: HaveDependencyOnAny catches an IL dependency on a forbidden namespace.
public class DomainLeakingToInfra
{
    public void DoSomething()
    {
        _ = new HttpClient();
    }
}
