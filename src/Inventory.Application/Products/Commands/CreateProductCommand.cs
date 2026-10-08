namespace Inventory.Application.Products.Commands;

public record CreateProductCommand(string Name, int CategoryId, decimal Price);
