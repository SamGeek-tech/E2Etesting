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
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void Visit(string baseUrl) => _driver.Navigate().GoToUrl($"{baseUrl}/Orders");

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
