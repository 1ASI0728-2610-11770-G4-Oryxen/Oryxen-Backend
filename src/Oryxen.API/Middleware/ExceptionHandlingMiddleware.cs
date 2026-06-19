using Microsoft.AspNetCore.Mvc;
using Oryxen.Application.Common.Exceptions;

namespace Oryxen.API.Middleware;

/// <summary>
/// Translates Application-layer domain exceptions into RFC 7807 ProblemDetails responses
/// with the appropriate HTTP status code.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            EmailAlreadyExistsException => (StatusCodes.Status409Conflict, exception.Message),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, exception.Message),
            InvalidRefreshTokenException => (StatusCodes.Status401Unauthorized, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Path}.", context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Handled domain exception: {Message}", exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.io/{statusCode}",
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
