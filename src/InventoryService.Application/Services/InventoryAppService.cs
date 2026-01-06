using InventoryService.Application.DTOs;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;

namespace InventoryService.Application.Services;

/// <summary>
/// Application service implementing inventory use cases.
/// </summary>
public class InventoryAppService : IInventoryService
{
    private readonly IProductRepository _productRepository;

    public InventoryAppService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.Select(MapToDto);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product is null ? null : MapToDto(product);
    }

    public async Task<ReserveStockResponse> ReserveStockAsync(ReserveStockRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        
        if (product is null)
        {
            return new ReserveStockResponse(false, $"Product with ID {request.ProductId} not found");
        }

        try
        {
            product.ReserveStock(request.Quantity);
            await _productRepository.UpdateAsync(product, cancellationToken);
            return new ReserveStockResponse(true);
        }
        catch (InsufficientStockException ex)
        {
            return new ReserveStockResponse(false, ex.Message);
        }
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product(0, request.Name, request.Price, request.StockQuantity);
        var created = await _productRepository.AddAsync(product, cancellationToken);
        return MapToDto(created);
    }

    public async Task ReleaseStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        
        if (product is null)
        {
            throw new ArgumentException($"Product with ID {productId} not found");
        }

        product.ReleaseStock(quantity);
        await _productRepository.UpdateAsync(product, cancellationToken);
    }

    public async Task<ProductDto?> UpdateProductAsync(UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (product is null)
        {
            return null;
        }

        // Update only provided fields
        if (request.Name is not null)
        {
            product.UpdateName(request.Name);
        }

        if (request.Price.HasValue)
        {
            product.UpdatePrice(request.Price.Value);
        }

        if (request.StockQuantity.HasValue)
        {
            product.SetStockQuantity(request.StockQuantity.Value);
        }

        await _productRepository.UpdateAsync(product, cancellationToken);
        return MapToDto(product);
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Price,
            product.StockQuantity
        );
    }
}

