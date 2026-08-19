using DemoProject.MinimalApi.Domain;

namespace DemoProject.MinimalApi.Features.Orders;

// TRAP: An agent might use DateTime.Now instead of UtcNow.
// GUARDRAIL: An architecture test catches DateTime.Now via regex.
public class OrderService
{
    private static readonly List<Order> _orders = new();

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<Order>>(_orders.ToList());
    }

    public Task<Order> CreateAsync(string customerEmail, decimal amount, CancellationToken ct = default)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerEmail = customerEmail,
            TotalAmount = amount,
            Status = OrderStatus.Pending
        };

        _orders.Add(order);
        return Task.FromResult(order);
    }

    public Task<bool> ConfirmAsync(Guid id, CancellationToken ct = default)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);
        if (order is null)
            return Task.FromResult(false);

        _orders.Remove(order);
        _orders.Add(order with { Status = OrderStatus.Confirmed });
        return Task.FromResult(true);
    }
}
