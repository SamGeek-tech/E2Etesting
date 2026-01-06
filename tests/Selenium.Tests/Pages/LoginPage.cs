using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Selenium.Tests.Pages;

public class LoginPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public LoginPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
    }

    private IWebElement EmailInput => _wait.Until(ExpectedConditions.ElementIsVisible(By.Id("email")));
    private IWebElement PasswordInput => _driver.FindElement(By.Id("password"));
    private IWebElement LoginButton => _driver.FindElement(By.Id("login-button"));
    private IWebElement ErrorSummary => _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".text-danger")));

    public void Visit(string baseUrl)
    {
        var url = $"{baseUrl}/Account/Login";
        var maxRetries = 3;
        Exception? lastException = null;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                _driver.Navigate().GoToUrl(url);
                // Wait for page to be in a ready state
                _wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString() == "complete");
                return;
            }
            catch (WebDriverException ex) when (ex.Message.Contains("ERR_CONNECTION_REFUSED") || ex.Message.Contains("net::"))
            {
                lastException = ex;
                Console.WriteLine($"Connection attempt {i + 1}/{maxRetries} failed, retrying in 5 seconds...");
                Thread.Sleep(5000);
            }
        }

        throw lastException ?? new Exception($"Failed to navigate to {url} after {maxRetries} attempts");
    }

    public void Login(string email, string password)
    {
        EmailInput.Clear();
        EmailInput.SendKeys(email);
        PasswordInput.Clear();
        PasswordInput.SendKeys(password);
        LoginButton.Click();
    }

    public string GetErrorMessage() => ErrorSummary.Text;
}
