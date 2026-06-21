using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.FishFarms.Commands;
using FishFarm.Application.Features.FishFarms.DTOs;
using FishFarm.Application.Features.FishFarms.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FishFarm.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class FishFarmsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FishFarmsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get a paginated list of all fish farms.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<FishFarmSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize   = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageSize > 50) pageSize = 50;  
        var result = await _mediator.Send(new GetFishFarmsQuery(pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get a single fish farm with all its active workers.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FishFarmDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFishFarmByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Register a new fish farm (multipart/form-data for optional picture).</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromForm] CreateFishFarmRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new CreateFishFarmCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Update fish farm metadata (does not change the picture).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFishFarmRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateFishFarmCommand(id, request), cancellationToken);
        return NoContent();
    }

    /// <summary>Replace the fish farm picture only.</summary>
    [HttpPatch("{id:guid}/picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePicture(
        Guid id,
        [FromForm] UpdateFishFarmPictureRequest request,
        CancellationToken cancellationToken)
    {
        var url = await _mediator.Send(new UpdateFishFarmPictureCommand(id, request), cancellationToken);
        return Ok(new { pictureUrl = url });
    }

    /// <summary>Soft-delete a fish farm and all its workers.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFishFarmCommand(id), cancellationToken);
        return NoContent();
    }
}
