using Inventory.Domain.Entities;
using Inventory.Domain.Enums;

namespace Inventory.Tests.Domain;

public class InventoryMovementTests
{
    [Fact]
    public void Constructor_ValidEntry_CreatesMovement()
    {
        var movement = new InventoryMovement(1, MovementType.Entry, 10);
        Assert.Equal(1, movement.ProductId);
        Assert.Equal(MovementType.Entry, movement.Type);
        Assert.Equal(10, movement.Quantity);
    }

    [Fact]
    public void Constructor_ValidExit_CreatesMovement()
    {
        var movement = new InventoryMovement(1, MovementType.Exit, 5);
        Assert.Equal(MovementType.Exit, movement.Type);
        Assert.Equal(5, movement.Quantity);
    }

    [Fact]
    public void Constructor_SetsCreatedAtToUtc()
    {
        var before = DateTime.UtcNow;
        var movement = new InventoryMovement(1, MovementType.Entry, 1);
        var after = DateTime.UtcNow;
        Assert.InRange(movement.CreatedAt, before, after);
        Assert.Equal(DateTimeKind.Utc, movement.CreatedAt.Kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_NonPositiveQuantity_ThrowsArgumentException(int quantity)
    {
        Assert.Throws<ArgumentException>(() => new InventoryMovement(1, MovementType.Entry, quantity));
    }

    [Fact]
    public void Properties_AreReadOnly_AfterCreation()
    {
        var movement = new InventoryMovement(1, MovementType.Entry, 10);
        var type = movement.GetType();
        Assert.False(type.GetProperty(nameof(InventoryMovement.ProductId))!.CanWrite);
        Assert.False(type.GetProperty(nameof(InventoryMovement.Type))!.CanWrite);
        Assert.False(type.GetProperty(nameof(InventoryMovement.Quantity))!.CanWrite);
        Assert.False(type.GetProperty(nameof(InventoryMovement.CreatedAt))!.CanWrite);
    }
}
