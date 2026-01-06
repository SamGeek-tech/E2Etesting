using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using OrderWeb.Mvc.Controllers;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Unit.Tests;

[TestFixture]
public class ShopControllerTests
{
    private Mock<IHttpClientFactory> _httpClientFactoryMock = null!;
    private Mock<IConfiguration> _configMock = null!;
    private ShopController _controller = null!;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;

    private const string TestUserEmail = "test@example.com";
    private const string TestToken = "test-jwt-token";

    [SetUp]
    public void Setup()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(x => x["Services:InventoryUrl"]).Returns("http://localhost:5001");
        _configMock.Setup(x => x["Services:OrderServiceUrl"]).Returns("http://localhost:5000");

        _controller = new ShopController(_httpClientFactoryMock.Object, _configMock.Object);

        SetupUserContext();
        SetupTempData();
    }

    private void SetupUserContext()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, TestUserEmail),
            new(ClaimTypes.Name, TestUserEmail),
            new("Token", TestToken)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetupTempData()
    {
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionaryFactory = new TempDataDictionaryFactory(tempDataProvider.Object);
        _controller.TempData = tempDataDictionaryFactory.GetTempData(new DefaultHttpContext());
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    private void SetupInventoryResponse(List<ProductViewModel> products)
    {
        var json = JsonSerializer.Serialize(products);
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.ToString().Contains("/api/inventory")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
    }

    private void SetupOrderResponse(HttpStatusCode statusCode)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.ToString().Contains("/api/orders")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode
            });
    }

    #region Index Tests

    [Test]
    public async Task Index_ReturnsViewWithProducts()
    {
        // Arrange
        var products = new List<ProductViewModel>
        {
            new() { Id = 1, Name = "Laptop", Price = 999.99m, StockQuantity = 10 },
            new() { Id = 2, Name = "Mouse", Price = 25.50m, StockQuantity = 50 }
        };
        SetupInventoryResponse(products);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Model, Is.InstanceOf<List<ProductViewModel>>());
        
        var model = result.Model as List<ProductViewModel>;
        Assert.That(model!.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Index_ReturnsCorrectProductDetails()
    {
        // Arrange
        var products = new List<ProductViewModel>
        {
            new() { Id = 1, Name = "Laptop", Price = 999.99m, StockQuantity = 10 }
        };
        SetupInventoryResponse(products);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        var model = result!.Model as List<ProductViewModel>;
        var product = model!.First();
        
        Assert.That(product.Name, Is.EqualTo("Laptop"));
        Assert.That(product.Price, Is.EqualTo(999.99m));
        Assert.That(product.StockQuantity, Is.EqualTo(10));
    }

    [Test]
    public async Task Index_WithEmptyInventory_ReturnsEmptyList()
    {
        // Arrange
        SetupInventoryResponse(new List<ProductViewModel>());

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        var model = result!.Model as List<ProductViewModel>;
        Assert.That(model, Is.Empty);
    }

    #endregion

    #region Order Tests

    [Test]
    public async Task Order_WithSuccessfulResponse_RedirectsToOrdersIndex()
    {
        // Arrange
        SetupOrderResponse(HttpStatusCode.OK);

        // Act
        var result = await _controller.Order(
            productId: 1,
            productName: "Laptop",
            price: 999.99m,
            quantity: 1) as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ActionName, Is.EqualTo("Index"));
        Assert.That(result.ControllerName, Is.EqualTo("Orders"));
    }

    [Test]
    public async Task Order_WithFailedResponse_RedirectsToShopIndex()
    {
        // Arrange
        SetupOrderResponse(HttpStatusCode.BadRequest);

        // Act
        var result = await _controller.Order(
            productId: 1,
            productName: "Laptop",
            price: 999.99m,
            quantity: 1) as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ActionName, Is.EqualTo("Index"));
        Assert.That(result.ControllerName, Is.Null); // Same controller
    }

    [Test]
    public async Task Order_WithFailedResponse_SetsErrorInTempData()
    {
        // Arrange
        SetupOrderResponse(HttpStatusCode.BadRequest);

        // Act
        await _controller.Order(
            productId: 1,
            productName: "Laptop",
            price: 999.99m,
            quantity: 1);

        // Assert
        Assert.That(_controller.TempData["Error"], Is.EqualTo("Failed to place order."));
    }

    [Test]
    public async Task Order_WithSuccessfulResponse_DoesNotSetErrorInTempData()
    {
        // Arrange
        SetupOrderResponse(HttpStatusCode.OK);

        // Act
        await _controller.Order(
            productId: 1,
            productName: "Laptop",
            price: 999.99m,
            quantity: 1);

        // Assert
        Assert.That(_controller.TempData.ContainsKey("Error"), Is.False);
    }

    #endregion
}

