using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrderService.Contract.Tests.Sdk;

public class OrderClient
{
    private readonly HttpClient _httpClient;
    private string? _authToken;

    public OrderClient(HttpClient httpClient, string? authToken = null)
    {
        _httpClient = httpClient;
        _authToken = authToken;
        
        if (!string.IsNullOrEmpty(_authToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
        }
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<IEnumerable<OrderDto>?> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<OrderDto>>("/api/orders", cancellationToken);
    }

    public async Task<OrderDto?> GetOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        try 
        {
            return await _httpClient.GetFromJsonAsync<OrderDto>($"/api/orders/{id}", cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<OrderDto?> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/orders", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: cancellationToken);
    }
    
    public async Task<string?> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
         var response = await _httpClient.GetAsync("/api/orders/health", cancellationToken);
         response.EnsureSuccessStatusCode();
         var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
         return result.GetProperty("status").GetString();
    }
}

// DTOs
public record OrderDto(
    int Id,
    string UserEmail,
    DateTime OrderDate,
    decimal TotalAmount,
    string Status,
    List<OrderItemDto> Items
);

public record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

public record CreateOrderRequest(
    List<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);
