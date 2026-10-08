using Inventory.Application.Abstractions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Models;

namespace Inventory.Application.Categories.Queries.Handlers;

public class ListCategoriesQueryHandler : IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResult>>
{
    private readonly ICategoryReadRepository _categoryReadRepository;

    public ListCategoriesQueryHandler(ICategoryReadRepository categoryReadRepository)
    {
        _categoryReadRepository = categoryReadRepository;
    }

    public async Task<IReadOnlyList<CategoryResult>> HandleAsync(ListCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        return await _categoryReadRepository.GetAllAsync(cancellationToken);
    }
}
