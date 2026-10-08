using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;
using Inventory.Application.Products.Commands;
using Inventory.Application.Products.Commands.Handlers;
using NSubstitute;

namespace Inventory.Tests.Commands;

public class UpdateProductCommandHandlerTests
{
    private readonly IProductWriteRepository _productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly ICategoryWriteRepository _categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests()
    {
        _handler = new UpdateProductCommandHandler(_productWriteRepository, _categoryWriteRepository);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CallsRepository()
    {
        var command = new UpdateProductCommand(1, "Updated", 2, 50m);
        _productWriteRepository.ExistsAsync(1).Returns(true);
        _categoryWriteRepository.ExistsAsync(2).Returns(true);
        _productWriteRepository.UpdateAsync(1, "Updated", 2, 50m).Returns(true);
        await _handler.HandleAsync(command);
        await _productWriteRepository.Received(1).UpdateAsync(1, "Updated", 2, 50m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NonexistentProduct_ThrowsNotFoundException()
    {
        var command = new UpdateProductCommand(99, "Name", 1, 10m);
        _productWriteRepository.ExistsAsync(99).Returns(false);
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_NonexistentCategory_ThrowsNotFoundException()
    {
        var command = new UpdateProductCommand(1, "Name", 99, 10m);
        _productWriteRepository.ExistsAsync(1).Returns(true);
        _categoryWriteRepository.ExistsAsync(99).Returns(false);
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
    }
}
