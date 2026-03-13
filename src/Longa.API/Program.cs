using Longa.API;
using Longa.Application;
using Longa.Infrastructure;
using Longa.API.Middleware;
using Auth0.AspNetCore.Authentication.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;

// A function that will build the application before it runs, its use to add services to the application
var builder = WebApplication.CreateBuilder(args);

// Wiring application services and interfaces.
builder.Services.AddApplication();
// Wiring infrastructure services and interfaces and using the configuration from the builder.
builder.Services.AddInfrastructure(builder.Configuration);

//Note we don't need AddDomain(). It’s just a class library (entities). 
// Other projects reference it; there’s nothing to register in DI.Note

// Auth0 API Authentication
builder.Services.AddAuth0ApiAuthentication(
    options =>
    {
        options.Domain = builder.Configuration["Auth0:Domain"];
        options.JwtBearerOptions = new JwtBearerOptions
        {
            Audience = builder.Configuration["Auth0:Audience"]
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

// This is the final application that has been built
var app = builder.Build();

//These 2 methods from Microsoft.AspNetCore.Builder, which is included by the Microsoft.AspNetCore SDK.
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseMiddleware<ApiRequestLoggingMiddleware>();

app.UseHttpsRedirection();
app.MapHealthEndpoints();
app.MapSearchEndpoints();
app.MapUsersEndpoints();
app.MapTripsEndpoints();
app.MapBookingsEndpoints();

app.Run();

public partial class Program;
