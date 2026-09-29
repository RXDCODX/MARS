using System.Net;
using System.Text.Json;
using MARS.Shared.Exceptions;
using MARS.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger
    )
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
        catch (ServiceUnavailableException ex)
        {
            _logger.LogWarning(ex, "Service unavailable: {ServiceName}", ex.ServiceName);
            await WriteErrorResponse(context, HttpStatusCode.ServiceUnavailable, ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error");
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (MarsException ex)
        {
            _logger.LogError(ex, "MARS error");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponse(
                context,
                HttpStatusCode.InternalServerError,
                "Internal server error"
            );
        }
    }

    private static async Task WriteErrorResponse(
        HttpContext context,
        HttpStatusCode statusCode,
        string message
    )
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var result = OperationResult.Fail(message, statusCode);
        await context.Response.WriteAsJsonAsync(result);
    }
}
