using Inventory.Application.Abstractions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;
using Inventory.Application.InventoryMovements.Models;
using Inventory.Domain.Enums;

namespace Inventory.Application.InventoryMovements.Commands.Handlers;

public class RegisterMovementCommandHandler : ICommandHandler<RegisterMovementCommand, MovementResult>
{
    private readonly IMovementWriteRepository _movementWriteRepository;
    private readonly IProductWriteRepository _productWriteRepository;

    public RegisterMovementCommandHandler(
        IMovementWriteRepository movementWriteRepository,
        IProductWriteRepository productWriteRepository)
    {
        _movementWriteRepository = movementWriteRepository;
        _productWriteRepository = productWriteRepository;
    }

    public async Task<MovementResult> HandleAsync(RegisterMovementCommand command, CancellationToken cancellationToken = default)
    {
        var productExists = await _productWriteRepository.ExistsAsync(command.ProductId, cancellationToken);
        if (!productExists)
            throw new NotFoundException("Product", command.ProductId);

        return command.Type switch
        {
            MovementType.Entry => await _movementWriteRepository.RegisterEntryAsync(command.ProductId, command.Quantity, cancellationToken),
            MovementType.Exit => await _movementWriteRepository.RegisterExitAsync(command.ProductId, command.Quantity, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(command.Type), "Invalid movement type.")
        };
    }
}
