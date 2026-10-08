using Inventory.Application.Abstractions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;

namespace Inventory.Application.Products.Commands.Handlers;

public class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, bool>
{
    private readonly IProductWriteRepository _productWriteRepository;

    public DeleteProductCommandHandler(IProductWriteRepository productWriteRepository)
    {
        _productWriteRepository = productWriteRepository;
    }

    public async Task<bool> HandleAsync(DeleteProductCommand command, CancellationToken cancellationToken = default)
    {
        var exists = await _productWriteRepository.ExistsAsync(command.Id, cancellationToken);
        if (!exists)
            throw new NotFoundException("Product", command.Id);
        var hasMovements = await _productWriteRepository.HasMovementsAsync(command.Id, cancellationToken);
        if (hasMovements)
            throw new ConflictException("Cannot delete product because it has inventory movement history.");
        await _productWriteRepository.DeleteAsync(command.Id, cancellationToken);

        return true;
    }
}
