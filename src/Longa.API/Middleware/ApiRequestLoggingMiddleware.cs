namespace Longa.API.Middleware;

public class ApiRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiRequestLoggingMiddleware> _logger;

    public ApiRequestLoggingMiddleware(RequestDelegate next, ILogger<ApiRequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path + context.Request.QueryString;
        var timestamp = DateTime.UtcNow.ToString("O");

        _logger.LogInformation("[Longa API] {Timestamp} {Method} {Path}", timestamp, method, path);

        await _next(context);

        var doneAt = DateTime.UtcNow.ToString("O");
        var status = context.Response.StatusCode;
        var statusLabel = status >= 400 ? "❌" : "✓";
        _logger.LogInformation(
            "[Longa API] {DoneAt} {Method} {Path} -> {Status} {StatusLabel}",
            doneAt,
            method,
            path,
            status,
            statusLabel);
    }
}
