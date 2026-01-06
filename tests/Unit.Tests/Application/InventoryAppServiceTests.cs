using InventoryService.Application.DTOs;
using InventoryService.Application.Services;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;
using Moq;
using Xunit;

namespace Unit.Tests.Application;

/// <summary>
/// Unit tests for InventoryAppService.
/// Tests use case implementations with mocked repository.
/// </summary>
public class InventoryAppServiceTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly InventoryAppService _service;

    public InventoryAppServiceTests()
    {
        // Setup - Initialize mocks for all tests
        _repositoryMock = new Mock<IProductRepository>();
        _service = new InventoryAppService(_repositoryMock.Object);
    }

    #region GetAllProductsAsync Tests

    [Fact]
    public async Task GetAllProductsAsync_WithProducts_ReturnsAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product(1, "Product A", 10.00m, 100),
            new Product(2, "Product B", 20.00m, 200)
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        // Act
        var result = await _service.GetAllProductsAsync();

        // Assert
        var productDtos = result.ToList();
        Assert.Equal(2, productDtos.Count);
        Assert.Equal("Product A", productDtos[0].Name);
        Assert.Equal("Product B", productDtos[1].Name);
    }

    [Fact]
    public async Task GetAllProductsAsync_WithNoProducts_ReturnsEmptyList()
    {
        // Arrange - Empty collection edge case
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        // Act
        var result = await _service.GetAllProductsAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetProductByIdAsync Tests

    [Fact]
    public async Task GetProductByIdAsync_WithExistingProduct_ReturnsProductDto()
    {
        // Arrange
        var product = new Product(1, "Test Product", 25.00m, 50);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.GetProductByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal(25.00m, result.Price);
        Assert.Equal(50, result.StockQuantity);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithNonExistingProduct_ReturnsNull()
    {
        // Arrange - Product not found edge case
        _repositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetProductByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ReserveStockAsync Tests

    [Fact]
    public async Task ReserveStockAsync_WithSufficientStock_ReturnsSuccess()
    {
        // Arrange
        var product = new Product(1, "Test Product", 10.00m, 100);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new ReserveStockRequest(1, 30);

        // Act
        var result = await _service.ReserveStockAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReserveStockAsync_WithInsufficientStock_ReturnsFailure()
    {
        // Arrange - Tests error handling for insufficient stock
        var product = new Product(1, "Test Product", 10.00m, 20);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new ReserveStockRequest(1, 100);

        // Act
        var result = await _service.ReserveStockAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Insufficient stock", result.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReserveStockAsync_WithNonExistingProduct_ReturnsFailure()
    {
        // Arrange - Tests error handling for product not found
        _repositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var request = new ReserveStockRequest(999, 10);

        // Act
        var result = await _service.ReserveStockAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Theory]
    [InlineData(100, 100)]  // Reserve all stock
    [InlineData(100, 1)]    // Reserve minimum
    [InlineData(100, 50)]   // Reserve half
    public async Task ReserveStockAsync_WithVariousQuantities_ReservesCorrectly(int initialStock, int reserveQuantity)
    {
        // Parameterized test for various reservation scenarios
        var product = new Product(1, "Test", 10.00m, initialStock);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new ReserveStockRequest(1, reserveQuantity);

        var result = await _service.ReserveStockAsync(request);

        Assert.True(result.Success);
        Assert.Equal(initialStock - reserveQuantity, product.StockQuantity);
    }

    #endregion

    #region CreateProductAsync Tests

    [Fact]
    public async Task CreateProductAsync_WithValidRequest_CreatesAndReturnsProduct()
    {
        // Arrange
        var request = new CreateProductRequest("New Product", 99.99m, 100);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        // Act
        var result = await _service.CreateProductAsync(request);

        // Assert
        Assert.Equal("New Product", result.Name);
        Assert.Equal(99.99m, result.Price);
        Assert.Equal(100, result.StockQuantity);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ReleaseStockAsync Tests

    [Fact]
    public async Task ReleaseStockAsync_WithExistingProduct_ReleasesStock()
    {
        // Arrange
        var product = new Product(1, "Test Product", 10.00m, 50);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        await _service.ReleaseStockAsync(1, 25);

        // Assert
        Assert.Equal(75, product.StockQuantity);
        _repositoryMock.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReleaseStockAsync_WithNonExistingProduct_ThrowsArgumentException()
    {
        // Arrange - Tests error handling for product not found
        _repositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ReleaseStockAsync(999, 10));
    }

    #endregion

    #region UpdateProductAsync Tests

    [Fact]
    public async Task UpdateProductAsync_WithExistingProduct_UpdatesAllFields()
    {
        // Arrange
        var product = new Product(1, "Old Name", 10.00m, 50);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new UpdateProductRequest(1, "New Name", 25.00m, 100);

        // Act
        var result = await _service.UpdateProductAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal(25.00m, result.Price);
        Assert.Equal(100, result.StockQuantity);
        _repositoryMock.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_WithNonExistingProduct_ReturnsNull()
    {
        // Arrange - Tests error handling for product not found
        _repositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var request = new UpdateProductRequest(999, "New Name");

        // Act
        var result = await _service.UpdateProductAsync(request);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_WithOnlyNameChange_UpdatesOnlyName()
    {
        // Arrange - Tests partial update scenario
        var product = new Product(1, "Old Name", 10.00m, 50);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new UpdateProductRequest(1, Name: "New Name");

        // Act
        var result = await _service.UpdateProductAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal(10.00m, result.Price);   // Unchanged
        Assert.Equal(50, result.StockQuantity);  // Unchanged
    }

    [Fact]
    public async Task UpdateProductAsync_WithOnlyPriceChange_UpdatesOnlyPrice()
    {
        // Arrange - Tests partial update scenario
        var product = new Product(1, "Test Product", 10.00m, 50);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new UpdateProductRequest(1, Price: 99.99m);

        // Act
        var result = await _service.UpdateProductAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Product", result.Name);  // Unchanged
        Assert.Equal(99.99m, result.Price);
        Assert.Equal(50, result.StockQuantity);  // Unchanged
    }

    [Fact]
    public async Task UpdateProductAsync_WithOnlyStockChange_UpdatesOnlyStock()
    {
        // Arrange - Tests partial update scenario
        var product = new Product(1, "Test Product", 10.00m, 50);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new UpdateProductRequest(1, StockQuantity: 200);

        // Act
        var result = await _service.UpdateProductAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Product", result.Name);  // Unchanged
        Assert.Equal(10.00m, result.Price);  // Unchanged
        Assert.Equal(200, result.StockQuantity);
    }

    [Fact]
    public async Task UpdateProductAsync_WithNoChanges_StillUpdatesRepository()
    {
        // Arrange - Tests idempotent behavior
        var product = new Product(1, "Test Product", 10.00m, 50);
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new UpdateProductRequest(1);  // No fields to update

        // Act
        var result = await _service.UpdateProductAsync(request);

        // Assert
        Assert.NotNull(result);
        _repositoryMock.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}

