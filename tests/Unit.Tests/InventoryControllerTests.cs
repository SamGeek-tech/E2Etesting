using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryService.Api.Controllers;
using InventoryService.Api.Data;
using InventoryService.Api.Models;

namespace Unit.Tests;

[TestFixture]
public class InventoryControllerTests
{
    private InventoryDbContext _context = null!;
    private InventoryController _controller = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new InventoryDbContext(options);
        
        // Seed test data
        _context.Products.AddRange(
            new Product { Id = 1, Name = "Laptop", Price = 999.99m, StockQuantity = 10 },
            new Product { Id = 2, Name = "Mouse", Price = 25.50m, StockQuantity = 50 },
            new Product { Id = 3, Name = "Keyboard", Price = 75.00m, StockQuantity = 0 }
        );
        _context.SaveChanges();

        _controller = new InventoryController(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetProducts Tests

    [Test]
    public async Task GetProducts_ReturnsAllProducts()
    {
        // Act
        var result = await _controller.GetProducts();

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Count(), Is.EqualTo(3));
    }

    [Test]
    public async Task GetProducts_ReturnsCorrectProductNames()
    {
        // Act
        var result = await _controller.GetProducts();

        // Assert
        var products = result.Value!.ToList();
        Assert.That(products.Select(p => p.Name), Contains.Item("Laptop"));
        Assert.That(products.Select(p => p.Name), Contains.Item("Mouse"));
        Assert.That(products.Select(p => p.Name), Contains.Item("Keyboard"));
    }

    #endregion

    #region GetProduct Tests

    [Test]
    public async Task GetProduct_WithValidId_ReturnsProduct()
    {
        // Act
        var result = await _controller.GetProduct(1);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Name, Is.EqualTo("Laptop"));
        Assert.That(result.Value.Price, Is.EqualTo(999.99m));
    }

    [Test]
    public async Task GetProduct_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetProduct(999);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    #endregion

    #region ReserveStock Tests

    [Test]
    public async Task ReserveStock_WithSufficientStock_ReturnsOkAndDeductsStock()
    {
        // Arrange
        var request = new ReserveRequest(ProductId: 1, Quantity: 5);
        var initialStock = (await _controller.GetProduct(1)).Value!.StockQuantity;

        // Act
        var result = await _controller.ReserveStock(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        
        var updatedProduct = await _context.Products.FindAsync(1);
        Assert.That(updatedProduct!.StockQuantity, Is.EqualTo(initialStock - 5));
    }

    [Test]
    public async Task ReserveStock_WithInsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var request = new ReserveRequest(ProductId: 1, Quantity: 100); // Only 10 in stock

        // Act
        var result = await _controller.ReserveStock(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = result as BadRequestObjectResult;
        Assert.That(badRequest!.Value, Is.EqualTo("Insufficient stock"));
    }

    [Test]
    public async Task ReserveStock_WithZeroStock_ReturnsBadRequest()
    {
        // Arrange - Product 3 (Keyboard) has 0 stock
        var request = new ReserveRequest(ProductId: 3, Quantity: 1);

        // Act
        var result = await _controller.ReserveStock(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ReserveStock_WithNonExistentProduct_ReturnsNotFound()
    {
        // Arrange
        var request = new ReserveRequest(ProductId: 999, Quantity: 1);

        // Act
        var result = await _controller.ReserveStock(request);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task ReserveStock_ReservingExactStock_ReturnsOkAndSetsStockToZero()
    {
        // Arrange
        var request = new ReserveRequest(ProductId: 1, Quantity: 10); // Exactly 10 in stock

        // Act
        var result = await _controller.ReserveStock(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        
        var updatedProduct = await _context.Products.FindAsync(1);
        Assert.That(updatedProduct!.StockQuantity, Is.EqualTo(0));
    }

    #endregion

    #region Health Check Tests

    [Test]
    public void Health_ReturnsOkWithHealthyStatus()
    {
        // Act
        var result = _controller.Health();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    #endregion
}

