using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Commands;
using Inventory.Application.Categories.Commands.Handlers;
using Inventory.Application.Exceptions;
using NSubstitute;

namespace Inventory.Tests.Commands;

public class UpdateCategoryCommandHandlerTests
{
    private readonly ICategoryWriteRepository _categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly UpdateCategoryCommandHandler _handler;

    public UpdateCategoryCommandHandlerTests()
    {
        _handler = new UpdateCategoryCommandHandler(_categoryWriteRepository);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CallsRepository()
    {
        var command = new UpdateCategoryCommand(1, "Updated");
        _categoryWriteRepository.UpdateAsync(1, "Updated").Returns(true);
        await _handler.HandleAsync(command);
        await _categoryWriteRepository.Received(1).UpdateAsync(1, "Updated", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NonexistentCategory_ThrowsNotFoundException()
    {
        var command = new UpdateCategoryCommand(99, "Name");
        _categoryWriteRepository.UpdateAsync(99, "Name").Returns(false);
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
    }
}
