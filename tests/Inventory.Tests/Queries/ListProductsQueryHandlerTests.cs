using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Products.Models;
using Inventory.Application.Products.Queries;
using Inventory.Application.Products.Queries.Handlers;
using NSubstitute;

namespace Inventory.Tests.Queries;

public class ListProductsQueryHandlerTests
{
    private readonly IProductReadRepository _productReadRepository = Substitute.For<IProductReadRepository>();
    private readonly ListProductsQueryHandler _handler;

    public ListProductsQueryHandlerTests()
    {
        _handler = new ListProductsQueryHandler(_productReadRepository);
    }

    [Fact]
    public async Task HandleAsync_ProductsExist_ReturnsAll()
    {
        var products = new List<ProductResult>
        {
            new(1, "Laptop", 1, "Electronics", 999m, 5, DateTime.UtcNow, DateTime.UtcNow),
            new(2, "Mouse", 1, "Electronics", 29m, 100, DateTime.UtcNow, DateTime.UtcNow)
        };
        _productReadRepository.GetAllAsync().Returns(products);
        var result = await _handler.HandleAsync(new ListProductsQuery());
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task HandleAsync_NoProducts_ReturnsEmptyCollection()
    {
        _productReadRepository.GetAllAsync().Returns(new List<ProductResult>());
        var result = await _handler.HandleAsync(new ListProductsQuery());
        Assert.Empty(result);
    }
}
