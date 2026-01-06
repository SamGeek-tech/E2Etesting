using Microsoft.Extensions.Configuration;
using OrderService.Domain.Interfaces;
using System.Net.Http.Json;

namespace OrderService.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client implementation for communicating with Inventory service.
/// </summary>
public class InventoryClient : IInventoryClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public InventoryClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["Services:InventoryUrl"] ?? "http://localhost:5001";
    }

    public async Task<bool> ReserveStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/inventory/reserve",
                new { ProductId = productId, Quantity = quantity },
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task ReleaseStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would have a release endpoint
        // For now, this is a no-op as the original API doesn't have this
        await Task.CompletedTask;
    }
}

