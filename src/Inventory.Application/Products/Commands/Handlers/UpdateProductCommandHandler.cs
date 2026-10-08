using Inventory.Application.Abstractions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;

namespace Inventory.Application.Products.Commands.Handlers;

public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, bool>
{
    private readonly IProductWriteRepository _productWriteRepository;
    private readonly ICategoryWriteRepository _categoryWriteRepository;

    public UpdateProductCommandHandler(
        IProductWriteRepository productWriteRepository,
        ICategoryWriteRepository categoryWriteRepository)
    {
        _productWriteRepository = productWriteRepository;
        _categoryWriteRepository = categoryWriteRepository;
    }

    public async Task<bool> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        var productExists = await _productWriteRepository.ExistsAsync(command.Id, cancellationToken);
        if (!productExists)
            throw new NotFoundException("Product", command.Id);
        var categoryExists = await _categoryWriteRepository.ExistsAsync(command.CategoryId, cancellationToken);
        if (!categoryExists)
            throw new NotFoundException("Category", command.CategoryId);

        return await _productWriteRepository.UpdateAsync(command.Id, command.Name, command.CategoryId, command.Price, cancellationToken);
    }
}
