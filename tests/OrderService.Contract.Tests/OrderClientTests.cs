using System.Net;
using System.Threading;
using FluentAssertions;
using OrderService.Contract.Tests.Sdk;
using PactNet;
using PactNet.Matchers;
using PactNet.Output.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace OrderService.Contract.Tests;

public class OrderClientTests
{
    private readonly IPactBuilderV4 _pactBuilder;
    private static int _pactFileCleaned;

    public OrderClientTests(ITestOutputHelper output)
    {
        // Clean pact file once per test run to avoid stale/duplicate interactions
        // (PactNet merges interactions into existing pact files; it does not delete old interactions automatically)
        if (Interlocked.Exchange(ref _pactFileCleaned, 1) == 0)
        {
            var pactDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "pacts");
            var pactFile = Path.Combine(pactDir, "OrderClientSdk-OrderServiceApi.json");
            if (File.Exists(pactFile))
            {
                File.Delete(pactFile);
            }
        }

        var config = new PactConfig
        {
            PactDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "pacts"),
            Outputters = new[] { new XunitOutput(output) },
            LogLevel = PactLogLevel.Information
        };

        var pact = Pact.V4("OrderClientSdk", "OrderServiceApi", config);
        _pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task GetOrders_ReturnsListOfOrders()
    {
        // Arrange
        _pactBuilder
            .UponReceiving("A request to get all orders")
                .Given("Orders exist for user")
                .WithRequest(HttpMethod.Get, "/api/orders")
                .WithHeader("Authorization", Match.Regex("Bearer .*", "Bearer test-token"))
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(Match.MinType(new
                {
                    id = Match.Integer(1),
                    userEmail = Match.Type("user@example.com"),
                    orderDate = Match.Type("2024-01-01T12:00:00Z"),
                    totalAmount = Match.Decimal(100.0m),
                    status = Match.Type("Pending"),
                    items = Match.MinType(new
                    {
                        id = Match.Integer(1),
                        productId = Match.Integer(1),
                        productName = Match.Type("Product"),
                        quantity = Match.Integer(1),
                        unitPrice = Match.Decimal(100.0m),
                        totalPrice = Match.Decimal(100.0m)
                    }, 1)
                }, 1));

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new OrderClient(new HttpClient { BaseAddress = ctx.MockServerUri }, "test-token");
            var orders = await client.GetOrdersAsync();

            orders.Should().NotBeNull();
            orders.Should().HaveCountGreaterThanOrEqualTo(1);
            var first = orders!.First();
            first.Id.Should().BeGreaterThan(0);
            first.UserEmail.Should().NotBeNullOrEmpty();
            first.Items.Should().HaveCountGreaterThanOrEqualTo(1);
        });
    }

    [Fact]
    public async Task GetOrder_WithExistingId_ReturnsOrder()
    {
        // Arrange
        _pactBuilder
            .UponReceiving("A request to get a specific order")
                .Given("Order 1 exists for user")
                .WithRequest(HttpMethod.Get, "/api/orders/1")
                .WithHeader("Authorization", Match.Regex("Bearer .*", "Bearer test-token"))
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = 1,
                    userEmail = Match.Type("user@example.com"),
                    orderDate = Match.Type("2024-01-01T12:00:00Z"),
                    totalAmount = Match.Decimal(100.0m),
                    status = Match.Type("Pending"),
                    items = Match.MinType(new
                    {
                        id = Match.Integer(1),
                        productId = Match.Integer(1),
                        productName = Match.Type("Product"),
                        quantity = Match.Integer(1),
                        unitPrice = Match.Decimal(100.0m),
                        totalPrice = Match.Decimal(100.0m)
                    }, 1)
                });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new OrderClient(new HttpClient { BaseAddress = ctx.MockServerUri }, "test-token");
            var order = await client.GetOrderAsync(1);

            order.Should().NotBeNull();
            order!.Id.Should().Be(1);
            order.Items.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task CreateOrder_WithValidRequest_ReturnsCreatedOrder()
    {
        // Arrange
        _pactBuilder
            .UponReceiving("A request to create an order")
                .Given("Inventory is available for products")
                .WithRequest(HttpMethod.Post, "/api/orders")
                .WithHeader("Authorization", Match.Regex("Bearer .*", "Bearer test-token"))
                .WithJsonBody(new
                {
                    items = Match.MinType(new
                    {
                        productId = Match.Integer(1),
                        productName = Match.Type("Product"),
                        quantity = Match.Integer(1),
                        unitPrice = Match.Decimal(10.0m)
                    }, 1)
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.Created)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = Match.Integer(1),
                    userEmail = Match.Type("user@example.com"),
                    orderDate = Match.Type("2024-01-01T12:00:00Z"),
                    totalAmount = Match.Decimal(10.0m),
                    status = Match.Type("Pending"),
                    items = Match.MinType(new
                    {
                        id = Match.Integer(1),
                        productId = 1,
                        productName = "Product",
                        quantity = 1,
                        unitPrice = 10.0m,
                        totalPrice = 10.0m
                    }, 1)
                });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new OrderClient(new HttpClient { BaseAddress = ctx.MockServerUri }, "test-token");
            
            var request = new CreateOrderRequest(
                Items: new List<CreateOrderItemRequest>
                {
                    new CreateOrderItemRequest(1, "Product", 1, 10.0m)
                }
            );
            
            var order = await client.CreateOrderAsync(request);

            order.Should().NotBeNull();
            order!.Items.Should().HaveCount(1);
            order.TotalAmount.Should().Be(10.0m);
        });
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Arrange
        _pactBuilder
            .UponReceiving("A request to health check")
                .WithRequest(HttpMethod.Get, "/api/orders/health")
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    status = "Healthy"
                });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new OrderClient(new HttpClient { BaseAddress = ctx.MockServerUri });
            var status = await client.HealthCheckAsync();

            status.Should().Be("Healthy");
        });
    }
}
