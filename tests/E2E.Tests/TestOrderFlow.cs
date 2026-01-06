using Microsoft.Playwright.NUnit;
using E2E.Tests.Pages;
using NUnit.Framework;

namespace E2E.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class TestOrderFlow : PageTest
{
    private string _baseUrl = "http://127.0.0.1:5002"; // MVC App Port

    [Test]
    public async Task FullUserJourney_PlaceOrder_VerifyHistory()
    {
        // 1. Arrange
        var loginPage = new LoginPage(Page);
        var shopPage = new ShopPage(Page);
        var ordersPage = new OrdersPage(Page);

        // 2. Act: Login
        await loginPage.GotoAsync(_baseUrl);
        await loginPage.LoginAsync("test@example.com", "Password123!");

        // Assert: Redirect to Home
        await Expect(Page).ToHaveURLAsync($"{_baseUrl}/");

        // 3. Act: Browse and Order Mouse
        await shopPage.GotoAsync(_baseUrl);
        int initialStock = await shopPage.GetStockCountAsync("Mouse");
        await shopPage.PlaceOrderAsync("Mouse");

        // Assert: Redirect to Orders
        await Expect(Page).ToHaveURLAsync($"{_baseUrl}/Orders");

        // 4. Assert: Order exists in history
        bool isOrderVisible = await ordersPage.IsOrderVisibleAsync("Mouse");
        Assert.That(isOrderVisible, Is.True, "Order should be visible in history");

        // 5. Assert: Stock depleted (Verification after reload or by revisiting shop)
        await shopPage.GotoAsync(_baseUrl);
        int finalStock = await shopPage.GetStockCountAsync("Mouse");
        Assert.That(finalStock, Is.EqualTo(initialStock - 1), "Stock should be decremented");
    }

    [Test]
    public async Task InvalidLogin_ShowsError()
    {
        var loginPage = new LoginPage(Page);
        await loginPage.GotoAsync(_baseUrl);
        await loginPage.LoginAsync("wrong@example.com", "WrongPass");

        var error = await loginPage.GetErrorMessageAsync();
        Assert.That(error, Does.Contain("Invalid login attempt"), "Should show error message");
    }
}
