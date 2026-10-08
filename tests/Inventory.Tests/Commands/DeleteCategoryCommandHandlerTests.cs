using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Commands;
using Inventory.Application.Categories.Commands.Handlers;
using Inventory.Application.Exceptions;
using NSubstitute;

namespace Inventory.Tests.Commands;

public class DeleteCategoryCommandHandlerTests
{
    private readonly ICategoryWriteRepository _categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly DeleteCategoryCommandHandler _handler;

    public DeleteCategoryCommandHandlerTests()
    {
        _handler = new DeleteCategoryCommandHandler(_categoryWriteRepository);
    }

    [Fact]
    public async Task HandleAsync_CategoryWithNoProducts_CallsDelete()
    {
        var command = new DeleteCategoryCommand(1);
        _categoryWriteRepository.ExistsAsync(1).Returns(true);
        _categoryWriteRepository.HasProductsAsync(1).Returns(false);
        _categoryWriteRepository.DeleteAsync(1).Returns(true);
        await _handler.HandleAsync(command);
        await _categoryWriteRepository.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NonexistentCategory_ThrowsNotFoundException()
    {
        var command = new DeleteCategoryCommand(99);
        _categoryWriteRepository.ExistsAsync(99).Returns(false);
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_CategoryWithProducts_ThrowsConflictException()
    {
        var command = new DeleteCategoryCommand(1);
        _categoryWriteRepository.ExistsAsync(1).Returns(true);
        _categoryWriteRepository.HasProductsAsync(1).Returns(true);
        await Assert.ThrowsAsync<ConflictException>(() => _handler.HandleAsync(command));
    }
}
