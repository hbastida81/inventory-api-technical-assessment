using Inventory.Application.InventoryMovements.Models;
using Inventory.Domain.Enums;

namespace Inventory.Application.Abstractions.Persistence;

public interface IMovementWriteRepository
{
    Task<MovementResult> RegisterEntryAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    Task<MovementResult> RegisterExitAsync(int productId, int quantity, CancellationToken cancellationToken = default);
}
