using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;
using Inventory.Application.Products.Commands;
using Inventory.Application.Products.Commands.Handlers;
using NSubstitute;

namespace Inventory.Tests.Commands;

public class CreateProductCommandHandlerTests
{
    private readonly ICategoryWriteRepository _categoryWriteRepository = Substitute.For<ICategoryWriteRepository>();
    private readonly IProductWriteRepository _productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(_productWriteRepository, _categoryWriteRepository);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsCreatedId()
    {
        var command = new CreateProductCommand("Laptop", 1, 999.99m);
        _categoryWriteRepository.ExistsAsync(1).Returns(true);
        _productWriteRepository.CreateAsync("Laptop", 1, 999.99m).Returns(42);
        var result = await _handler.HandleAsync(command);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task HandleAsync_NonexistentCategory_ThrowsNotFoundException()
    {
        var command = new CreateProductCommand("Laptop", 99, 999.99m);
        _categoryWriteRepository.ExistsAsync(99).Returns(false);
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CallsRepositoryOnce()
    {
        var command = new CreateProductCommand("Laptop", 1, 100m);
        _categoryWriteRepository.ExistsAsync(1).Returns(true);
        _productWriteRepository.CreateAsync("Laptop", 1, 100m).Returns(1);
        await _handler.HandleAsync(command);
        await _productWriteRepository.Received(1).CreateAsync("Laptop", 1, 100m, Arg.Any<CancellationToken>());
    }
}
