using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace OrderWeb.Mvc.Controllers;

[Authorize]
public class ShopController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public ShopController(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        var inventoryUrl = _config["Services:InventoryUrl"] ?? "http://127.0.0.1:5001";
        var products = await _httpClient.GetFromJsonAsync<List<ProductViewModel>>($"{inventoryUrl}/api/inventory");
        return View(products);
    }

    [HttpPost]
    public async Task<IActionResult> Order(int productId, string productName, decimal price, int quantity)
    {
        var token = User.FindFirstValue("Token");
        var orderServiceUrl = _config["Services:OrderServiceUrl"] ?? "http://127.0.0.1:5000";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Items = new[]
            {
                new { ProductId = productId, ProductName = productName, Quantity = quantity, UnitPrice = price }
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"{orderServiceUrl}/api/orders", request);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("Index", "Orders");
        }

        TempData["Error"] = "Failed to place order.";
        return RedirectToAction("Index");
    }
}

public class ProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}
