using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for Inventory bounded context.
/// </summary>
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            
            // Seed data
            entity.HasData(
                new { Id = 1, Name = "Laptop", Price = 999.99m, StockQuantity = 400 },
                new { Id = 2, Name = "Mouse", Price = 25.50m, StockQuantity = 500 },
                new { Id = 3, Name = "Keyboard", Price = 75.00m, StockQuantity = 300 }
            );
        });
    }
}

