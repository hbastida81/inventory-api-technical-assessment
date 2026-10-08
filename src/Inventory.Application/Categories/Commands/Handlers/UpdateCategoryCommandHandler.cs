using Inventory.Application.Abstractions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;

namespace Inventory.Application.Categories.Commands.Handlers;

public class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, bool>
{
    private readonly ICategoryWriteRepository _categoryWriteRepository;

    public UpdateCategoryCommandHandler(ICategoryWriteRepository categoryWriteRepository)
    {
        _categoryWriteRepository = categoryWriteRepository;
    }

    public async Task<bool> HandleAsync(UpdateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var updated = await _categoryWriteRepository.UpdateAsync(command.Id, command.Name, cancellationToken);
        if (!updated)
            throw new NotFoundException("Category", command.Id);

        return true;
    }
}
