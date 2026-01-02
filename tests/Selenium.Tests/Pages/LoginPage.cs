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
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    private IWebElement EmailInput => _wait.Until(ExpectedConditions.ElementIsVisible(By.Id("email")));
    private IWebElement PasswordInput => _driver.FindElement(By.Id("password"));
    private IWebElement LoginButton => _driver.FindElement(By.Id("login-button"));
    private IWebElement ErrorSummary => _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".text-danger")));

    public void Visit(string baseUrl) => _driver.Navigate().GoToUrl($"{baseUrl}/Account/Login");

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
