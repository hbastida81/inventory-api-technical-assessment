using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;
using Inventory.Application.Products.Models;
using Inventory.Application.Products.Queries;
using Inventory.Application.Products.Queries.Handlers;
using NSubstitute;

namespace Inventory.Tests.Queries;

public class GetProductQueryHandlerTests
{
    private readonly IProductReadRepository _productReadRepository = Substitute.For<IProductReadRepository>();
    private readonly GetProductQueryHandler _handler;

    public GetProductQueryHandlerTests()
    {
        _handler = new GetProductQueryHandler(_productReadRepository);
    }

    [Fact]
    public async Task HandleAsync_ExistingProduct_ReturnsProductResult()
    {
        var expected = new ProductResult(1, "Laptop", 1, "Electronics", 999.99m, 10, DateTime.UtcNow, DateTime.UtcNow);
        _productReadRepository.GetByIdAsync(1).Returns(expected);
        var result = await _handler.HandleAsync(new GetProductQuery(1));
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleAsync_NonexistentProduct_ThrowsNotFoundException()
    {
        _productReadRepository.GetByIdAsync(99).Returns((ProductResult?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(new GetProductQuery(99)));
    }
}
