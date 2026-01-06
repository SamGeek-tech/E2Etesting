using Microsoft.EntityFrameworkCore;
using InventoryService.Api.Models;

namespace InventoryService.Api.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop", Price = 999.99m, StockQuantity = 400 },
            new Product { Id = 2, Name = "Mouse", Price = 25.50m, StockQuantity = 500 },
            new Product { Id = 3, Name = "Keyboard", Price = 75.00m, StockQuantity = 300 }
        );
    }
}
