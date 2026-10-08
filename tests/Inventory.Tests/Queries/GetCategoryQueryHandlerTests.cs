using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Models;
using Inventory.Application.Categories.Queries;
using Inventory.Application.Categories.Queries.Handlers;
using Inventory.Application.Exceptions;
using NSubstitute;

namespace Inventory.Tests.Queries;

public class GetCategoryQueryHandlerTests
{
    private readonly ICategoryReadRepository _categoryReadRepository = Substitute.For<ICategoryReadRepository>();
    private readonly GetCategoryQueryHandler _handler;

    public GetCategoryQueryHandlerTests()
    {
        _handler = new GetCategoryQueryHandler(_categoryReadRepository);
    }

    [Fact]
    public async Task HandleAsync_ExistingCategory_ReturnsCategoryResult()
    {
        var expected = new CategoryResult(1, "Electronics", DateTime.UtcNow, DateTime.UtcNow);
        _categoryReadRepository.GetByIdAsync(1).Returns(expected);
        var result = await _handler.HandleAsync(new GetCategoryQuery(1));
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleAsync_NonexistentCategory_ThrowsNotFoundException()
    {
        _categoryReadRepository.GetByIdAsync(99).Returns((CategoryResult?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(new GetCategoryQuery(99)));
    }
}
