using Inventory.Application.Abstractions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Products.Models;

namespace Inventory.Application.Products.Queries.Handlers;

public class ListProductsQueryHandler : IQueryHandler<ListProductsQuery, IReadOnlyList<ProductResult>>
{
    private readonly IProductReadRepository _productReadRepository;

    public ListProductsQueryHandler(IProductReadRepository productReadRepository)
    {
        _productReadRepository = productReadRepository;
    }

    public async Task<IReadOnlyList<ProductResult>> HandleAsync(ListProductsQuery query, CancellationToken cancellationToken = default)
    {
        return await _productReadRepository.GetAllAsync(cancellationToken);
    }
}
