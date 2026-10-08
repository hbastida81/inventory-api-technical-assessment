using Inventory.Api.Models.Requests;
using Inventory.Api.Models.Responses;
using Inventory.Application.Abstractions;
using Inventory.Application.Categories.Commands;
using Inventory.Application.Categories.Models;
using Inventory.Application.Categories.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICommandHandler<CreateCategoryCommand, int> _createHandler;
    private readonly ICommandHandler<UpdateCategoryCommand, bool> _updateHandler;
    private readonly ICommandHandler<DeleteCategoryCommand, bool> _deleteHandler;
    private readonly IQueryHandler<GetCategoryQuery, CategoryResult> _getHandler;
    private readonly IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResult>> _listHandler;

    public CategoriesController(
        ICommandHandler<CreateCategoryCommand, int> createHandler,
        ICommandHandler<UpdateCategoryCommand, bool> updateHandler,
        ICommandHandler<DeleteCategoryCommand, bool> deleteHandler,
        IQueryHandler<GetCategoryQuery, CategoryResult> getHandler,
        IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResult>> listHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getHandler = getHandler;
        _listHandler = listHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoryResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var command = new CreateCategoryCommand(request.Name);
        var id = await _createHandler.HandleAsync(command, ct);
        var category = await _getHandler.HandleAsync(new GetCategoryQuery(id), ct);

        return CreatedAtAction(nameof(GetById), new { id }, category);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var category = await _getHandler.HandleAsync(new GetCategoryQuery(id), ct);

        return Ok(category);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var categories = await _listHandler.HandleAsync(new ListCategoriesQuery(), ct);

        return Ok(categories);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CategoryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        var command = new UpdateCategoryCommand(id, request.Name);
        await _updateHandler.HandleAsync(command, ct);
        var category = await _getHandler.HandleAsync(new GetCategoryQuery(id), ct);

        return Ok(category);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _deleteHandler.HandleAsync(new DeleteCategoryCommand(id), ct);

        return NoContent();
    }
}
