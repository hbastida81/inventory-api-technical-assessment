using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;
using Inventory.Application.Products.Commands;
using Inventory.Application.Products.Commands.Handlers;
using NSubstitute;

namespace Inventory.Tests.Commands;

public class DeleteProductCommandHandlerTests
{
    private readonly IProductWriteRepository _productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests()
    {
        _handler = new DeleteProductCommandHandler(_productWriteRepository);
    }

    [Fact]
    public async Task HandleAsync_ExistingProductNoMovements_CallsDelete()
    {
        var command = new DeleteProductCommand(1);
        _productWriteRepository.ExistsAsync(1).Returns(true);
        _productWriteRepository.HasMovementsAsync(1).Returns(false);
        _productWriteRepository.DeleteAsync(1).Returns(true);
        await _handler.HandleAsync(command);
        await _productWriteRepository.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NonexistentProduct_ThrowsNotFoundException()
    {
        var command = new DeleteProductCommand(99);
        _productWriteRepository.ExistsAsync(99).Returns(false);
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_ProductWithMovements_ThrowsConflictException()
    {
        var command = new DeleteProductCommand(1);
        _productWriteRepository.ExistsAsync(1).Returns(true);
        _productWriteRepository.HasMovementsAsync(1).Returns(true);
        await Assert.ThrowsAsync<ConflictException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_ProductWithMovements_DoesNotCallDelete()
    {
        var command = new DeleteProductCommand(1);
        _productWriteRepository.ExistsAsync(1).Returns(true);
        _productWriteRepository.HasMovementsAsync(1).Returns(true);
        try { await _handler.HandleAsync(command); } catch (ConflictException) { }
        await _productWriteRepository.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
