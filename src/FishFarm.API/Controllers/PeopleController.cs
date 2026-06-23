using FishFarm.Application.Common.Models;
using FishFarm.Application.Features.People.Commands;
using FishFarm.Application.Features.People.DTOs;
using FishFarm.Application.Features.People.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FishFarm.API.Controllers;

/// <summary>
/// Manages person profiles. A person can be assigned to multiple fish farms via
/// the <c>POST /api/fishfarms/{farmId}/workers</c> endpoint.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class PeopleController : ControllerBase
{
    private readonly IMediator _mediator;
    public PeopleController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// List all active people with optional search and certification-expiry filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<PersonSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int     pageNumber  = 1,
        [FromQuery] int     pageSize    = 20,
        [FromQuery] string? search      = null,
        [FromQuery] bool?   certExpired = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize   < 1) pageSize   = 1;
        if (pageSize   > 100) pageSize = 100;

        var result = await _mediator.Send(
            new GetPeopleQuery(pageNumber, pageSize, search, certExpired),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Get a person's full profile including their current farm assignments.</summary>
    [HttpGet("{personId:guid}")]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid personId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPersonByIdQuery(personId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new person profile. Optionally include a profile picture (multipart/form-data).
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromForm] CreatePersonRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new CreatePersonCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { personId = id }, new { id });
    }

    /// <summary>Update a person's personal details (not picture or farm assignments).</summary>
    [HttpPut("{personId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid personId,
        [FromBody] UpdatePersonRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdatePersonCommand(personId, request), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-delete a person. All their active farm assignments are also soft-deleted.
    /// </summary>
    [HttpDelete("{personId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid personId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePersonCommand(personId), cancellationToken);
        return NoContent();
    }

    /// <summary>Replace a person's profile picture.</summary>
    [HttpPatch("{personId:guid}/picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePicture(
        Guid personId,
        [FromForm] UpdatePersonPictureRequest request,
        CancellationToken cancellationToken)
    {
        var url = await _mediator.Send(
            new UpdatePersonPictureCommand(personId, request), cancellationToken);
        return Ok(new { pictureUrl = url });
    }

    /// <summary>Delete a person's profile picture. Idempotent.</summary>
    [HttpDelete("{personId:guid}/picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePicture(
        Guid personId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePersonPictureCommand(personId), cancellationToken);
        return NoContent();
    }
}
