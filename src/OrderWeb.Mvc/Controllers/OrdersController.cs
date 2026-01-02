using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Claims;

namespace OrderWeb.Mvc.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public OrdersController(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        var token = User.FindFirstValue("Token");
        var orderServiceUrl = _config["Services:OrderServiceUrl"] ?? "http://localhost:5000";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orders = await _httpClient.GetFromJsonAsync<List<OrderViewModel>>($"{orderServiceUrl}/api/orders");
        return View(orders);
    }
}

public class OrderViewModel
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = new();
}

public class OrderItemViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
