using Inventory.Application.Abstractions;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;
using Inventory.Application.Products.Models;

namespace Inventory.Application.Products.Queries.Handlers;

public class GetProductQueryHandler : IQueryHandler<GetProductQuery, ProductResult>
{
    private readonly IProductReadRepository _productReadRepository;

    public GetProductQueryHandler(IProductReadRepository productReadRepository)
    {
        _productReadRepository = productReadRepository;
    }

    public async Task<ProductResult> HandleAsync(GetProductQuery query, CancellationToken cancellationToken = default)
    {
        var product = await _productReadRepository.GetByIdAsync(query.Id, cancellationToken);
        if (product is null)
            throw new NotFoundException("Product", query.Id);

        return product;
    }
}
