using OrderService.Domain.Interfaces;

namespace OrderService.Infrastructure.ExternalServices;

/// <summary>
/// Mock implementation of IInventoryClient for contract testing.
/// Always returns success for stock operations.
/// </summary>
public class MockInventoryClient : IInventoryClient
{
    public Task<bool> ReserveStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        // Always succeed during contract testing
        return Task.FromResult(true);
    }

    public Task ReleaseStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        // No-op during contract testing
        return Task.CompletedTask;
    }
}
