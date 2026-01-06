using Microsoft.Extensions.Configuration;
using Moq;
using OrderService.Application.Services;
using Xunit;

namespace Unit.Tests.Application;

/// <summary>
/// Unit tests for IdentityAppService.
/// Tests authentication use cases.
/// </summary>
public class IdentityAppServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly IdentityAppService _service;

    public IdentityAppServiceTests()
    {
        // Setup - Initialize configuration mock
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns("super_secret_key_that_is_long_enough_for_testing_123456");
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        
        _service = new IdentityAppService(_configMock.Object);
    }

    #region Login Tests

    [Fact]
    public void Login_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange - Using mock user credentials (test@example.com / Password123!)
        var email = "test@example.com";
        var password = "Password123!";

        // Act
        var result = _service.Login(email, password);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Login_WithInvalidEmail_ReturnsFailure()
    {
        // Tests error handling for wrong email
        var result = _service.Login("wrong@example.com", "Password123!");

        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Contains("Invalid", result.ErrorMessage);
    }

    [Fact]
    public void Login_WithInvalidPassword_ReturnsFailure()
    {
        // Tests error handling for wrong password
        var result = _service.Login("test@example.com", "WrongPassword");

        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Contains("Invalid", result.ErrorMessage);
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("test@example.com", "")]
    [InlineData("", "")]
    public void Login_WithEmptyCredentials_ReturnsFailure(string email, string password)
    {
        // Tests edge cases with empty credentials
        var result = _service.Login(email, password);

        Assert.False(result.Success);
        Assert.Null(result.Token);
    }

    [Fact]
    public void Login_GeneratesValidJwtToken()
    {
        // Arrange
        var result = _service.Login("test@example.com", "Password123!");

        // Assert - Verify JWT structure (header.payload.signature)
        Assert.True(result.Success);
        var tokenParts = result.Token!.Split('.');
        Assert.Equal(3, tokenParts.Length); // JWT has 3 parts separated by dots
    }

    [Theory]
    [InlineData("TEST@EXAMPLE.COM")]  // Uppercase
    [InlineData("Test@Example.Com")]  // Mixed case
    public void Login_IsCaseSensitiveForEmail(string email)
    {
        // Tests that email matching is case-sensitive
        var result = _service.Login(email, "Password123!");

        // Email should be case-sensitive, so these should fail
        Assert.False(result.Success);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Login_UsesConfiguredJwtSettings()
    {
        // Act - Perform login to trigger config access
        _service.Login("test@example.com", "Password123!");
        
        // Verify configuration is being used
        _configMock.Verify(c => c["Jwt:Key"], Times.AtLeastOnce);
    }

    [Fact]
    public void Login_WithNullJwtKey_UsesFallbackKey()
    {
        // Arrange - Test fallback when JWT key is not configured
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns((string?)null);
        configMock.Setup(c => c["Jwt:Issuer"]).Returns((string?)null);
        configMock.Setup(c => c["Jwt:Audience"]).Returns((string?)null);
        
        var service = new IdentityAppService(configMock.Object);

        // Act
        var result = service.Login("test@example.com", "Password123!");

        // Assert - Should still work with fallback key
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
    }

    #endregion
}

