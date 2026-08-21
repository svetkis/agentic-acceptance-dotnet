using DemoProject.Traps.Modules.Orders;

namespace DemoProject.Traps.Modules.Shipping;

// TRAP: The cycle closes — Shipping depends on Orders.
// GUARDRAIL: ArchUnitNET BeFreeOfCycles catches this cycle.
// NetArchTest.NotHaveDependenciesBetweenSlices would also have caught it,
// but it would forbid ALL cross-module dependencies, even legal DAGs.
public class ShippingService
{
    public void Ship(IOrderRepository order) { }
}
