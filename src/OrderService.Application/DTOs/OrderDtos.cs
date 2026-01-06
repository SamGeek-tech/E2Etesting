using OrderService.Domain.Entities;

namespace OrderService.Application.DTOs;

/// <summary>
/// Data transfer object for Order.
/// </summary>
public record OrderDto(
    int Id,
    string UserEmail,
    DateTime OrderDate,
    decimal TotalAmount,
    string Status,
    List<OrderItemDto> Items
);

/// <summary>
/// Data transfer object for OrderItem.
/// </summary>
public record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

/// <summary>
/// Request to create a new order.
/// </summary>
public record CreateOrderRequest(
    List<CreateOrderItemRequest> Items
);

/// <summary>
/// Request to add an item to order.
/// </summary>
public record CreateOrderItemRequest(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

/// <summary>
/// Response after creating an order.
/// </summary>
public record CreateOrderResponse(
    bool Success,
    OrderDto? Order = null,
    string? ErrorMessage = null
);

/// <summary>
/// Request for user login.
/// </summary>
public record LoginRequest(
    string Email,
    string Password
);

/// <summary>
/// Response after login.
/// </summary>
public record LoginResponse(
    bool Success,
    string? Token = null,
    string? ErrorMessage = null
);

