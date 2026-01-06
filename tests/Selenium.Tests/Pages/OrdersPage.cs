using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Selenium.Tests.Pages;

public class OrdersPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public OrdersPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
    }

    public void Visit(string baseUrl)
    {
        var url = $"{baseUrl}/Orders";
        NavigateWithRetry(url);
    }

    private void NavigateWithRetry(string url, int maxRetries = 3)
    {
        Exception? lastException = null;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                _driver.Navigate().GoToUrl(url);
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

    public bool IsOrderVisible(string productName)
    {
        try
        {
            var element = _wait.Until(ExpectedConditions.ElementIsVisible(By.XPath($"//div[contains(@class, 'order-item') and contains(., '{productName}')]")));
            return element.Displayed;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public string GetEmptyMessage()
    {
        var element = _wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//p[contains(text(), \"You haven't placed any orders yet.\")]")));
        return element.Text;
    }
}
