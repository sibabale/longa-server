using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Longa.API.Middleware;

/// <summary>
/// Catches unhandled exceptions and returns uniform ErrorResponse JSON.
/// Raw exception is logged; only user-friendly message is returned to the client.
/// </summary>
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var raw = ex.ToString();
            _logger.LogError(ex, "Unhandled exception: {Raw}", raw);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse(
                500,
                "Something went wrong. Please try again later.",
                _env.IsDevelopment() ? raw : ex.Message);

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}
