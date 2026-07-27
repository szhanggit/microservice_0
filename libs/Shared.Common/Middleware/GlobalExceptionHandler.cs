using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Common.Exceptions;

namespace Shared.Common.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = MapException(exception);

        logger.LogError(exception, "Unhandled exception while processing {Method} {Path}: {Message}",
            httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
        DuplicateEmailException => (StatusCodes.Status409Conflict, "Duplicate email"),
        ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
    };
}
