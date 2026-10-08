namespace Inventory.Application.Products.Models;

public record ProductResult(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    DateTime UpdatedAt);
