using System.Net;
using System.Text;
using System.Text.Json;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Persistence;

namespace InventoryService.Api.Middleware;

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
            { "Products exist", SeedProducts },
            { "Product 1 exists", SeedSingleProduct },
            { "Product 1 has sufficient stock", SeedProductWithStock },
            { "Inventory item 1 exists", SeedSingleProduct }
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
                
                if (providerState != null && !string.IsNullOrEmpty(providerState.State))
                {
                    if (_providerStates.ContainsKey(providerState.State))
                    {
                        Console.WriteLine($"[ProviderState] Setting up: {providerState.State}");
                        _providerStates[providerState.State].Invoke();
                    }
                    else
                    {
                        Console.WriteLine($"[ProviderState] Unknown state (ignoring): {providerState.State}");
                    }
                }
                else
                {
                    // No state to set up - this is fine for interactions without .Given()
                    Console.WriteLine("[ProviderState] No state specified, ensuring database exists");
                    EnsureDatabaseExists();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProviderState] Error: {ex.Message}");
                Console.WriteLine($"[ProviderState] Stack: {ex.StackTrace}");
                // Don't fail the request - just log and continue
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(string.Empty);
        }
        else
        {
            await _next(context);
        }
    }

    private void EnsureDatabaseExists()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        context.Database.EnsureCreated();
    }

    private void SeedProducts()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        // For contract testing, ensure clean database with predictable IDs
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // Note: EnsureCreated() already seeds products via HasData()
        // The seed data includes products with IDs 1, 2, 3
        Console.WriteLine("[ProviderState] Database recreated with seed data");
    }

    private void SeedSingleProduct()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        // For contract testing, ensure clean database with predictable IDs
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // Note: EnsureCreated() already seeds a product with ID=1 via HasData()
        Console.WriteLine("[ProviderState] Database recreated with seed data (Product 1 exists)");
    }

    private void SeedProductWithStock()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        // For contract testing, ensure clean database with predictable IDs
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // Note: EnsureCreated() already seeds products with stock via HasData()
        // Product ID=1 (Laptop) has StockQuantity=400, which is sufficient
        Console.WriteLine("[ProviderState] Database recreated with seed data (Product 1 has stock)");
    }
}

public class ProviderState
{
    public string State { get; set; } = string.Empty;
    public Dictionary<string, string>? Params { get; set; }
}
