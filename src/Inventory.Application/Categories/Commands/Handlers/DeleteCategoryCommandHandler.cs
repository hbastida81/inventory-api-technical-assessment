using Inventory.Application.Abstractions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;

namespace Inventory.Application.Categories.Commands.Handlers;

public class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand, bool>
{
    private readonly ICategoryWriteRepository _categoryWriteRepository;

    public DeleteCategoryCommandHandler(ICategoryWriteRepository categoryWriteRepository)
    {
        _categoryWriteRepository = categoryWriteRepository;
    }

    public async Task<bool> HandleAsync(DeleteCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var exists = await _categoryWriteRepository.ExistsAsync(command.Id, cancellationToken);
        if (!exists)
            throw new NotFoundException("Category", command.Id);
        var hasProducts = await _categoryWriteRepository.HasProductsAsync(command.Id, cancellationToken);
        if (hasProducts)
            throw new ConflictException("Cannot delete category because it has associated products.");

        await _categoryWriteRepository.DeleteAsync(command.Id, cancellationToken);

        return true;
    }
}
