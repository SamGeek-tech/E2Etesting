namespace InventoryService.Application.DTOs;

/// <summary>
/// Data transfer object for Product.
/// </summary>
public record ProductDto(
    int Id,
    string Name,
    decimal Price,
    int StockQuantity
);

/// <summary>
/// Request to reserve stock for a product.
/// </summary>
public record ReserveStockRequest(
    int ProductId,
    int Quantity
);

/// <summary>
/// Response after stock reservation.
/// </summary>
public record ReserveStockResponse(
    bool Success,
    string? Message = null
);

/// <summary>
/// Request to create a new product.
/// </summary>
public record CreateProductRequest(
    string Name,
    decimal Price,
    int StockQuantity
);

/// <summary>
/// Request to update product stock.
/// </summary>
public record UpdateStockRequest(
    int ProductId,
    int Quantity
);

/// <summary>
/// Request to update a product's details.
/// </summary>
public record UpdateProductRequest(
    int Id,
    string? Name = null,
    decimal? Price = null,
    int? StockQuantity = null
);

