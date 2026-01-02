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
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void Visit(string baseUrl) => _driver.Navigate().GoToUrl($"{baseUrl}/Shop");

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
