using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using OrderWeb.Mvc.Controllers;
using Xunit;

namespace Unit.Tests.Mvc;

/// <summary>
/// Unit tests for ShopController - handles product browsing and ordering
/// </summary>
public class ShopControllerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public ShopControllerTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configMock = new Mock<IConfiguration>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);
    }

    private ShopController CreateController(string? token = "test-jwt-token")
    {
        var controller = new ShopController(_httpClientFactoryMock.Object, _configMock.Object);
        
        // Setup authenticated user with token claim
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim("Token", token ?? "")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext { User = principal };
        
        // Setup TempData
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        controller.TempData = tempData;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        
        return controller;
    }

    #region Index Tests

    [Fact]
    // Tests Index returns view with products from inventory service
    public async Task Index_ReturnsViewWithProducts()
    {
        // Arrange
        var controller = CreateController();
        var products = new List<ProductViewModel>
        {
            new() { Id = 1, Name = "Widget", Price = 9.99m, StockQuantity = 100 },
            new() { Id = 2, Name = "Gadget", Price = 19.99m, StockQuantity = 50 }
        };
        
        _configMock.Setup(c => c["Services:InventoryUrl"]).Returns("http://localhost:5001");
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(products), Encoding.UTF8, "application/json")
            });

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<ProductViewModel>>(viewResult.Model);
        Assert.Equal(2, model.Count);
        Assert.Equal("Widget", model[0].Name);
    }

    [Fact]
    // Tests Index uses default URL when config is null
    public async Task Index_WithNullConfigUrl_UsesDefaultUrl()
    {
        // Arrange
        var controller = CreateController();
        
        _configMock.Setup(c => c["Services:InventoryUrl"]).Returns((string?)null);
        
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
        Assert.StartsWith("http://127.0.0.1:5001", capturedRequest!.RequestUri!.ToString());
    }

    [Fact]
    // Tests Index handles empty product list
    public async Task Index_WithEmptyProducts_ReturnsEmptyList()
    {
        // Arrange
        var controller = CreateController();
        
        _configMock.Setup(c => c["Services:InventoryUrl"]).Returns("http://localhost:5001");
        
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
        var model = Assert.IsAssignableFrom<List<ProductViewModel>>(viewResult.Model);
        Assert.Empty(model);
    }

    #endregion

    #region Order Tests

    [Fact]
    // Tests successful order redirects to Orders/Index
    public async Task Order_WithSuccessfulResponse_RedirectsToOrdersIndex()
    {
        // Arrange
        var controller = CreateController();
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Created });

        // Act
        var result = await controller.Order(1, "Test Product", 9.99m, 2);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Orders", redirectResult.ControllerName);
    }

    [Fact]
    // Tests failed order redirects to Index with error
    public async Task Order_WithFailedResponse_RedirectsToIndexWithError()
    {
        // Arrange
        var controller = CreateController();
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

        // Act
        var result = await controller.Order(1, "Test Product", 9.99m, 2);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Null(redirectResult.ControllerName);
        Assert.Equal("Failed to place order.", controller.TempData["Error"]);
    }

    [Fact]
    // Tests Order sends correct authorization header
    public async Task Order_IncludesAuthorizationHeader()
    {
        // Arrange
        var expectedToken = "my-jwt-token-123";
        var controller = CreateController(expectedToken);
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Created });

        // Act
        await controller.Order(1, "Test", 10m, 1);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal(expectedToken, capturedRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    // Tests Order sends correct payload to API
    public async Task Order_SendsCorrectPayload()
    {
        // Arrange
        var controller = CreateController();
        var productId = 42;
        var productName = "Super Widget";
        var price = 99.99m;
        var quantity = 5;
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        string? capturedBody = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                if (req.Content != null)
                    capturedBody = await req.Content.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Created });

        // Act
        await controller.Order(productId, productName, price, quantity);

        // Assert
        Assert.NotNull(capturedBody);
        Assert.Contains("42", capturedBody);
        Assert.Contains("Super Widget", capturedBody);
        Assert.Contains("5", capturedBody);
    }

    [Fact]
    // Tests Order uses default URL when config is null
    public async Task Order_WithNullConfigUrl_UsesDefaultUrl()
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
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Created });

        // Act
        await controller.Order(1, "Test", 10m, 1);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.StartsWith("http://127.0.0.1:5000", capturedRequest!.RequestUri!.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    // Tests Order with zero or negative quantity still makes request
    public async Task Order_WithInvalidQuantity_StillMakesRequest(int quantity)
    {
        // Arrange
        var controller = CreateController();
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        var requestMade = false;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((_, _) => requestMade = true)
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

        // Act
        await controller.Order(1, "Test", 10m, quantity);

        // Assert - Validation is on server side
        Assert.True(requestMade);
    }

    #endregion

    #region ProductViewModel Tests

    [Fact]
    // Tests ProductViewModel default values
    public void ProductViewModel_HasCorrectDefaults()
    {
        // Arrange & Act
        var viewModel = new ProductViewModel();

        // Assert
        Assert.Equal(0, viewModel.Id);
        Assert.Equal(string.Empty, viewModel.Name);
        Assert.Equal(0m, viewModel.Price);
        Assert.Equal(0, viewModel.StockQuantity);
    }

    [Fact]
    // Tests ProductViewModel with values
    public void ProductViewModel_WithValues_StoresCorrectly()
    {
        // Arrange & Act
        var viewModel = new ProductViewModel
        {
            Id = 1,
            Name = "Test Product",
            Price = 29.99m,
            StockQuantity = 150
        };

        // Assert
        Assert.Equal(1, viewModel.Id);
        Assert.Equal("Test Product", viewModel.Name);
        Assert.Equal(29.99m, viewModel.Price);
        Assert.Equal(150, viewModel.StockQuantity);
    }

    #endregion
}

