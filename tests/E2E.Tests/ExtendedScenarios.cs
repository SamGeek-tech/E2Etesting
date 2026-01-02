using Microsoft.Playwright.NUnit;
using E2E.Tests.Pages;
using NUnit.Framework;
using Microsoft.Playwright;

namespace E2E.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class ExtendedScenarios : PageTest
{
    private string _baseUrl = "http://localhost:5002";

    [SetUp]
    public async Task Setup()
    {
        var loginPage = new LoginPage(Page);
        await loginPage.GotoAsync(_baseUrl);
        await loginPage.LoginAsync("test@example.com", "Password123!");
    }

    [Test]
    public async Task Scenario1_VerifyEmptyOrdersMessage()
    {
        var ordersPage = new OrdersPage(Page);
        await ordersPage.GotoAsync(_baseUrl);
        
        var orderItems = Page.Locator(".order-item");
        if (await orderItems.CountAsync() == 0)
        {
            var msg = await ordersPage.GetEmptyMessageAsync();
            Assert.That(msg, Does.Contain("You haven't placed any orders yet."));
        }
    }

    [Test]
    public async Task Scenario2_OrderWithMultipleQuantity()
    {
        var shopPage = new ShopPage(Page);
        var ordersPage = new OrdersPage(Page);
        string productName = "Mouse";
        int quantity = 3;

        await shopPage.GotoAsync(_baseUrl);
        await shopPage.SetQuantityAsync(productName, quantity);
        await shopPage.PlaceOrderAsync(productName);

        await Expect(Page).ToHaveURLAsync($"{_baseUrl}/Orders");
        bool isVisible = await ordersPage.IsOrderVisibleAsync(productName);
        Assert.That(isVisible, Is.True);
        
        var orderText = await Page.Locator(".order-item").First.InnerTextAsync();
        Assert.That(orderText, Does.Contain($"{productName} x {quantity}"));
    }

    [Test]
    public async Task Scenario3_OutOfStockUIDisabled()
    {
        var shopPage = new ShopPage(Page);
        string productName = "Keyboard";

        await shopPage.GotoAsync(_baseUrl);
        int stock = await shopPage.GetStockCountAsync(productName);

        if (stock > 0)
        {
            await shopPage.SetQuantityAsync(productName, stock);
            await shopPage.PlaceOrderAsync(productName);

            await shopPage.GotoAsync(_baseUrl);
            bool isEnabled = await shopPage.IsOrderButtonEnabledAsync(productName);
            Assert.That(isEnabled, Is.False, "Order button should be disabled for out of stock item");
            
            var stockText = await Page.Locator(".card").Filter(new() { HasText = productName }).Locator("p:has-text('In Stock:')").InnerTextAsync();
            Assert.That(stockText, Does.Contain("In Stock: 0"));
        }
    }

    [Test]
    public async Task Scenario4_HeaderNavigationLinks()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{_baseUrl}/");

        await Page.GetByRole(AriaRole.Link, new() { Name = "Shop" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{_baseUrl}/Shop");

        await Page.GetByRole(AriaRole.Link, new() { Name = "My Orders" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{_baseUrl}/Orders");
    }

    [Test]
    public async Task Scenario5_SecureLogoutAndProtectedRoutes()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Logout" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Account/Login"));

        // Try to access Shop
        await Page.GotoAsync($"{_baseUrl}/Shop");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Account/Login"));
    }
}
