using Inventory.Api.Models.Requests;
using Inventory.Api.Models.Responses;
using Inventory.Application.Abstractions;
using Inventory.Application.Products.Commands;
using Inventory.Application.Products.Models;
using Inventory.Application.Products.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ICommandHandler<CreateProductCommand, int> _createHandler;
    private readonly ICommandHandler<UpdateProductCommand, bool> _updateHandler;
    private readonly ICommandHandler<DeleteProductCommand, bool> _deleteHandler;
    private readonly IQueryHandler<GetProductQuery, ProductResult> _getHandler;
    private readonly IQueryHandler<ListProductsQuery, IReadOnlyList<ProductResult>> _listHandler;

    public ProductsController(
        ICommandHandler<CreateProductCommand, int> createHandler,
        ICommandHandler<UpdateProductCommand, bool> updateHandler,
        ICommandHandler<DeleteProductCommand, bool> deleteHandler,
        IQueryHandler<GetProductQuery, ProductResult> getHandler,
        IQueryHandler<ListProductsQuery, IReadOnlyList<ProductResult>> listHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getHandler = getHandler;
        _listHandler = listHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var command = new CreateProductCommand(request.Name, request.CategoryId, request.Price);
        var id = await _createHandler.HandleAsync(command, ct);
        var product = await _getHandler.HandleAsync(new GetProductQuery(id), ct);

        return CreatedAtAction(nameof(GetById), new { id }, product);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var product = await _getHandler.HandleAsync(new GetProductQuery(id), ct);

        return Ok(product);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var products = await _listHandler.HandleAsync(new ListProductsQuery(), ct);

        return Ok(products);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var command = new UpdateProductCommand(id, request.Name, request.CategoryId, request.Price);
        await _updateHandler.HandleAsync(command, ct);
        var product = await _getHandler.HandleAsync(new GetProductQuery(id), ct);

        return Ok(product);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _deleteHandler.HandleAsync(new DeleteProductCommand(id), ct);

        return NoContent();
    }
}
