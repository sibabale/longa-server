using Longa.Application.Common.Interfaces;
using Longa.Infrastructure.Data;
using Longa.Infrastructure.Options;
using Longa.Infrastructure.Repositories;
using Longa.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Longa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MapboxOptions>(configuration.GetSection(MapboxOptions.SectionName));
        services.Configure<MatchingOptions>(configuration.GetSection(MatchingOptions.SectionName));
        services.AddHttpClient<ILocationSearchService, MapboxLocationSearchService>();
        services.AddScoped<IDateTime, DateTimeService>();

        var rawConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is required.");
        var connectionString = ConnectionStringBuilder.NormalizeForNpgsql(rawConnectionString);
        services.AddDbContext<LongaDbContext>(opt =>
        {
            opt.UseNpgsql(connectionString);
            opt.UseSnakeCaseNamingConvention();
        });

        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IPushTokenRepository, PushTokenRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IMatchingService, MatchingService>();

        return services;
    }
}
