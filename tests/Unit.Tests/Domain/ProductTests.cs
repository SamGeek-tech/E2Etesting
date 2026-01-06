using InventoryService.Domain.Entities;
using Xunit;

namespace Unit.Tests.Domain;

/// <summary>
/// Unit tests for the Product domain entity.
/// Tests domain logic and invariants.
/// </summary>
public class ProductTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesProduct()
    {
        // Arrange
        var id = 1;
        var name = "Test Product";
        var price = 99.99m;
        var stockQuantity = 100;

        // Act
        var product = new Product(id, name, price, stockQuantity);

        // Assert
        Assert.Equal(id, product.Id);
        Assert.Equal(name, product.Name);
        Assert.Equal(price, product.Price);
        Assert.Equal(stockQuantity, product.StockQuantity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Tests edge case where product name is null or empty
        Assert.Throws<ArgumentException>(() => new Product(1, invalidName!, 10.00m, 50));
    }

    [Fact]
    public void Constructor_WithNegativePrice_ThrowsArgumentException()
    {
        // Tests edge case where price is negative
        Assert.Throws<ArgumentException>(() => new Product(1, "Test", -1.00m, 50));
    }

    [Fact]
    public void Constructor_WithNegativeStockQuantity_ThrowsArgumentException()
    {
        // Tests edge case where stock quantity is negative
        Assert.Throws<ArgumentException>(() => new Product(1, "Test", 10.00m, -1));
    }

    [Fact]
    public void Constructor_WithZeroPrice_Succeeds()
    {
        // Tests boundary value - zero price is valid (free product)
        var product = new Product(1, "Free Item", 0m, 100);
        Assert.Equal(0m, product.Price);
    }

    [Fact]
    public void Constructor_WithZeroStockQuantity_Succeeds()
    {
        // Tests boundary value - zero stock is valid (out of stock)
        var product = new Product(1, "Out of Stock Item", 10.00m, 0);
        Assert.Equal(0, product.StockQuantity);
    }

    #endregion

    #region ReserveStock Tests

    [Fact]
    public void ReserveStock_WithValidQuantity_DecreasesStock()
    {
        // Arrange
        var product = new Product(1, "Test Product", 10.00m, 100);
        var reserveQuantity = 30;

        // Act
        product.ReserveStock(reserveQuantity);

        // Assert
        Assert.Equal(70, product.StockQuantity);
    }

    [Theory]
    [InlineData(100, 100, 0)]   // Reserve all stock
    [InlineData(100, 1, 99)]    // Reserve minimum
    [InlineData(100, 50, 50)]   // Reserve half
    public void ReserveStock_WithVariousQuantities_ReturnsCorrectStock(int initial, int reserve, int expected)
    {
        // Parameterized test for various reservation scenarios
        var product = new Product(1, "Test", 10.00m, initial);
        product.ReserveStock(reserve);
        Assert.Equal(expected, product.StockQuantity);
    }

    [Fact]
    public void ReserveStock_WithInsufficientStock_ThrowsInsufficientStockException()
    {
        // Tests error handling when trying to reserve more than available
        var product = new Product(1, "Test Product", 10.00m, 50);

        var exception = Assert.Throws<InsufficientStockException>(() => product.ReserveStock(100));

        Assert.Equal(1, exception.ProductId);
        Assert.Equal("Test Product", exception.ProductName);
        Assert.Equal(50, exception.AvailableStock);
        Assert.Equal(100, exception.RequestedQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ReserveStock_WithNonPositiveQuantity_ThrowsArgumentException(int invalidQuantity)
    {
        // Tests edge case where quantity is zero or negative
        var product = new Product(1, "Test", 10.00m, 100);
        Assert.Throws<ArgumentException>(() => product.ReserveStock(invalidQuantity));
    }

    #endregion

    #region ReleaseStock Tests

    [Fact]
    public void ReleaseStock_WithValidQuantity_IncreasesStock()
    {
        // Arrange
        var product = new Product(1, "Test Product", 10.00m, 50);

        // Act
        product.ReleaseStock(30);

        // Assert
        Assert.Equal(80, product.StockQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReleaseStock_WithNonPositiveQuantity_ThrowsArgumentException(int invalidQuantity)
    {
        // Tests edge case where quantity is zero or negative
        var product = new Product(1, "Test", 10.00m, 100);
        Assert.Throws<ArgumentException>(() => product.ReleaseStock(invalidQuantity));
    }

    #endregion

    #region UpdatePrice Tests

    [Fact]
    public void UpdatePrice_WithValidPrice_UpdatesPrice()
    {
        // Arrange
        var product = new Product(1, "Test Product", 10.00m, 100);

        // Act
        product.UpdatePrice(25.99m);

        // Assert
        Assert.Equal(25.99m, product.Price);
    }

    [Fact]
    public void UpdatePrice_WithZeroPrice_Succeeds()
    {
        // Tests boundary value - setting price to zero (making item free)
        var product = new Product(1, "Test", 10.00m, 100);
        product.UpdatePrice(0m);
        Assert.Equal(0m, product.Price);
    }

    [Fact]
    public void UpdatePrice_WithNegativePrice_ThrowsArgumentException()
    {
        // Tests error handling for negative price
        var product = new Product(1, "Test", 10.00m, 100);
        Assert.Throws<ArgumentException>(() => product.UpdatePrice(-5.00m));
    }

    #endregion

    #region AddStock Tests

    [Fact]
    public void AddStock_WithValidQuantity_IncreasesStock()
    {
        // Arrange
        var product = new Product(1, "Test Product", 10.00m, 100);

        // Act
        product.AddStock(50);

        // Assert
        Assert.Equal(150, product.StockQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddStock_WithNonPositiveQuantity_ThrowsArgumentException(int invalidQuantity)
    {
        // Tests edge case where quantity is zero or negative
        var product = new Product(1, "Test", 10.00m, 100);
        Assert.Throws<ArgumentException>(() => product.AddStock(invalidQuantity));
    }

    #endregion

    #region UpdateName Tests

    [Fact]
    public void UpdateName_WithValidName_UpdatesName()
    {
        // Arrange
        var product = new Product(1, "Old Name", 10.00m, 100);

        // Act
        product.UpdateName("New Name");

        // Assert
        Assert.Equal("New Name", product.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Tests error handling for empty/null/whitespace name
        var product = new Product(1, "Test", 10.00m, 100);
        Assert.Throws<ArgumentException>(() => product.UpdateName(invalidName!));
    }

    [Fact]
    public void UpdateName_WithSameName_Succeeds()
    {
        // Tests updating to the same name (idempotent operation)
        var product = new Product(1, "Same Name", 10.00m, 100);
        product.UpdateName("Same Name");
        Assert.Equal("Same Name", product.Name);
    }

    #endregion

    #region SetStockQuantity Tests

    [Fact]
    public void SetStockQuantity_WithValidQuantity_SetsStock()
    {
        // Arrange
        var product = new Product(1, "Test Product", 10.00m, 100);

        // Act
        product.SetStockQuantity(50);

        // Assert
        Assert.Equal(50, product.StockQuantity);
    }

    [Fact]
    public void SetStockQuantity_WithZero_SetsStockToZero()
    {
        // Tests boundary value - setting stock to zero
        var product = new Product(1, "Test", 10.00m, 100);
        product.SetStockQuantity(0);
        Assert.Equal(0, product.StockQuantity);
    }

    [Fact]
    public void SetStockQuantity_WithNegativeQuantity_ThrowsArgumentException()
    {
        // Tests error handling for negative quantity
        var product = new Product(1, "Test", 10.00m, 100);
        Assert.Throws<ArgumentException>(() => product.SetStockQuantity(-1));
    }

    [Fact]
    public void SetStockQuantity_WithLargeQuantity_Succeeds()
    {
        // Tests with large values
        var product = new Product(1, "Test", 10.00m, 100);
        product.SetStockQuantity(1000000);
        Assert.Equal(1000000, product.StockQuantity);
    }

    #endregion
}

