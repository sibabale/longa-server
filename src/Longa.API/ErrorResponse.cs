namespace Longa.API;

/// <summary>
/// Uniform error response for all API errors.
/// </summary>
public record ErrorResponse(
    int Status,
    string Message,
    string? Error = null
);
