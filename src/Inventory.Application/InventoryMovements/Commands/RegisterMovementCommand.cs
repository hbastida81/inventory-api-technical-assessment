using Inventory.Domain.Enums;

namespace Inventory.Application.InventoryMovements.Commands;

public record RegisterMovementCommand(int ProductId, MovementType Type, int Quantity);
