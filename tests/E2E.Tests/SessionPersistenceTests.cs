using Microsoft.Playwright.NUnit;
using E2E.Tests.Pages;
using NUnit.Framework;
using Microsoft.Playwright;

namespace E2E.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class SessionPersistenceTests : PageTest
{
    private string _baseUrl = "http://127.0.0.1:5002";

    [Test]
    public async Task Scenario10_OrderPersistenceAcrossSessions()
    {
        // 1. Login and navigate to Orders
        var loginPage = new LoginPage(Page);
        await loginPage.GotoAsync(_baseUrl);
        await loginPage.LoginAsync("test@example.com", "Password123!");
        
        await Page.GotoAsync($"{_baseUrl}/Orders");
        int initialCount = await Page.Locator(".order-item").CountAsync();

        // 2. Create a NEW browser context to simulate a fresh session/window
        // Note: By default, Playwright PageTest shares context for speed, but constructing a new one mimics "closing and reopening"
        var newContext = await Browser.NewContextAsync();
        var newPage = await newContext.NewPageAsync();
        
        // 3. Login again in the new context (since session might not persist depending on auth setup, but we want to verify DATA persistence)
        var newLoginPage = new LoginPage(newPage);
        await newLoginPage.GotoAsync(_baseUrl);
        await newLoginPage.LoginAsync("test@example.com", "Password123!");

        await newPage.GotoAsync($"{_baseUrl}/Orders");
        int finalCount = await newPage.Locator(".order-item").CountAsync();

        Assert.That(finalCount, Is.EqualTo(initialCount), "Orders should persist across sessions for the same user");
        
        await newContext.CloseAsync();
    }
}
