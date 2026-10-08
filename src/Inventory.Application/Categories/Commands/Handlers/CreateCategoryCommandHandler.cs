using Inventory.Application.Abstractions;
using Inventory.Application.Abstractions.Persistence;

namespace Inventory.Application.Categories.Commands.Handlers;

public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, int>
{
    private readonly ICategoryWriteRepository _categoryWriteRepository;

    public CreateCategoryCommandHandler(ICategoryWriteRepository categoryWriteRepository)
    {
        _categoryWriteRepository = categoryWriteRepository;
    }

    public async Task<int> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        return await _categoryWriteRepository.CreateAsync(command.Name, cancellationToken);
    }
}
