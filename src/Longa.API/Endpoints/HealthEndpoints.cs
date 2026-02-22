using Longa.Application.Common.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Longa.API;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", (IHealthService healthService) =>
        {
            var response = healthService.GetStatus();
            return Results.Ok(response);
        })
        .WithName("GetHealth")
        .Produces(StatusCodes.Status200OK);

        return app;
    }
}
