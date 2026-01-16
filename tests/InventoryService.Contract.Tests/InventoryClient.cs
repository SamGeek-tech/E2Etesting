using System.Net.Http.Json;
using System.Text.Json;

namespace InventoryService.Contract.Tests.Sdk;

public class InventoryClient
{
    private readonly HttpClient _httpClient;

    public InventoryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<ProductDto>?> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ProductDto>>("/api/inventory", cancellationToken);
    }

    public async Task<ProductDto?> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        try 
        {
            return await _httpClient.GetFromJsonAsync<ProductDto>($"/api/inventory/{id}", cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task ReserveStockAsync(ReserveStockRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/inventory/reserve", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/inventory", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: cancellationToken);
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/inventory/{id}", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: cancellationToken);
    }
    
    public async Task<string?> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
         var response = await _httpClient.GetAsync("/api/inventory/health", cancellationToken);
         response.EnsureSuccessStatusCode();
         var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
         return result.GetProperty("status").GetString();
    }
    public async Task<bool> DeleteInventoryItemAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/api/inventory/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return true;
    }
}

// DTOs
public record ProductDto(
    int Id,
    string Name,
    decimal Price,
    int StockQuantity
);

public record ReserveStockRequest(
    int ProductId,
    int Quantity
);

public record CreateProductRequest(
    string Name,
    decimal Price,
    int StockQuantity
);

public record UpdateProductRequest(
    int Id,
    string? Name = null,
    decimal? Price = null,
    int? StockQuantity = null
);
