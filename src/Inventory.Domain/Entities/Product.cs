namespace Inventory.Domain.Entities;

public class Product
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public int CategoryId { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Product(string name, int categoryId, decimal price)
    {
        ValidateName(name);
        ValidatePrice(price);
        Name = name;
        CategoryId = categoryId;
        Price = price;
        Stock = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    private Product() { Name = string.Empty; }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        Stock += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (quantity > Stock)
            throw new InvalidOperationException("Insufficient stock for the requested quantity.");
        Stock -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, int categoryId, decimal price)
    {
        ValidateName(name);
        ValidatePrice(price);
        Name = name;
        CategoryId = categoryId;
        Price = price;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("Name must not exceed 200 characters.", nameof(name));
    }

    private static void ValidatePrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentException("Price must be greater than or equal to zero.", nameof(price));
    }
}
