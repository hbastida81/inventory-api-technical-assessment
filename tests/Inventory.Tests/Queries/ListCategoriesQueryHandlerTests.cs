using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Models;
using Inventory.Application.Categories.Queries;
using Inventory.Application.Categories.Queries.Handlers;
using NSubstitute;

namespace Inventory.Tests.Queries;

public class ListCategoriesQueryHandlerTests
{
    private readonly ICategoryReadRepository _categoryReadRepository = Substitute.For<ICategoryReadRepository>();
    private readonly ListCategoriesQueryHandler _handler;

    public ListCategoriesQueryHandlerTests()
    {
        _handler = new ListCategoriesQueryHandler(_categoryReadRepository);
    }

    [Fact]
    public async Task HandleAsync_CategoriesExist_ReturnsAll()
    {
        var categories = new List<CategoryResult>
        {
            new(1, "Electronics", DateTime.UtcNow, DateTime.UtcNow),
            new(2, "Books", DateTime.UtcNow, DateTime.UtcNow)
        };
        _categoryReadRepository.GetAllAsync().Returns(categories);
        var result = await _handler.HandleAsync(new ListCategoriesQuery());
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task HandleAsync_NoCategories_ReturnsEmptyCollection()
    {
        _categoryReadRepository.GetAllAsync().Returns(new List<CategoryResult>());
        var result = await _handler.HandleAsync(new ListCategoriesQuery());
        Assert.Empty(result);
    }
}
