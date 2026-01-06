using OrderService.Application.DTOs;

namespace OrderService.Application.Interfaces;

/// <summary>
/// Application service interface for order operations.
/// </summary>
public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CreateOrderResponse> CreateOrderAsync(string userEmail, CreateOrderRequest request, CancellationToken cancellationToken = default);
}

