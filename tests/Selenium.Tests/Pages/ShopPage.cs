using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Selenium.Tests.Pages;

public class ShopPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public ShopPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
    }

    public void Visit(string baseUrl)
    {
        var url = $"{baseUrl}/Shop";
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

    private IWebElement GetProductCard(string productName)
    {
        return _wait.Until(ExpectedConditions.ElementIsVisible(By.XPath($"//div[contains(@class, 'card') and .//h5[text()='{productName}']]")));
    }

    public void PlaceOrder(string productName)
    {
        var card = GetProductCard(productName);
        card.FindElement(By.CssSelector(".place-order-btn")).Click();
    }

    public void SetQuantity(string productName, int quantity)
    {
        var card = GetProductCard(productName);
        var input = card.FindElement(By.Name("quantity"));
        input.Clear();
        input.SendKeys(quantity.ToString());
    }

    public bool IsOrderButtonEnabled(string productName)
    {
        var card = GetProductCard(productName);
        return card.FindElement(By.CssSelector(".place-order-btn")).Enabled;
    }

    public int GetStockCount(string productName)
    {
        var card = GetProductCard(productName);
        var text = card.FindElement(By.XPath(".//p[contains(text(), 'In Stock:')]")).Text;
        return int.Parse(text.Replace("In Stock: ", "").Trim());
    }
}
