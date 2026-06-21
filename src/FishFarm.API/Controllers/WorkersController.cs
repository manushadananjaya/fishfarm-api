using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.Workers.Commands;
using FishFarm.Application.Features.Workers.DTOs;
using FishFarm.Application.Features.Workers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FishFarm.API.Controllers;

[ApiController]
[Route("api/fishfarms/{fishFarmId:guid}/workers")]
[Produces("application/json")]
public sealed class WorkersController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkersController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get paginated active workers for a specific fish farm.</summary>
    /// <remarks>
    /// Design decisions:
    /// - Filtering by ?position= / ?certifiedOnly=true / ?search= deferred (#6):
    ///   No filter params are in the spec; adding dead API surface before frontend
    ///   confirms requirements is premature. Planned for next iteration.
    /// - Soft-deleted farms return 404 not 410 (#8): the global query filter makes
    ///   deleted records invisible; 404 is semantically correct here — the resource
    ///   is not accessible. Exposing 410 would require tracking deleted IDs separately
    ///   and leak internal soft-delete state to consumers.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<WorkerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(
        Guid fishFarmId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize   = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize   < 1) pageSize   = 1;
        if (pageSize   > 100) pageSize = 100;
        var result = await _mediator.Send(
            new GetWorkersByFarmQuery(fishFarmId, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get a single worker by id within a fish farm.</summary>
    [HttpGet("{workerId:guid}")]
    [ProducesResponseType(typeof(WorkerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid fishFarmId,
        Guid workerId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkerByIdQuery(fishFarmId, workerId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Register a new worker for a fish farm.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid fishFarmId,
        [FromForm] CreateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new CreateWorkerCommand(fishFarmId, request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { fishFarmId, workerId = id }, new { id });
    }

    /// <summary>Update worker information (does not change picture).</summary>
    [HttpPut("{workerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid fishFarmId,
        Guid workerId,
        [FromBody] UpdateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateWorkerCommand(fishFarmId, workerId, request), cancellationToken);
        return NoContent();
    }

    /// <summary>Replace the worker's profile picture.</summary>
    [HttpPatch("{workerId:guid}/picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePicture(
        Guid fishFarmId,
        Guid workerId,
        [FromForm] UpdateWorkerPictureRequest request,
        CancellationToken cancellationToken)
    {
        var url = await _mediator.Send(
            new UpdateWorkerPictureCommand(fishFarmId, workerId, request), cancellationToken);
        return Ok(new { pictureUrl = url });
    }

    /// <summary>Soft-delete a worker from a fish farm.</summary>
    [HttpDelete("{workerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid fishFarmId,
        Guid workerId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteWorkerCommand(fishFarmId, workerId), cancellationToken);
        return NoContent();
    }
}
