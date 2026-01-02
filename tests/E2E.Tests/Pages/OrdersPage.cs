using Microsoft.Playwright;

namespace E2E.Tests.Pages;

public class OrdersPage
{
    private readonly IPage _page;

    public OrdersPage(IPage page) => _page = page;

    public async Task GotoAsync(string baseUrl) => await _page.GotoAsync($"{baseUrl}/Orders");

    public async Task<bool> IsOrderVisibleAsync(string productName)
    {
        var orderItem = _page.Locator(".order-item").Filter(new() { HasText = productName }).First;
        return await orderItem.IsVisibleAsync();
    }

    public async Task<string?> GetEmptyMessageAsync()
    {
        return await _page.Locator("p:has-text('You haven\\'t placed any orders yet.')").TextContentAsync();
    }
}
