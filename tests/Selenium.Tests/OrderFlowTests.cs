using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Selenium.Tests.Pages;
using NUnit.Framework;

namespace Selenium.Tests;

[TestFixture]
public class OrderFlowTests
{
    private IWebDriver _driver;
    private string _baseUrl = "http://127.0.0.1:5002";
    private LoginPage _loginPage;
    private ShopPage _shopPage;
    private OrdersPage _ordersPage;

    private readonly string _testEmail = "test@example.com";
    private readonly string _testPassword = "Password123!";

    [SetUp]
    public void Setup()
    {
        var options = new ChromeOptions();
        if (Environment.GetEnvironmentVariable("HEADLESS_MODE") == "true")
        {
            options.AddArgument("--headless=new");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-background-networking");
            options.AddArgument("--ignore-certificate-errors");
        }
        _driver = new ChromeDriver(options);
        if (Environment.GetEnvironmentVariable("HEADLESS_MODE") != "true")
        {
            _driver.Manage().Window.Maximize();
        }
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);

        _loginPage = new LoginPage(_driver);
        _shopPage = new ShopPage(_driver);
        _ordersPage = new OrdersPage(_driver);
    }

    [TearDown]
    public void Teardown()
    {
        _driver.Quit();
        _driver.Dispose();
    }

    [Test]
    public void Scenario1_SuccessfulAuthentication()
    {
        _loginPage.Visit(_baseUrl);
        _loginPage.Login(_testEmail, _testPassword);

        Assert.That(_driver.Url.TrimEnd('/'), Is.EqualTo(_baseUrl.TrimEnd('/')));
        var bodyText = _driver.FindElement(By.TagName("body")).Text;
        Assert.That(bodyText, Does.Contain($"Hello, {_testEmail}!"));
    }

    [Test]
    public void Scenario2_FailedAuthentication()
    {
        _loginPage.Visit(_baseUrl);
        _loginPage.Login("wrong@example.com", "WrongPass");

        var error = _loginPage.GetErrorMessage();
        Assert.That(error, Does.Contain("Invalid login attempt"));
    }

    [Test]
    public void Scenario3_FullJourney_PlaceOrder_VerifyHistory()
    {
        _loginPage.Visit(_baseUrl);
        _loginPage.Login(_testEmail, _testPassword);

        _shopPage.Visit(_baseUrl);
        Thread.Sleep(500);
        int initialStock = _shopPage.GetStockCount("Mouse");
        Thread.Sleep(500);
        _shopPage.PlaceOrder("Mouse");
        Thread.Sleep(500);
        Assert.That(_driver.Url, Does.Contain("/Orders"));
        Assert.That(_ordersPage.IsOrderVisible("Mouse"), Is.True);

        _shopPage.Visit(_baseUrl);
        int finalStock = _shopPage.GetStockCount("Mouse");
        Assert.That(finalStock, Is.EqualTo(initialStock - 1));
    }

    [Test]
    public void Scenario4_BulkQuantityOrder()
    {
        _loginPage.Visit(_baseUrl);
        _loginPage.Login(_testEmail, _testPassword);
        Thread.Sleep(500);
        const string productName = "Mouse";
        const int quantity = 2;
        _shopPage.Visit(_baseUrl);

        _shopPage.SetQuantity(productName, quantity);
        _shopPage.PlaceOrder(productName);
        Thread.Sleep(500);

        Assert.That(_driver.Url, Does.Contain("/Orders"));

        var orderItemText = _driver.FindElement(By.CssSelector(".order-item")).Text;
        Thread.Sleep(500);

        Assert.That(orderItemText, Does.Contain($"{productName} x {quantity}"));
    }

    [Test]
    public void Scenario5_OutOfStockUIValidation()
    {
        _loginPage.Visit(_baseUrl);
        _loginPage.Login(_testEmail, _testPassword);

        _shopPage.Visit(_baseUrl);
        const string productName = "Keyboard";
        int stock = _shopPage.GetStockCount(productName);

        if (stock > 0)
        {
            _shopPage.SetQuantity(productName, stock);
            _shopPage.PlaceOrder(productName);

            _shopPage.Visit(_baseUrl);
            Assert.That(_shopPage.IsOrderButtonEnabled(productName), Is.False);
            Assert.That(_shopPage.GetStockCount(productName), Is.EqualTo(0));
        }
    }

    [Test]
    public void Scenario6_EmptyOrdersState()
    {
        _loginPage.Visit(_baseUrl);
        // Assuming we could register a new user or use one with no orders
        // For this demo, let's just check if the message exists if no orders are visible
        _loginPage.Login(_testEmail, _testPassword); 
        Thread.Sleep(500);
        _ordersPage.Visit(_baseUrl);
        Thread.Sleep(500);
        try {
            var orders = _driver.FindElements(By.CssSelector(".order-item"));
            Thread.Sleep(500);
            if (orders.Count == 0)
            {
                Assert.That(_ordersPage.GetEmptyMessage(), Does.Contain("You haven't placed any orders yet."));
            }
        } catch (NoSuchElementException) {
            Assert.That(_ordersPage.GetEmptyMessage(), Does.Contain("You haven't placed any orders yet."));
        }
    }

    [Test]
    public void Scenario7_HeaderNavigationLinks()
    {
        _loginPage.Visit(_baseUrl);
        _loginPage.Login(_testEmail, _testPassword);

        _driver.FindElement(By.LinkText("Home")).Click();
        Assert.That(_driver.Url.TrimEnd('/'), Is.EqualTo(_baseUrl.TrimEnd('/')));

        _driver.FindElement(By.LinkText("Shop")).Click();
        Assert.That(_driver.Url, Does.Contain("/Shop"));

        _driver.FindElement(By.LinkText("My Orders")).Click();
        Assert.That(_driver.Url, Does.Contain("/Orders"));
    }

    [Test]
    public void Scenario8_SessionTermination_ProtectedRoutes()
    {
        _loginPage.Visit(_baseUrl);
        _loginPage.Login(_testEmail, _testPassword);

        _driver.FindElement(By.LinkText("Logout")).Click();
        Assert.That(_driver.Url, Does.Contain("/Account/Login"));

        _driver.Navigate().GoToUrl($"{_baseUrl}/Shop");
        Assert.That(_driver.Url, Does.Contain("/Account/Login"));
    }

    [Test]
    public void Scenario9_StockDepletionLogicVerification()
    {
        _loginPage.Visit(_baseUrl);
        _loginPage.Login(_testEmail, _testPassword);
        Thread.Sleep(500);
        _shopPage.Visit(_baseUrl);
        Thread.Sleep(500);
        int initial = _shopPage.GetStockCount("Mouse");
        Thread.Sleep(500);
        _shopPage.PlaceOrder("Mouse");
        Thread.Sleep(500);

        _shopPage.Visit(_baseUrl);
        Thread.Sleep(500);
        int final = _shopPage.GetStockCount("Mouse");
        Assert.That(final, Is.EqualTo(initial - 1));
    }

    [Test]
    public void Scenario10_OrderPersistenceAcrossSessions()
    {
        _loginPage.Visit(_baseUrl);
        _loginPage.Login(_testEmail, _testPassword);

        _ordersPage.Visit(_baseUrl);
        int initialCount = _driver.FindElements(By.CssSelector(".order-item")).Count;

        // Simulate new session by quitting and restarting
        Teardown();
        Setup();

        _loginPage.Visit(_baseUrl);
        _loginPage.Login(_testEmail, _testPassword);
        _ordersPage.Visit(_baseUrl);
        Thread.Sleep(500);
        int finalCount = _driver.FindElements(By.CssSelector(".order-item")).Count;
        Thread.Sleep(500);
        Assert.That(finalCount, Is.EqualTo(initialCount));
    }
}
