using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;
using Inventory.Application.InventoryMovements.Commands;
using Inventory.Application.InventoryMovements.Commands.Handlers;
using Inventory.Application.InventoryMovements.Models;
using Inventory.Domain.Enums;
using NSubstitute;

namespace Inventory.Tests.Commands;

public class RegisterMovementCommandHandlerTests
{
    private readonly IProductWriteRepository _productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly IMovementWriteRepository _movementWriteRepository = Substitute.For<IMovementWriteRepository>();
    private readonly RegisterMovementCommandHandler _handler;

    public RegisterMovementCommandHandlerTests()
    {
        _handler = new RegisterMovementCommandHandler(_movementWriteRepository, _productWriteRepository);
    }

    [Fact]
    public async Task HandleAsync_ValidEntry_ReturnsMovementResult()
    {
        var command = new RegisterMovementCommand(1, MovementType.Entry, 10);
        _productWriteRepository.ExistsAsync(1).Returns(true);
        var expected = new MovementResult(1, 1, "Entry", 10, DateTime.UtcNow);
        _movementWriteRepository.RegisterEntryAsync(1, 10).Returns(expected);
        var result = await _handler.HandleAsync(command);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleAsync_ValidExit_ReturnsMovementResult()
    {
        var command = new RegisterMovementCommand(1, MovementType.Exit, 5);
        _productWriteRepository.ExistsAsync(1).Returns(true);
        var expected = new MovementResult(2, 1, "Exit", 5, DateTime.UtcNow);
        _movementWriteRepository.RegisterExitAsync(1, 5).Returns(expected);
        var result = await _handler.HandleAsync(command);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleAsync_NonexistentProduct_ThrowsNotFoundException()
    {
        var command = new RegisterMovementCommand(99, MovementType.Entry, 5);
        _productWriteRepository.ExistsAsync(99).Returns(false);
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_ExitInsufficientStock_ThrowsConflictException()
    {
        var command = new RegisterMovementCommand(1, MovementType.Exit, 100);
        _productWriteRepository.ExistsAsync(1).Returns(true);
        _movementWriteRepository.RegisterExitAsync(1, 100)
            .Returns<MovementResult>(x => throw new ConflictException("Insufficient stock for the requested exit quantity."));
        await Assert.ThrowsAsync<ConflictException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_Entry_CallsRegisterEntry()
    {
        var command = new RegisterMovementCommand(1, MovementType.Entry, 10);
        _productWriteRepository.ExistsAsync(1).Returns(true);
        _movementWriteRepository.RegisterEntryAsync(1, 10)
            .Returns(new MovementResult(1, 1, "Entry", 10, DateTime.UtcNow));
        await _handler.HandleAsync(command);
        await _movementWriteRepository.Received(1).RegisterEntryAsync(1, 10, Arg.Any<CancellationToken>());
        await _movementWriteRepository.DidNotReceive().RegisterExitAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Exit_CallsRegisterExit()
    {
        var command = new RegisterMovementCommand(1, MovementType.Exit, 5);
        _productWriteRepository.ExistsAsync(1).Returns(true);
        _movementWriteRepository.RegisterExitAsync(1, 5)
            .Returns(new MovementResult(1, 1, "Exit", 5, DateTime.UtcNow));
        await _handler.HandleAsync(command);
        await _movementWriteRepository.Received(1).RegisterExitAsync(1, 5, Arg.Any<CancellationToken>());
        await _movementWriteRepository.DidNotReceive().RegisterEntryAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
