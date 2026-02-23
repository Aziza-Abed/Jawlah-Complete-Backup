using System.Net;
using System.Text.Json;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.Exceptions;

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

        var statusCode = exception switch
        {
            NotFoundException or KeyNotFoundException or FileNotFoundException => HttpStatusCode.NotFound,
            UnauthorizedException or UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            Core.Exceptions.ValidationException or ArgumentException or InvalidOperationException or FollowUpException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        var message = exception switch
        {
            UnauthorizedAccessException => "غير مصرح",
            KeyNotFoundException or FileNotFoundException => "غير موجود",
            _ when statusCode == HttpStatusCode.InternalServerError => "حدث خطأ داخلي في الخادم",
            _ => exception.Message
        };

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
