namespace InventoryService.Domain.Entities;

/// <summary>
/// Represents a product in the inventory system.
/// </summary>
public class Product
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }

    // EF Core requires parameterless constructor
    private Product() { }

    public Product(int id, string name, decimal price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));

        Id = id;
        Name = name;
        Price = price;
        StockQuantity = stockQuantity;
    }

    /// <summary>
    /// Reserves stock for an order. Throws if insufficient stock.
    /// </summary>
    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        
        if (StockQuantity < quantity)
            throw new InsufficientStockException(Id, Name, StockQuantity, quantity);

        StockQuantity -= quantity;
    }

    /// <summary>
    /// Releases previously reserved stock back to inventory.
    /// </summary>
    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        StockQuantity += quantity;
    }

    /// <summary>
    /// Updates the product price.
    /// </summary>
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Price cannot be negative", nameof(newPrice));

        Price = newPrice;
    }

    /// <summary>
    /// Updates the product name.
    /// </summary>
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Product name cannot be empty", nameof(newName));

        Name = newName;
    }

    /// <summary>
    /// Sets the stock quantity directly (for adjustments).
    /// </summary>
    public void SetStockQuantity(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(quantity));

        StockQuantity = quantity;
    }

    /// <summary>
    /// Adds stock to the inventory.
    /// </summary>
    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        StockQuantity += quantity;
    }
}

/// <summary>
/// Exception thrown when attempting to reserve more stock than available.
/// </summary>
public class InsufficientStockException : Exception
{
    public int ProductId { get; }
    public string ProductName { get; }
    public int AvailableStock { get; }
    public int RequestedQuantity { get; }

    public InsufficientStockException(int productId, string productName, int availableStock, int requestedQuantity)
        : base($"Insufficient stock for product '{productName}' (ID: {productId}). Available: {availableStock}, Requested: {requestedQuantity}")
    {
        ProductId = productId;
        ProductName = productName;
        AvailableStock = availableStock;
        RequestedQuantity = requestedQuantity;
    }
}

