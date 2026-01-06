using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderWeb.Mvc.Controllers;
using OrderWeb.Mvc.Models;
using Xunit;

namespace Unit.Tests.Mvc;

/// <summary>
/// Unit tests for HomeController - handles home page and error views
/// </summary>
public class HomeControllerTests
{
    private HomeController CreateController(string? traceIdentifier = null)
    {
        var controller = new HomeController();
        
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = traceIdentifier ?? "test-trace-id"
        };
        
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        return controller;
    }

    #region Index Tests

    [Fact]
    // Tests Index returns a ViewResult
    public void Index_ReturnsViewResult()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Index();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    // Tests Index returns view with null model
    public void Index_ReturnsViewWithNullModel()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Index() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Model);
    }

    #endregion

    #region Privacy Tests

    [Fact]
    // Tests Privacy returns a ViewResult
    public void Privacy_ReturnsViewResult()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Privacy();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    // Tests Privacy returns view with null model
    public void Privacy_ReturnsViewWithNullModel()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Privacy() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Model);
    }

    #endregion

    #region Error Tests

    [Fact]
    // Tests Error returns a ViewResult
    public void Error_ReturnsViewResult()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Error();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    // Tests Error returns view with ErrorViewModel
    public void Error_ReturnsViewWithErrorViewModel()
    {
        // Arrange
        var controller = CreateController("trace-123");

        // Act
        var result = controller.Error() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ErrorViewModel>(result.Model);
    }

    [Fact]
    // Tests Error uses HttpContext TraceIdentifier when no Activity
    public void Error_WithNoActivity_UsesTraceIdentifier()
    {
        // Arrange
        var expectedTraceId = "my-trace-identifier";
        var controller = CreateController(expectedTraceId);
        
        // Ensure no current activity
        Activity.Current = null;

        // Act
        var result = controller.Error() as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = result.Model as ErrorViewModel;
        Assert.NotNull(model);
        Assert.Equal(expectedTraceId, model.RequestId);
    }

    [Fact]
    // Tests Error uses Activity.Current.Id when available
    public void Error_WithActivity_UsesActivityId()
    {
        // Arrange
        var controller = CreateController();
        var activity = new Activity("TestActivity").Start();
        
        try
        {
            // Act
            var result = controller.Error() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as ErrorViewModel;
            Assert.NotNull(model);
            Assert.Equal(activity.Id, model.RequestId);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    // Tests Error has ResponseCache with no caching
    public void Error_HasNoCacheAttribute()
    {
        // Arrange
        var methodInfo = typeof(HomeController).GetMethod(nameof(HomeController.Error));

        // Act
        var attributes = methodInfo?.GetCustomAttributes(typeof(ResponseCacheAttribute), false);

        // Assert
        Assert.NotNull(attributes);
        var cacheAttribute = Assert.Single(attributes) as ResponseCacheAttribute;
        Assert.NotNull(cacheAttribute);
        Assert.Equal(0, cacheAttribute.Duration);
        Assert.Equal(ResponseCacheLocation.None, cacheAttribute.Location);
        Assert.True(cacheAttribute.NoStore);
    }

    #endregion

    #region ErrorViewModel Tests

    [Fact]
    // Tests ErrorViewModel default values
    public void ErrorViewModel_HasCorrectDefaults()
    {
        // Arrange & Act
        var viewModel = new ErrorViewModel();

        // Assert
        Assert.Null(viewModel.RequestId);
        Assert.False(viewModel.ShowRequestId);
    }

    [Fact]
    // Tests ErrorViewModel ShowRequestId when RequestId is set
    public void ErrorViewModel_WithRequestId_ShowRequestIdIsTrue()
    {
        // Arrange & Act
        var viewModel = new ErrorViewModel { RequestId = "request-123" };

        // Assert
        Assert.True(viewModel.ShowRequestId);
    }

    [Fact]
    // Tests ErrorViewModel ShowRequestId when RequestId is empty
    public void ErrorViewModel_WithEmptyRequestId_ShowRequestIdIsFalse()
    {
        // Arrange & Act
        var viewModel = new ErrorViewModel { RequestId = "" };

        // Assert
        Assert.False(viewModel.ShowRequestId);
    }

    [Fact]
    // Tests ErrorViewModel ShowRequestId when RequestId is whitespace - IsNullOrEmpty treats whitespace as non-empty
    public void ErrorViewModel_WithWhitespaceRequestId_ShowRequestIdIsTrue()
    {
        // Arrange & Act
        var viewModel = new ErrorViewModel { RequestId = "   " };

        // Assert - Whitespace is NOT empty, so ShowRequestId is true
        Assert.True(viewModel.ShowRequestId);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", true)]  // Whitespace is non-empty
    [InlineData("abc", true)]
    [InlineData("request-id-123", true)]
    // Tests ErrorViewModel ShowRequestId with various values (uses IsNullOrEmpty, not IsNullOrWhiteSpace)
    public void ErrorViewModel_ShowRequestId_VariousValues(string? requestId, bool expectedShowRequestId)
    {
        // Arrange & Act
        var viewModel = new ErrorViewModel { RequestId = requestId };

        // Assert
        Assert.Equal(expectedShowRequestId, viewModel.ShowRequestId);
    }

    #endregion
}

