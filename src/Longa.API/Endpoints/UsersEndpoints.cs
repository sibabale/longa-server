using Longa.Domain.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Longa.Application.Common.Models;
using Longa.Application.Common.Interfaces;

namespace Longa.API;

public static class UsersEndpoints
{
    public const string IdentifierForVendorHeader = "X-Identifier-For-Vendor";

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users", CreateOrGetUser)
            .RequireAuthorization()
            .WithName("CreateOrGetUser")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        app.MapPut("/users/me/push-token", PutPushToken)
            .RequireAuthorization()
            .WithName("PutPushToken")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateOrGetUser(
        HttpContext httpContext,
        IUserRepository userRepo,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetOrCreateCurrentUserAsync(httpContext.User, userRepo, cancellationToken);
        if (currentUser is null)
            return Results.Unauthorized();

        return Results.Ok(ToUserResponse(currentUser));
    }

    private static async Task<IResult> PutPushToken(
        HttpContext httpContext,
        [FromBody] PutPushTokenRequest request,
        IUserRepository userRepo,
        IPushTokenRepository pushTokenRepo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return ErrorResults.BadRequest("Push token is required.");

        var currentUser = await GetOrCreateCurrentUserAsync(httpContext.User, userRepo, cancellationToken);
        if (currentUser is null)
            return Results.Unauthorized();

        await pushTokenRepo.UpsertAsync(currentUser.Id, request.Token.Trim(), cancellationToken);
        return Results.NoContent();
    }

    private static UserResponse ToUserResponse(User u) => new(
        u.Id,
        u.Email,
        u.FullName,
        u.IdentifierForVendor ?? "",
        u.DeviceModel,
        u.DeviceMake,
        u.CreatedAt
    );

    internal static async Task<User?> GetOrCreateCurrentUserAsync(
        ClaimsPrincipal principal,
        IUserRepository userRepo,
        CancellationToken cancellationToken)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub))
            return null;

        var user = await userRepo.GetByAuth0UserIdAsync(sub, cancellationToken);
        if (user is not null)
            return user;

        var email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email) ?? "";
        var fullName = principal.FindFirstValue("name") ?? principal.FindFirstValue(ClaimTypes.Name) ?? "";

        var newUser = new User
        {
            Auth0UserId = sub,
            Email = email,
            FullName = fullName,
            IdentifierForVendor = null,
            DeviceModel = null,
            DeviceMake = null
        };
        await userRepo.CreateAsync(newUser, cancellationToken);
        return newUser;
    }
}
