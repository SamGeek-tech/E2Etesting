using System.Net;
using FluentAssertions;
using InventoryService.Contract.Tests.Sdk;
using PactNet;
using PactNet.Matchers;
using PactNet.Output.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace InventoryService.Contract.Tests;

public class InventoryClientTests
{
    private readonly IPactBuilderV4 _pactBuilder;

    public InventoryClientTests(ITestOutputHelper output)
    {
        var config = new PactConfig
        {
            PactDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "pacts"),
            Outputters = new[] { new XunitOutput(output) },
            LogLevel = PactLogLevel.Information
        };

        var pact = Pact.V4("InventoryClientSdk", "InventoryService", config);
        _pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task GetProducts_ReturnsListOfProducts()
    {
        // Arrange
        _pactBuilder
            .UponReceiving("A request to get all products")
                .Given("Products exist")
                .WithRequest(HttpMethod.Get, "/api/inventory")
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(Match.MinType(new
                {
                    id = Match.Integer(1),
                    name = Match.Type("Product Name"),
                    price = Match.Decimal(10.0m),
                    stockQuantity = Match.Integer(100)
                }, 1));

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new InventoryClient(new HttpClient { BaseAddress = ctx.MockServerUri });
            var products = await client.GetProductsAsync();

            products.Should().NotBeNull();
            products.Should().HaveCountGreaterThanOrEqualTo(1);
            var first = products!.First();
            first.Id.Should().BeGreaterThan(0);
            first.Name.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetProduct_WithExistingId_ReturnsProduct()
    {
        // Arrange
        _pactBuilder
            .UponReceiving("A request to get a specific product")
                .Given("Product 1 exists")
                .WithRequest(HttpMethod.Get, "/api/inventory/1")
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = 1,
                    name = Match.Type("Product Name"),
                    price = Match.Decimal(10.0m),
                    stockQuantity = Match.Integer(100)
                });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new InventoryClient(new HttpClient { BaseAddress = ctx.MockServerUri });
            var product = await client.GetProductAsync(1);

            product.Should().NotBeNull();
            product!.Id.Should().Be(1);
        });
    }

    [Fact]
    public async Task ReserveStock_WithValidRequest_ReturnsOk()
    {
        // Arrange
        _pactBuilder
            .UponReceiving("A request to reserve stock")
                .Given("Product 1 has sufficient stock")
                .WithRequest(HttpMethod.Post, "/api/inventory/reserve")
                .WithJsonBody(new
                {
                    productId = 1,
                    quantity = 1
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.OK);

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new InventoryClient(new HttpClient { BaseAddress = ctx.MockServerUri });
            await client.ReserveStockAsync(new ReserveStockRequest(1, 1));
        });
    }

    [Fact]
    public async Task CreateProduct_WithValidRequest_ReturnsCreatedProduct()
    {
        // Arrange
        _pactBuilder
            .UponReceiving("A request to create a product")
                .WithRequest(HttpMethod.Post, "/api/inventory")
                .WithJsonBody(new
                {
                    name = "New Product",
                    price = 99.99m,
                    stockQuantity = 50
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.Created)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = Match.Integer(1),
                    name = "New Product",
                    price = 99.99m,
                    stockQuantity = 50
                });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new InventoryClient(new HttpClient { BaseAddress = ctx.MockServerUri });
            var product = await client.CreateProductAsync(new CreateProductRequest("New Product", 99.99m, 50));

            product.Should().NotBeNull();
            product!.Name.Should().Be("New Product");
        });
    }

    [Fact]
    public async Task DeleteInventoryItem_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        _pactBuilder
            .UponReceiving("A request to delete an inventory item")
                .Given("Inventory item 1 exists")
                .WithRequest(HttpMethod.Delete, "/api/inventory/1")
            .WillRespond()
                .WithStatus(HttpStatusCode.NoContent);

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new InventoryClient(new HttpClient { BaseAddress = ctx.MockServerUri });
            var result = await client.DeleteInventoryItemAsync(1);
            result.Should().BeTrue();
        });
    }
}
