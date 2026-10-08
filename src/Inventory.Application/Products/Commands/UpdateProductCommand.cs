namespace Inventory.Application.Products.Commands;

public record UpdateProductCommand(int Id, string Name, int CategoryId, decimal Price);
