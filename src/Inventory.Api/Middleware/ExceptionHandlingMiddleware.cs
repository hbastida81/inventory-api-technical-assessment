using System.Text.Json;
using Inventory.Api.Models.Responses;
using Inventory.Application.Exceptions;

namespace Inventory.Api.Middleware;

public class ExceptionHandlingMiddleware
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            NotFoundException ex => new ErrorResponse { Status = 404, Message = ex.Message },
            ConflictException ex => new ErrorResponse { Status = 409, Message = ex.Message },
            ArgumentException ex => new ErrorResponse { Status = 400, Message = ex.Message },
            _ => HandleUnexpectedException(exception)
        };
        context.Response.StatusCode = response.Status;
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }

    private ErrorResponse HandleUnexpectedException(Exception exception)
    {
        _logger.LogError(exception, "An unexpected error occurred.");

        return new ErrorResponse { Status = 500, Message = "An unexpected error occurred." };
    }
}
