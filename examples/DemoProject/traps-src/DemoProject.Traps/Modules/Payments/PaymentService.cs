using DemoProject.Traps.Modules.Shipping;

namespace DemoProject.Traps.Modules.Payments;

// TRAP: The cycle continues — Payments depends on Shipping.
public class PaymentService
{
    public void Pay(IShippingProvider shipping) { }
}
