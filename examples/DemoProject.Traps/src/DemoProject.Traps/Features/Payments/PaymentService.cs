using DemoProject.Traps.Features.Orders;

namespace DemoProject.Traps.Features.Payments;

// TRAP: An agent added a using from a neighboring feature "for the sake of one DTO".
// GUARDRAIL: Slice().NotHaveDependenciesBetweenSlices() catches a cross-module dependency.
public class PaymentService
{
    public void ProcessPayment(OrderDto order)
    {
        ArgumentNullException.ThrowIfNull(order);
    }
}
