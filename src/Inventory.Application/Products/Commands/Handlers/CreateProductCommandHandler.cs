using Inventory.Application.Abstractions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;

namespace Inventory.Application.Products.Commands.Handlers;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, int>
{
    private readonly IProductWriteRepository _productWriteRepository;
    private readonly ICategoryWriteRepository _categoryWriteRepository;

    public CreateProductCommandHandler(
        IProductWriteRepository productWriteRepository,
        ICategoryWriteRepository categoryWriteRepository)
    {
        _productWriteRepository = productWriteRepository;
        _categoryWriteRepository = categoryWriteRepository;
    }

    public async Task<int> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        var categoryExists = await _categoryWriteRepository.ExistsAsync(command.CategoryId, cancellationToken);
        if (!categoryExists)
            throw new NotFoundException("Category", command.CategoryId);

        return await _productWriteRepository.CreateAsync(command.Name, command.CategoryId, command.Price, cancellationToken);
    }
}
