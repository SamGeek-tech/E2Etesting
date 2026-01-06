namespace OrderService.Domain.Interfaces;

/// <summary>
/// Anti-corruption layer interface for communicating with Inventory service.
/// </summary>
public interface IInventoryClient
{
    Task<bool> ReserveStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    Task ReleaseStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
}

