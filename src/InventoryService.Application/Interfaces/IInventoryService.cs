using InventoryService.Application.DTOs;

namespace InventoryService.Application.Interfaces;

/// <summary>
/// Application service interface for inventory operations.
/// </summary>
public interface IInventoryService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ReserveStockResponse> ReserveStockAsync(ReserveStockRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task ReleaseStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateProductAsync(UpdateProductRequest request, CancellationToken cancellationToken = default);
}

