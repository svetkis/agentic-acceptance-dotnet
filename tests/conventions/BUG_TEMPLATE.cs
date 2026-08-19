// TEMPLATE: Regression test for a bug fix.
// Copy this file and rename it following the pattern: BUG###_ShortDescription.cs

using TUnit;

namespace Tests.Conventions;

// TRAP: The bug was fixed, but a week later the agent brought it back with the same hands.
// GUARDRAIL: A bug fix must be accompanied by a test that reproduces the problem.
public class BUG_TEMPLATE // Rename: BUG055_ShortDescription
{
    [Test]
    public async Task Scenario_ShouldNotReproduceTheBug()
    {
         // Arrange: create the state in which the bug reproduced
        // var context = await SetupBrokenState();

         // Act: perform the action that used to break
        // var result = await context.Execute();

         // Assert: verify the bug no longer reproduces
        // await Assert.That(result.Status).IsEqualTo(ExpectedStatus);
    }

    // If the bug was a race condition or requires a specific setup —
    // add [Before(Test)] and [After(Test)] for isolation.
}
