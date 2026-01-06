using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using OrderService.Api.Controllers;
using OrderService.Api.Data;
using OrderService.Api.Models;
using System.Net;
using System.Security.Claims;

namespace Unit.Tests;

[TestFixture]
public class OrdersControllerTests
{
    private OrderDbContext _context = null!;
    private Mock<IHttpClientFactory> _httpClientFactoryMock = null!;
    private Mock<IConfiguration> _configMock = null!;
    private OrdersController _controller = null!;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;

    private const string TestUserEmail = "test@example.com";

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrderDbContext(options);

        // Seed test data
        _context.Orders.Add(new Order
        {
            Id = 1,
            UserEmail = TestUserEmail,
            OrderDate = DateTime.UtcNow.AddDays(-1),
            TotalAmount = 100.00m,
            Items = new List<OrderItem>
            {
                new() { Id = 1, ProductId = 1, ProductName = "Laptop", Quantity = 1, UnitPrice = 100.00m }
            }
        });
        _context.Orders.Add(new Order
        {
            Id = 2,
            UserEmail = "other@example.com",
            OrderDate = DateTime.UtcNow,
            TotalAmount = 50.00m,
            Items = new List<OrderItem>
            {
                new() { Id = 2, ProductId = 2, ProductName = "Mouse", Quantity = 2, UnitPrice = 25.00m }
            }
        });
        _context.SaveChanges();

        // Setup mocks
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(x => x["Services:InventoryUrl"]).Returns("http://localhost:5001");

        _controller = new OrdersController(_context, _httpClientFactoryMock.Object, _configMock.Object);

        // Set up user claims
        SetupUserContext(TestUserEmail);
    }

    private void SetupUserContext(string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetupHttpClientResponse(HttpStatusCode statusCode)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode
            });
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetOrders Tests

    [Test]
    public async Task GetOrders_ReturnsOnlyCurrentUserOrders()
    {
        // Act
        var result = await _controller.GetOrders();

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Count(), Is.EqualTo(1));
        Assert.That(result.Value!.First().UserEmail, Is.EqualTo(TestUserEmail));
    }

    [Test]
    public async Task GetOrders_IncludesOrderItems()
    {
        // Act
        var result = await _controller.GetOrders();

        // Assert
        var order = result.Value!.First();
        Assert.That(order.Items, Is.Not.Null);
        Assert.That(order.Items.Count, Is.EqualTo(1));
        Assert.That(order.Items.First().ProductName, Is.EqualTo("Laptop"));
    }

    [Test]
    public async Task GetOrders_ForUserWithNoOrders_ReturnsEmptyList()
    {
        // Arrange
        SetupUserContext("newuser@example.com");

        // Act
        var result = await _controller.GetOrders();

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Count(), Is.EqualTo(0));
    }

    #endregion

    #region CreateOrder Tests

    [Test]
    public async Task CreateOrder_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        SetupHttpClientResponse(HttpStatusCode.OK);
        var request = new CreateOrderRequest(new List<CreateOrderItemRequest>
        {
            new(ProductId: 1, ProductName: "Test Product", Quantity: 2, UnitPrice: 50.00m)
        });

        // Act
        var result = await _controller.CreateOrder(request);

        // Assert
        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task CreateOrder_WithValidRequest_CalculatesTotalAmountCorrectly()
    {
        // Arrange
        SetupHttpClientResponse(HttpStatusCode.OK);
        var request = new CreateOrderRequest(new List<CreateOrderItemRequest>
        {
            new(ProductId: 1, ProductName: "Product A", Quantity: 2, UnitPrice: 50.00m),
            new(ProductId: 2, ProductName: "Product B", Quantity: 3, UnitPrice: 30.00m)
        });

        // Act
        var result = await _controller.CreateOrder(request) as CreatedAtActionResult;

        // Assert
        var order = result!.Value as Order;
        Assert.That(order!.TotalAmount, Is.EqualTo(190.00m)); // (2*50) + (3*30) = 100 + 90
    }

    [Test]
    public async Task CreateOrder_WithValidRequest_SetsCorrectUserEmail()
    {
        // Arrange
        SetupHttpClientResponse(HttpStatusCode.OK);
        var request = new CreateOrderRequest(new List<CreateOrderItemRequest>
        {
            new(ProductId: 1, ProductName: "Test Product", Quantity: 1, UnitPrice: 100.00m)
        });

        // Act
        var result = await _controller.CreateOrder(request) as CreatedAtActionResult;

        // Assert
        var order = result!.Value as Order;
        Assert.That(order!.UserEmail, Is.EqualTo(TestUserEmail));
    }

    [Test]
    public async Task CreateOrder_WhenInventoryServiceFails_ReturnsBadRequest()
    {
        // Arrange
        SetupHttpClientResponse(HttpStatusCode.BadRequest);
        var request = new CreateOrderRequest(new List<CreateOrderItemRequest>
        {
            new(ProductId: 1, ProductName: "Test Product", Quantity: 1000, UnitPrice: 50.00m)
        });

        // Act
        var result = await _controller.CreateOrder(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateOrder_SavesOrderToDatabase()
    {
        // Arrange
        SetupHttpClientResponse(HttpStatusCode.OK);
        var initialCount = await _context.Orders.CountAsync();
        var request = new CreateOrderRequest(new List<CreateOrderItemRequest>
        {
            new(ProductId: 1, ProductName: "New Product", Quantity: 1, UnitPrice: 75.00m)
        });

        // Act
        await _controller.CreateOrder(request);

        // Assert
        var finalCount = await _context.Orders.CountAsync();
        Assert.That(finalCount, Is.EqualTo(initialCount + 1));
    }

    [Test]
    public async Task CreateOrder_SetsOrderDateToUtcNow()
    {
        // Arrange
        SetupHttpClientResponse(HttpStatusCode.OK);
        var beforeCreate = DateTime.UtcNow;
        var request = new CreateOrderRequest(new List<CreateOrderItemRequest>
        {
            new(ProductId: 1, ProductName: "Test", Quantity: 1, UnitPrice: 10.00m)
        });

        // Act
        var result = await _controller.CreateOrder(request) as CreatedAtActionResult;
        var afterCreate = DateTime.UtcNow;

        // Assert
        var order = result!.Value as Order;
        Assert.That(order!.OrderDate, Is.InRange(beforeCreate, afterCreate));
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

