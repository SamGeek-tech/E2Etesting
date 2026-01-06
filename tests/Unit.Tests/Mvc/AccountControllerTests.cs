using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using OrderWeb.Mvc.Controllers;
using Xunit;

namespace Unit.Tests.Mvc;

/// <summary>
/// Unit tests for AccountController - handles user authentication flow
/// </summary>
public class AccountControllerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public AccountControllerTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configMock = new Mock<IConfiguration>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);
    }

    private AccountController CreateController()
    {
        var controller = new AccountController(_httpClientFactoryMock.Object, _configMock.Object);
        
        // Setup HttpContext with required services
        var httpContext = new DefaultHttpContext();
        
        // Mock Authentication service
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock
            .Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        authServiceMock
            .Setup(x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        // Mock TempData
        var tempDataMock = new Mock<ITempDataDictionary>();
        controller.TempData = tempDataMock.Object;

        // Mock URL Helper
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/test");
        controller.Url = urlHelperMock.Object;
        
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);
        
        httpContext.RequestServices = serviceProviderMock.Object;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        
        return controller;
    }

    #region Login GET Tests

    [Fact]
    // Tests that GET Login returns the login view
    public void LoginGet_ReturnsViewResult()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Login();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    #endregion

    #region Login POST Tests

    [Fact]
    // Tests successful login with valid credentials redirects to Home
    public async Task LoginPost_WithValidCredentials_RedirectsToHome()
    {
        // Arrange
        var controller = CreateController();
        var email = "test@example.com";
        var password = "password123";
        var token = "jwt-token-12345";
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        var loginResponse = new { Token = token };
        var responseContent = JsonSerializer.Serialize(loginResponse);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await controller.Login(email, password);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    // Tests that failed login returns view with model error
    public async Task LoginPost_WithInvalidCredentials_ReturnsViewWithError()
    {
        // Arrange
        var controller = CreateController();
        var email = "invalid@example.com";
        var password = "wrongpassword";
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized
            });

        // Act
        var result = await controller.Login(email, password);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(""));
    }

    [Fact]
    // Tests login uses default URL when config is null
    public async Task LoginPost_WithNullConfigUrl_UsesDefaultUrl()
    {
        // Arrange
        var controller = CreateController();
        var email = "test@example.com";
        var password = "password123";
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns((string?)null);
        
        var loginResponse = new { Token = "token" };
        var responseContent = JsonSerializer.Serialize(loginResponse);
        
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
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        await controller.Login(email, password);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.StartsWith("http://localhost:5000", capturedRequest!.RequestUri!.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    // Tests login with empty email
    public async Task LoginPost_WithEmptyEmail_StillAttempsLogin(string email)
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
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        var result = await controller.Login(email, "password");

        // Assert - Controller still attempts login (validation is server-side)
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    // Tests login handles null token in response gracefully
    public async Task LoginPost_WithNullTokenInResponse_HandlesGracefully()
    {
        // Arrange
        var controller = CreateController();
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://localhost:5000");
        
        var loginResponse = new { Token = (string?)null };
        var responseContent = JsonSerializer.Serialize(loginResponse);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await controller.Login("test@example.com", "password");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    // Tests login sends correct request to identity endpoint
    public async Task LoginPost_SendsCorrectRequestToIdentityEndpoint()
    {
        // Arrange
        var controller = CreateController();
        var email = "test@example.com";
        var password = "secret123";
        
        _configMock.Setup(c => c["Services:OrderServiceUrl"]).Returns("http://orderservice:5000");
        
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedRequest = req;
                if (req.Content != null)
                    capturedBody = await req.Content.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Token\":\"tok\"}", Encoding.UTF8, "application/json")
            });

        // Act
        await controller.Login(email, password);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("http://orderservice:5000/api/identity/login", capturedRequest.RequestUri!.ToString());
        Assert.NotNull(capturedBody);
        Assert.Contains(email, capturedBody);
    }

    #endregion

    #region Logout Tests

    [Fact]
    // Tests logout redirects to login page
    public async Task Logout_RedirectsToLogin()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.Logout();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
        Assert.Null(redirectResult.ControllerName);
    }

    #endregion

    #region LoginResponse Record Tests

    [Fact]
    // Tests LoginResponse record serialization
    public void LoginResponse_SerializesCorrectly()
    {
        // Arrange
        var response = new LoginResponse("test-token");

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<LoginResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.Equal("test-token", deserialized?.Token);
    }

    [Fact]
    // Tests LoginResponse with null token
    public void LoginResponse_WithNullToken_IsValid()
    {
        // Arrange & Act
        var response = new LoginResponse(null!);

        // Assert
        Assert.Null(response.Token);
    }

    #endregion
}

