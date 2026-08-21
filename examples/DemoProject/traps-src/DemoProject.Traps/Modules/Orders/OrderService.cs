using DemoProject.Traps.Modules.Payments;

namespace DemoProject.Traps.Modules.Orders;

// TRAP: An agent added a dependency on Payments "for a single call".
// This is the start of a cycle: Orders -> Payments -> Shipping -> Orders.
// NetArchTest.NotHaveDependenciesBetweenSlices would have caught this too,
// but it would forbid ALL cross-module dependencies — even legal DAGs.
public class OrderService
{
    public void CreateOrder(IPaymentGateway payment) { }
}
