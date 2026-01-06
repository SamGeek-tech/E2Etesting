using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

/// <summary>
/// Repository interface for Order aggregate.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetByUserEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}

