using Inventory.Application.Abstractions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Models;
using Inventory.Application.Exceptions;

namespace Inventory.Application.Categories.Queries.Handlers;

public class GetCategoryQueryHandler : IQueryHandler<GetCategoryQuery, CategoryResult>
{
    private readonly ICategoryReadRepository _categoryReadRepository;

    public GetCategoryQueryHandler(ICategoryReadRepository categoryReadRepository)
    {
        _categoryReadRepository = categoryReadRepository;
    }

    public async Task<CategoryResult> HandleAsync(GetCategoryQuery query, CancellationToken cancellationToken = default)
    {
        var category = await _categoryReadRepository.GetByIdAsync(query.Id, cancellationToken);
        if (category is null)
            throw new NotFoundException("Category", query.Id);

        return category;
    }
}
