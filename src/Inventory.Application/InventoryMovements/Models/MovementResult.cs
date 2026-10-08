namespace Inventory.Application.InventoryMovements.Models;

public record MovementResult(
    int Id,
    int ProductId,
    string Type,
    int Quantity,
    DateTime CreatedAt);
