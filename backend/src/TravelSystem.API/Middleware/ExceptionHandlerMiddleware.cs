using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TravelSystem.Application.Interfaces;

namespace TravelSystem.API.Middleware;

/// <summary>
/// Global exception handler — converts domain exceptions to HTTP responses.
/// </summary>
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await WriteErrorAsync(ctx, ex);
        }
    }

    private static async Task WriteErrorAsync(HttpContext ctx, Exception ex)
    {
        ctx.Response.ContentType = "application/json";

        var (status, code) = ex switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, ex.Message),
            InvalidOperationException when ex.Message == "EMAIL_ALREADY_EXISTS" => (HttpStatusCode.Conflict, ex.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
            ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR")
        };

        ctx.Response.StatusCode = (int)status;

        var body = JsonSerializer.Serialize(new
        {
            error = code,
            statusCode = (int)status,
            timestamp = DateTime.UtcNow
        });

        await ctx.Response.WriteAsync(body);
    }
}

/// <summary>
/// Provides info about the currently authenticated user to services.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid UserId
    {
        get
        {
            var claim = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }

    public string Email =>
        _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public string Role =>
        _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsAuthenticated =>
        _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
