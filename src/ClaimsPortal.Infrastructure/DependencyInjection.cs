using ClaimsPortal.Core.Claims;
using ClaimsPortal.Infrastructure.Data;
using ClaimsPortal.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClaimsPortal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Claims")
            ?? throw new InvalidOperationException("Connection string 'Claims' is not configured.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddDbContext<ClaimsDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IClaimRepository, EfClaimRepository>();
        services.AddSingleton<IClaimNumberGenerator, RandomClaimNumberGenerator>();
        services.AddScoped<ClaimService>();

        services.AddScoped<IMessagePublisher, LoggingMessagePublisher>();
        services.AddSingleton<OutboxDispatcher>();
        services.AddHostedService(sp => sp.GetRequiredService<OutboxDispatcher>());

        return services;
    }
}
