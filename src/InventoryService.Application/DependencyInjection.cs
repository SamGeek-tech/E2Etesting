using InventoryService.Application.Interfaces;
using InventoryService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryService.Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IInventoryService, InventoryAppService>();
        return services;
    }
}

