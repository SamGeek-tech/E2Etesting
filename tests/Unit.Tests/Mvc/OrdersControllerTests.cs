using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using OrderWeb.Mvc.Controllers;
using Xunit;

namespace Unit.Tests.Mvc;

/// <summary>
/// Unit tests for OrdersController - handles order viewing
/// </summary>
public class OrdersControllerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public OrdersControllerTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configMock = new Mock<IConfiguration>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);
    }

    private OrdersController CreateController(string? token = "test-jwt-token")
    {
        var controller = new OrdersController(_httpClientFactoryMock.Object, _configMock.Object);
        
        // Setup authenticated user with token claim
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim("Token", token ?? "")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        
        return controller;
    }

    #region Index Tests

    [Fact]
    // Tests Index returns view with orders from service
    public async Task Index_ReturnsViewWithOrders()
    {
        // Arrange
        var controller = CreateController();
        var orders = new List<OrderViewModel>
        {
            new()
            {
                Id = 1,
                OrderDate = new DateTime(2024, 1, 15),
                TotalAmount = 99.99m,
                Items = new List<OrderItemViewModel>
                {
                    new() { ProductName = "Widget", Quantity = 2, UnitPrice = 49.995m }
                }
            },
            new()
            {
                Id = 2,
                OrderDate = new DateTime(2024, 1, 16),
                TotalAmount = 25.00m,
                Items = new List<OrderItemViewModel>()
            }
        };
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(orders), Encoding.UTF8, "application/json")
            });

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<OrderViewModel>>(viewResult.Model);
        Assert.Equal(2, model.Count);
        Assert.Equal(99.99m, model[0].TotalAmount);
    }

    [Fact]
    // Tests Index uses default URL when config is null
    public async Task Index_WithNullConfigUrl_UsesDefaultUrl()
    {
        // Arrange
        var controller = CreateController();
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns((string?)null);
        
        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });

        // Act
        await controller.Index();

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.StartsWith("http://localhost:5000", capturedRequest!.RequestUri!.ToString());
    }

    [Fact]
    // Tests Index includes authorization header with token
    public async Task Index_IncludesAuthorizationHeader()
    {
        // Arrange
        var expectedToken = "my-auth-token-xyz";
        var controller = CreateController(expectedToken);
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });

        // Act
        await controller.Index();

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal(expectedToken, capturedRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    // Tests Index handles empty order list
    public async Task Index_WithEmptyOrders_ReturnsEmptyList()
    {
        // Arrange
        var controller = CreateController();
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<OrderViewModel>>(viewResult.Model);
        Assert.Empty(model);
    }

    [Fact]
    // Tests Index calls correct API endpoint
    public async Task Index_CallsCorrectEndpoint()
    {
        // Arrange
        var controller = CreateController();
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://orderapi:5000");
        
        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });

        // Act
        await controller.Index();

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("http://orderapi:5000/api/orders", capturedRequest.RequestUri!.ToString());
    }

    [Fact]
    // Tests Index handles user without token claim
    public async Task Index_WithoutTokenClaim_SendsEmptyToken()
    {
        // Arrange
        var controller = CreateController(null);
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });

        // Act
        await controller.Index();

        // Assert
        Assert.NotNull(capturedRequest);
        // Empty token is still sent
        Assert.Equal("", capturedRequest!.Headers.Authorization?.Parameter);
    }

    #endregion

    #region OrderViewModel Tests

    [Fact]
    // Tests OrderViewModel default values
    public void OrderViewModel_HasCorrectDefaults()
    {
        // Arrange & Act
        var viewModel = new OrderViewModel();

        // Assert
        Assert.Equal(0, viewModel.Id);
        Assert.Equal(default(DateTime), viewModel.OrderDate);
        Assert.Equal(0m, viewModel.TotalAmount);
        Assert.NotNull(viewModel.Items);
        Assert.Empty(viewModel.Items);
    }

    [Fact]
    // Tests OrderViewModel with values
    public void OrderViewModel_WithValues_StoresCorrectly()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15, 10, 30, 0);
        var items = new List<OrderItemViewModel>
        {
            new() { ProductName = "Item 1", Quantity = 3, UnitPrice = 10m }
        };

        // Act
        var viewModel = new OrderViewModel
        {
            Id = 123,
            OrderDate = date,
            TotalAmount = 30m,
            Items = items
        };

        // Assert
        Assert.Equal(123, viewModel.Id);
        Assert.Equal(date, viewModel.OrderDate);
        Assert.Equal(30m, viewModel.TotalAmount);
        Assert.Single(viewModel.Items);
    }

    #endregion

    #region OrderItemViewModel Tests

    [Fact]
    // Tests OrderItemViewModel default values
    public void OrderItemViewModel_HasCorrectDefaults()
    {
        // Arrange & Act
        var viewModel = new OrderItemViewModel();

        // Assert
        Assert.Equal(string.Empty, viewModel.ProductName);
        Assert.Equal(0, viewModel.Quantity);
        Assert.Equal(0m, viewModel.UnitPrice);
    }

    [Fact]
    // Tests OrderItemViewModel with values
    public void OrderItemViewModel_WithValues_StoresCorrectly()
    {
        // Arrange & Act
        var viewModel = new OrderItemViewModel
        {
            ProductName = "Test Product",
            Quantity = 5,
            UnitPrice = 19.99m
        };

        // Assert
        Assert.Equal("Test Product", viewModel.ProductName);
        Assert.Equal(5, viewModel.Quantity);
        Assert.Equal(19.99m, viewModel.UnitPrice);
    }

    [Theory]
    [InlineData(1, 10.00, 10.00)]
    [InlineData(5, 20.00, 100.00)]
    [InlineData(0, 50.00, 0)]
    // Tests OrderItemViewModel total calculation (conceptually)
    public void OrderItemViewModel_TotalCalculation(int quantity, decimal unitPrice, decimal expectedTotal)
    {
        // Arrange
        var viewModel = new OrderItemViewModel
        {
            ProductName = "Product",
            Quantity = quantity,
            UnitPrice = unitPrice
        };

        // Act
        var calculatedTotal = viewModel.Quantity * viewModel.UnitPrice;

        // Assert
        Assert.Equal(expectedTotal, calculatedTotal);
    }

    #endregion
}

