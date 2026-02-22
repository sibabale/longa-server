using Longa.Application.Common.Interfaces;
using Longa.Application.Common.Models;
using Longa.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Longa.API;

public static class UsersEndpoints
{
    public const string IdentifierForVendorHeader = "X-Identifier-For-Vendor";

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users", CreateOrGetUser)
            .WithName("CreateOrGetUser")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        app.MapPut("/users/me/push-token", PutPushToken)
            .WithName("PutPushToken")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateOrGetUser(
        [FromBody] CreateUserRequest request,
        IUserRepository userRepo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdentifierForVendor))
            return ErrorResults.BadRequest("Device identifier is required.");

        var existing = await userRepo.GetByIdentifierForVendorAsync(request.IdentifierForVendor, cancellationToken);
        if (existing is not null)
        {
            return Results.Ok(ToUserResponse(existing));
        }

        var user = new User
        {
            IdentifierForVendor = request.IdentifierForVendor.Trim(),
            DeviceModel = request.DeviceModel?.Trim(),
            DeviceMake = request.DeviceMake?.Trim()
        };
        await userRepo.CreateAsync(user, cancellationToken);
        return Results.Ok(ToUserResponse(user));
    }

    private static async Task<IResult> PutPushToken(
        [FromHeader(Name = IdentifierForVendorHeader)] string? identifierForVendor,
        [FromBody] PutPushTokenRequest request,
        IUserRepository userRepo,
        IPushTokenRepository pushTokenRepo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifierForVendor))
            return ErrorResults.BadRequest($"Header '{IdentifierForVendorHeader}' is required.");
        if (string.IsNullOrWhiteSpace(request.Token))
            return ErrorResults.BadRequest("Push token is required.");

        var user = await userRepo.GetByIdentifierForVendorAsync(identifierForVendor.Trim(), cancellationToken);
        if (user is null)
            return ErrorResults.NotFound("User not found. Create user first via POST /users.");

        await pushTokenRepo.UpsertAsync(user.Id, request.Token.Trim(), cancellationToken);
        return Results.NoContent();
    }

    private static UserResponse ToUserResponse(User u) => new(
        u.Id,
        u.IdentifierForVendor,
        u.DeviceModel,
        u.DeviceMake,
        u.CreatedAt
    );
}
