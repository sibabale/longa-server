using Longa.API;
using Longa.API.Middleware;
using Longa.Application;
using Longa.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

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
