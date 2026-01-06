using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

/// <summary>
/// Application service implementing order use cases.
/// </summary>
public class OrderAppService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryClient _inventoryClient;

    public OrderAppService(IOrderRepository orderRepository, IInventoryClient inventoryClient)
    {
        _orderRepository = orderRepository;
        _inventoryClient = inventoryClient;
    }

    public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByUserEmailAsync(userEmail, cancellationToken);
        return orders.Select(MapToDto);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        return order is null ? null : MapToDto(order);
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(string userEmail, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Reserve stock for all items
        var reservedItems = new List<CreateOrderItemRequest>();
        
        try
        {
            foreach (var item in request.Items)
            {
                var reserved = await _inventoryClient.ReserveStockAsync(item.ProductId, item.Quantity, cancellationToken);
                if (!reserved)
                {
                    // Rollback previously reserved items
                    await RollbackReservationsAsync(reservedItems, cancellationToken);
                    return new CreateOrderResponse(false, ErrorMessage: $"Failed to reserve stock for product {item.ProductId}");
                }
                reservedItems.Add(item);
            }

            // 2. Create the order
            var order = new Order(userEmail);
            foreach (var item in request.Items)
            {
                order.AddItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);
            }
            order.Confirm();

            var created = await _orderRepository.AddAsync(order, cancellationToken);
            return new CreateOrderResponse(true, MapToDto(created));
        }
        catch (Exception ex)
        {
            // Rollback on failure
            await RollbackReservationsAsync(reservedItems, cancellationToken);
            return new CreateOrderResponse(false, ErrorMessage: ex.Message);
        }
    }

    private async Task RollbackReservationsAsync(List<CreateOrderItemRequest> items, CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            try
            {
                await _inventoryClient.ReleaseStockAsync(item.ProductId, item.Quantity, cancellationToken);
            }
            catch
            {
                // Log but don't throw - best effort rollback
            }
        }
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.UserEmail,
            order.OrderDate,
            order.TotalAmount,
            order.Status.ToString(),
            order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice
            )).ToList()
        );
    }
}

