using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public class InventoryMovement
{
    public int Id { get; private set; }
    public int ProductId { get; }
    public MovementType Type { get; }
    public int Quantity { get; }
    public DateTime CreatedAt { get; }

    public InventoryMovement(int productId, MovementType type, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be a positive integer.", nameof(quantity));
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        CreatedAt = DateTime.UtcNow;
    }

    private InventoryMovement() { }
}
