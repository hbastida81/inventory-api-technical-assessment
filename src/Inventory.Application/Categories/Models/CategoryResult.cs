namespace Inventory.Application.Categories.Models;

public record CategoryResult(
    int Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt);
