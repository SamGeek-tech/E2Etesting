using Moq;
using OrderService.Application.DTOs;
using OrderService.Application.Services;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using Xunit;

namespace Unit.Tests.Application;

/// <summary>
/// Unit tests for OrderAppService.
/// Tests order use cases with mocked dependencies.
/// </summary>
public class OrderAppServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IInventoryClient> _inventoryClientMock;
    private readonly OrderAppService _service;

    public OrderAppServiceTests()
    {
        // Setup - Initialize mocks for all tests
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _inventoryClientMock = new Mock<IInventoryClient>();
        _service = new OrderAppService(_orderRepositoryMock.Object, _inventoryClientMock.Object);
    }

    #region GetUserOrdersAsync Tests

    [Fact]
    public async Task GetUserOrdersAsync_WithOrders_ReturnsUserOrders()
    {
        // Arrange
        var userEmail = "test@example.com";
        var order1 = CreateTestOrder(userEmail, 1, "Product A", 1, 10.00m);
        var order2 = CreateTestOrder(userEmail, 2, "Product B", 2, 20.00m);

        _orderRepositoryMock.Setup(r => r.GetByUserEmailAsync(userEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order1, order2 });

        // Act
        var result = await _service.GetUserOrdersAsync(userEmail);

        // Assert
        var orders = result.ToList();
        Assert.Equal(2, orders.Count);
        Assert.All(orders, o => Assert.Equal(userEmail, o.UserEmail));
    }

    [Fact]
    public async Task GetUserOrdersAsync_WithNoOrders_ReturnsEmptyList()
    {
        // Arrange - Empty collection edge case
        _orderRepositoryMock.Setup(r => r.GetByUserEmailAsync("noorders@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        // Act
        var result = await _service.GetUserOrdersAsync("noorders@example.com");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetOrderByIdAsync Tests

    [Fact]
    public async Task GetOrderByIdAsync_WithExistingOrder_ReturnsOrderDto()
    {
        // Arrange
        var order = CreateTestOrder("test@example.com", 1, "Test Product", 2, 25.00m);
        _orderRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetOrderByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.UserEmail);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WithNonExistingOrder_ReturnsNull()
    {
        // Arrange - Order not found edge case
        _orderRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.GetOrderByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateOrderAsync Tests

    [Fact]
    public async Task CreateOrderAsync_WithValidRequest_CreatesOrderSuccessfully()
    {
        // Arrange
        var userEmail = "test@example.com";
        var request = new CreateOrderRequest(new List<CreateOrderItemRequest>
        {
            new(1, "Product A", 2, 10.00m),
            new(2, "Product B", 1, 25.00m)
        });

        _inventoryClientMock.Setup(c => c.ReserveStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _service.CreateOrderAsync(userEmail, request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Order);
        Assert.Equal(userEmail, result.Order.UserEmail);
        Assert.Equal(2, result.Order.Items.Count);
        Assert.Equal(45.00m, result.Order.TotalAmount); // (2*10) + (1*25) = 45
    }

    [Fact]
    public async Task CreateOrderAsync_WithInventoryReservationFailure_ReturnsFailure()
    {
        // Arrange - Tests error handling when inventory reservation fails
        var request = new CreateOrderRequest(new List<CreateOrderItemRequest>
        {
            new(1, "Product A", 100, 10.00m) // Large quantity that fails
        });

        _inventoryClientMock.Setup(c => c.ReserveStockAsync(1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateOrderAsync("test@example.com", request);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Order);
        Assert.Contains("Failed to reserve", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateOrderAsync_WithPartialInventoryFailure_RollsBackReservations()
    {
        // Arrange - Tests rollback when second item fails
        var request = new CreateOrderRequest(new List<CreateOrderItemRequest>
        {
            new(1, "Product A", 5, 10.00m),
            new(2, "Product B", 100, 20.00m) // This one fails
        });

        _inventoryClientMock.Setup(c => c.ReserveStockAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _inventoryClientMock.Setup(c => c.ReserveStockAsync(2, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateOrderAsync("test@example.com", request);

        // Assert
        Assert.False(result.Success);
        // Verify rollback was attempted for the first item
        _inventoryClientMock.Verify(c => c.ReleaseStockAsync(1, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithEmptyItems_ReturnsFailure()
    {
        // Arrange - Tests edge case with empty order items
        var request = new CreateOrderRequest(new List<CreateOrderItemRequest>());

        // Act
        var result = await _service.CreateOrderAsync("test@example.com", request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot confirm an empty order", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateOrderAsync_VerifiesOrderIsConfirmed()
    {
        // Arrange
        var request = new CreateOrderRequest(new List<CreateOrderItemRequest>
        {
            new(1, "Product", 1, 10.00m)
        });

        _inventoryClientMock.Setup(c => c.ReserveStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Order? savedOrder = null;
        _orderRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => savedOrder = o)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        await _service.CreateOrderAsync("test@example.com", request);

        // Assert - Verify order was confirmed before saving
        Assert.NotNull(savedOrder);
        Assert.Equal("Confirmed", savedOrder!.Status.ToString());
    }

    #endregion

    #region Helper Methods

    private static Order CreateTestOrder(string email, int productId, string productName, int quantity, decimal price)
    {
        var order = new Order(email);
        order.AddItem(productId, productName, quantity, price);
        order.Confirm();
        return order;
    }

    #endregion
}

