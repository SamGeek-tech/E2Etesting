using System.Net;
using System.Text;
using System.Text.Json;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Api.Middleware;

public class ProviderStateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Action> _providerStates;

    public ProviderStateMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
        _providerStates = new Dictionary<string, Action>
        {
            { "Orders exist for user", SeedOrders },
            { "Order 1 exists for user", SeedSingleOrder },
            { "Inventory is available for products", PrepareForCreateOrder }
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Value == "/provider-states")
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            try 
            {
                var providerState = JsonSerializer.Deserialize<ProviderState>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (providerState != null && !string.IsNullOrEmpty(providerState.State) && _providerStates.ContainsKey(providerState.State))
                {
                    _providerStates[providerState.State].Invoke();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to handle provider state: {ex.Message}");
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(string.Empty);
        }
        else
        {
            await _next(context);
        }
    }

    private void SeedOrders()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        // For contract testing, ensure clean database with predictable IDs
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // Add sample orders - will get predictable IDs in fresh database
        var order1 = new Order("user@example.com");
        order1.AddItem(1, "Product", 1, 100.0m);
        
        var order2 = new Order("user@example.com");
        order2.AddItem(2, "Another Product", 2, 50.0m);
        
        context.Orders.Add(order1);
        context.Orders.Add(order2);
        context.SaveChanges();
    }

    private void SeedSingleOrder()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        // For contract testing, ensure clean database with predictable IDs
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // Add single order - will get ID=1 in fresh database
        var order = new Order("user@example.com");
        order.AddItem(1, "Product", 1, 100.0m);
        
        context.Orders.Add(order);
        context.SaveChanges();
    }

    private void PrepareForCreateOrder()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        // Ensure clean database for order creation
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        
        // Note: The mock InventoryClient (registered in Program.cs for contract testing)
        // will automatically return success for ReserveStockAsync
    }
}

public class ProviderState
{
    public string State { get; set; } = string.Empty;
    public Dictionary<string, string>? Params { get; set; }
}
