using Inventory.Api.Models.Requests;
using Inventory.Api.Models.Responses;
using Inventory.Application.Abstractions;
using Inventory.Application.InventoryMovements.Commands;
using Inventory.Application.InventoryMovements.Models;
using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products/{productId:int}/movements")]
public class InventoryMovementsController : ControllerBase
{
    private readonly ICommandHandler<RegisterMovementCommand, MovementResult> _registerHandler;

    public InventoryMovementsController(ICommandHandler<RegisterMovementCommand, MovementResult> registerHandler)
    {
        _registerHandler = registerHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MovementResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(int productId, [FromBody] CreateMovementRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<MovementType>(request.Type, true, out var movementType))
        {
            return BadRequest(new { status = 400, message = "Invalid movement type. Use 'Entry' or 'Exit'." });
        }
        var command = new RegisterMovementCommand(productId, movementType, request.Quantity);
        var result = await _registerHandler.HandleAsync(command, ct);

        return Created($"api/products/{productId}/movements", result);
    }
}
