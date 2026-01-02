using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Models;
using System.Security.Claims;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public OrdersController(OrderDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _context = context;
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        return await _context.Orders.Include(o => o.Items)
            .Where(o => o.UserEmail == email)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        
        // 1. Call InventoryService to reserve stock
        var inventoryUrl = _config["Services:InventoryUrl"] ?? "http://localhost:5001";
        
        foreach (var item in request.Items)
        {
            var response = await _httpClient.PostAsJsonAsync($"{inventoryUrl}/api/inventory/reserve", new { ProductId = item.ProductId, Quantity = item.Quantity });
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest($"Failed to reserve stock for product {item.ProductId}");
            }
        }

        // 2. Create Order
        var order = new Order
        {
            UserEmail = email ?? "unknown",
            OrderDate = DateTime.UtcNow,
            TotalAmount = request.Items.Sum(i => i.UnitPrice * i.Quantity),
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, order);
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "Healthy" });
}

public record CreateOrderRequest(List<CreateOrderItemRequest> Items);
public record CreateOrderItemRequest(int ProductId, string ProductName, int Quantity, decimal UnitPrice);
