using Microsoft.Playwright;

namespace E2E.Tests.Pages;

public class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page) => _page = page;

    private ILocator EmailInput => _page.Locator("#email");
    private ILocator PasswordInput => _page.Locator("#password");
    private ILocator LoginButton => _page.Locator("#login-button");
    private ILocator ErrorSummary => _page.Locator(".text-danger");

    public async Task GotoAsync(string baseUrl) => await _page.GotoAsync($"{baseUrl}/Account/Login");

    public async Task LoginAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
    }

    public async Task<string?> GetErrorMessageAsync() => await ErrorSummary.TextContentAsync();
}
