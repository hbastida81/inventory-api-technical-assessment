using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Commands;
using Inventory.Application.Categories.Commands.Handlers;
using NSubstitute;

namespace Inventory.Tests.Commands;

public class CreateCategoryCommandHandlerTests
{
    private readonly ICategoryWriteRepository _categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly CreateCategoryCommandHandler _handler;

    public CreateCategoryCommandHandlerTests()
    {
        _handler = new CreateCategoryCommandHandler(_categoryWriteRepository);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsCreatedId()
    {
        var command = new CreateCategoryCommand("Electronics");
        _categoryWriteRepository.CreateAsync("Electronics").Returns(1);
        var result = await _handler.HandleAsync(command);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CallsRepositoryOnce()
    {
        var command = new CreateCategoryCommand("Books");
        _categoryWriteRepository.CreateAsync("Books").Returns(2);
        await _handler.HandleAsync(command);
        await _categoryWriteRepository.Received(1).CreateAsync("Books", Arg.Any<CancellationToken>());
    }
}
