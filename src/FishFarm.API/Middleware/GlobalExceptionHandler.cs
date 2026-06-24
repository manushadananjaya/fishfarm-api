using FishFarm.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ValidationException = FishFarm.Application.Common.Exceptions.ValidationException;

namespace FishFarm.API.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title, errors) = exception switch
        {
            NotFoundException   => (StatusCodes.Status404NotFound,   "Resource not found", null),
            ValidationException ve => (StatusCodes.Status400BadRequest, "Validation failed",   ve.Errors),
            _                   => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", null)
        };

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        
        string detail = exception.Message;
        if (errors is not null && errors.Count > 0)
        {
            var firstEntry = errors.First();
            var fieldName  = firstEntry.Key;
            var firstMsg   = firstEntry.Value.FirstOrDefault() ?? exception.Message;
            detail = $"{fieldName}: {firstMsg}";
        }

        var problem = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = detail,
            Instance = httpContext.Request.Path
        };

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
