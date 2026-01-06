using OrderService.Domain.Entities;
using Xunit;

namespace Unit.Tests.Domain;

/// <summary>
/// Unit tests for the Order domain entity.
/// Tests domain logic, invariants, and state transitions.
/// </summary>
public class OrderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidEmail_CreatesOrder()
    {
        // Arrange & Act
        var order = new Order("test@example.com");

        // Assert
        Assert.Equal("test@example.com", order.UserEmail);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(0m, order.TotalAmount);
        Assert.Empty(order.Items);
        Assert.True(order.OrderDate <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidEmail_ThrowsArgumentException(string? invalidEmail)
    {
        // Tests edge case where email is null or empty
        Assert.Throws<ArgumentException>(() => new Order(invalidEmail!));
    }

    #endregion

    #region AddItem Tests

    [Fact]
    public void AddItem_WithValidParameters_AddsItemToOrder()
    {
        // Arrange
        var order = new Order("test@example.com");

        // Act
        order.AddItem(1, "Test Product", 2, 10.00m);

        // Assert
        Assert.Single(order.Items);
        var item = order.Items.First();
        Assert.Equal(1, item.ProductId);
        Assert.Equal("Test Product", item.ProductName);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(10.00m, item.UnitPrice);
        Assert.Equal(20.00m, item.TotalPrice);
    }

    [Fact]
    public void AddItem_MultipleItems_CalculatesTotalCorrectly()
    {
        // Arrange
        var order = new Order("test@example.com");

        // Act
        order.AddItem(1, "Product A", 2, 10.00m);  // 20.00
        order.AddItem(2, "Product B", 3, 15.00m);  // 45.00

        // Assert
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(65.00m, order.TotalAmount);
    }

    [Fact]
    public void AddItem_SameProductTwice_CombinesQuantity()
    {
        // Tests that adding same product combines quantities
        var order = new Order("test@example.com");

        order.AddItem(1, "Test Product", 2, 10.00m);
        order.AddItem(1, "Test Product", 3, 10.00m);

        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
        Assert.Equal(50.00m, order.TotalAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_WithNonPositiveQuantity_ThrowsArgumentException(int invalidQuantity)
    {
        // Tests edge case where quantity is zero or negative
        var order = new Order("test@example.com");
        Assert.Throws<ArgumentException>(() => order.AddItem(1, "Test", invalidQuantity, 10.00m));
    }

    [Fact]
    public void AddItem_WithNegativePrice_ThrowsArgumentException()
    {
        // Tests edge case where price is negative
        var order = new Order("test@example.com");
        Assert.Throws<ArgumentException>(() => order.AddItem(1, "Test", 1, -10.00m));
    }

    [Fact]
    public void AddItem_ToConfirmedOrder_ThrowsInvalidOperationException()
    {
        // Tests that confirmed orders cannot be modified
        var order = new Order("test@example.com");
        order.AddItem(1, "Test", 1, 10.00m);
        order.Confirm();

        Assert.Throws<InvalidOperationException>(() => order.AddItem(2, "Another", 1, 5.00m));
    }

    #endregion

    #region Confirm Tests

    [Fact]
    public void Confirm_WithItems_ChangesStatusToConfirmed()
    {
        // Arrange
        var order = new Order("test@example.com");
        order.AddItem(1, "Test Product", 1, 10.00m);

        // Act
        order.Confirm();

        // Assert
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_EmptyOrder_ThrowsInvalidOperationException()
    {
        // Tests that empty orders cannot be confirmed
        var order = new Order("test@example.com");
        Assert.Throws<InvalidOperationException>(() => order.Confirm());
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_PendingOrder_ChangesStatusToCancelled()
    {
        // Arrange
        var order = new Order("test@example.com");
        order.AddItem(1, "Test", 1, 10.00m);

        // Act
        order.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_ConfirmedOrder_ChangesStatusToCancelled()
    {
        // Tests that confirmed orders can still be cancelled
        var order = new Order("test@example.com");
        order.AddItem(1, "Test", 1, 10.00m);
        order.Confirm();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    #endregion
}

/// <summary>
/// Unit tests for the OrderItem value object.
/// </summary>
public class OrderItemTests
{
    [Fact]
    public void TotalPrice_CalculatesCorrectly()
    {
        // Arrange
        var item = new OrderItem(1, "Test", 5, 10.00m);

        // Assert
        Assert.Equal(50.00m, item.TotalPrice);
    }

    [Fact]
    public void UpdateQuantity_WithValidQuantity_UpdatesQuantity()
    {
        // Arrange
        var item = new OrderItem(1, "Test", 5, 10.00m);

        // Act
        item.UpdateQuantity(10);

        // Assert
        Assert.Equal(10, item.Quantity);
        Assert.Equal(100.00m, item.TotalPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateQuantity_WithNonPositiveQuantity_ThrowsArgumentException(int invalidQuantity)
    {
        // Tests edge case where quantity is zero or negative
        var item = new OrderItem(1, "Test", 5, 10.00m);
        Assert.Throws<ArgumentException>(() => item.UpdateQuantity(invalidQuantity));
    }
}

