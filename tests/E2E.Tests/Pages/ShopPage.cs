using Microsoft.Playwright;

namespace E2E.Tests.Pages;

public class ShopPage
{
    private readonly IPage _page;

    public ShopPage(IPage page) => _page = page;

    public async Task GotoAsync(string baseUrl) => await _page.GotoAsync($"{baseUrl}/Shop");

    public async Task PlaceOrderAsync(string productName)
    {
        // Find the card with the specific product name
        var card = _page.Locator(".card").Filter(new() { HasText = productName });
        await card.Locator(".place-order-btn").ClickAsync();
    }

    public async Task SetQuantityAsync(string productName, int quantity)
    {
        var card = _page.Locator(".card").Filter(new() { HasText = productName });
        await card.Locator("input[name='quantity']").FillAsync(quantity.ToString());
    }

    public async Task<bool> IsOrderButtonEnabledAsync(string productName)
    {
        var card = _page.Locator(".card").Filter(new() { HasText = productName });
        return await card.Locator(".place-order-btn").IsEnabledAsync();
    }

    public async Task<int> GetStockCountAsync(string productName)
    {
        var card = _page.Locator(".card").Filter(new() { HasText = productName });
        var text = await card.Locator("p:has-text('In Stock:')").TextContentAsync();
        return int.Parse(text?.Replace("In Stock: ", "").Trim() ?? "0");
    }
}
