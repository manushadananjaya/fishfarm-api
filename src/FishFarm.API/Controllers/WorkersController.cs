using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.FarmWorkers.Commands;
using FishFarm.Application.Features.FarmWorkers.DTOs;
using FishFarm.Application.Features.FarmWorkers.Queries;
using FishFarm.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FishFarm.API.Controllers;

/// <summary>
/// Manages the assignment of people to a specific fish farm.
/// A person must first exist in <c>POST /api/people</c> before they can be assigned here.
/// </summary>
[ApiController]
[Route("api/fishfarms/{fishFarmId:guid}/workers")]
[Produces("application/json")]
public sealed class WorkersController : ControllerBase
{
    private readonly IMediator _mediator;
    public WorkersController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// List all active worker assignments for a fish farm.
    /// Supports search by person name/email, position filter, and certification-expiry filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<FarmWorkerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(
        Guid fishFarmId,
        [FromQuery] int             pageNumber  = 1,
        [FromQuery] int             pageSize    = 20,
        [FromQuery] string?         search      = null,
        [FromQuery] WorkerPosition? position    = null,
        [FromQuery] bool?           certExpired = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize   < 1) pageSize   = 1;
        if (pageSize   > 100) pageSize = 100;

        var result = await _mediator.Send(
            new GetFarmWorkersQuery(
                fishFarmId, pageNumber, pageSize, search, position, certExpired),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Get a single farm-worker assignment by its ID.</summary>
    [HttpGet("{farmWorkerId:guid}")]
    [ProducesResponseType(typeof(FarmWorkerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid fishFarmId,
        Guid farmWorkerId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetFarmWorkerByIdQuery(fishFarmId, farmWorkerId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Assign an existing person to this farm with a specific position.
    /// The person must already exist — create them via <c>POST /api/people</c> first.
    /// A person can only hold one active assignment per farm.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(
        Guid fishFarmId,
        [FromBody] AssignPersonToFarmRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new AssignPersonToFarmCommand(fishFarmId, request), cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { fishFarmId, farmWorkerId = id },
            new { id });
    }

    /// <summary>Update the position (role) of an existing assignment.</summary>
    [HttpPut("{farmWorkerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePosition(
        Guid fishFarmId,
        Guid farmWorkerId,
        [FromBody] UpdateFarmWorkerRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateFarmWorkerCommand(fishFarmId, farmWorkerId, request), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Remove a person's assignment from this farm (soft-delete).
    /// The person's profile and other farm assignments are not affected.
    /// </summary>
    [HttpDelete("{farmWorkerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(
        Guid fishFarmId,
        Guid farmWorkerId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new RemoveFarmWorkerCommand(fishFarmId, farmWorkerId), cancellationToken);
        return NoContent();
    }
}
