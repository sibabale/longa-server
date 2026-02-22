using Microsoft.AspNetCore.Http;

namespace Longa.API;

/// <summary>
/// Returns uniform ErrorResponse JSON for all API errors.
/// </summary>
public static class ErrorResults
{
    public static IResult BadRequest(string message, string? raw = null) =>
        Results.Json(new ErrorResponse(400, message, raw), statusCode: 400);

    public static IResult NotFound(string message, string? raw = null) =>
        Results.Json(new ErrorResponse(404, message, raw), statusCode: 404);

    public static IResult Forbidden(string message, string? raw = null) =>
        Results.Json(new ErrorResponse(403, message, raw), statusCode: 403);

    public static IResult Conflict(string message, string? raw = null) =>
        Results.Json(new ErrorResponse(409, message, raw), statusCode: 409);

    public static IResult InternalServerError(string message, string? raw = null) =>
        Results.Json(new ErrorResponse(500, message, raw), statusCode: 500);
}
