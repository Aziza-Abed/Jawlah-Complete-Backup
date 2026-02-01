using System.Net;
using System.Text.Json;
using FollowUp.Core.DTOs.Common;

namespace FollowUp.API.Middleware;

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
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // determine status code and message based on exception type
        HttpStatusCode statusCode;
        string message;

        if (exception is ArgumentException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = exception.Message;
        }
        else if (exception is UnauthorizedAccessException)
        {
            statusCode = HttpStatusCode.Unauthorized;
            message = "غير مصرح";
        }
        else if (exception is KeyNotFoundException)
        {
            statusCode = HttpStatusCode.NotFound;
            message = "غير موجود";
        }
        else
        {
            statusCode = HttpStatusCode.InternalServerError;
            message = "حدث خطأ داخلي في الخادم";
        }

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.ErrorResponse(message);
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

// extension method to easily add the middleware in Program.cs
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
