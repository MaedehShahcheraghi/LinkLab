using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace LinkLab.BuildingBlocks.Idempotency;

public static class IdempotencyExtensions
{
    public static WebApplicationBuilder AddLinkLabIdempotency<TContext>(
        this WebApplicationBuilder builder,
        IConfiguration configuration)
        where TContext : DbContext
    {
        builder.Services
            .AddOptions<IdempotencyOptions>()
            .BindConfiguration(IdempotencyOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var connectionString = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Redis connection string 'Redis' is missing.");

            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        builder.Services.TryAddSingleton<IIdempotencyResponseCache,
            RedisIdempotencyResponseCache>();

        builder.Services.TryAddSingleton<RequestHasher>();
        builder.Services.TryAddSingleton(TimeProvider.System);
        
        builder.Services.TryAddTransient<IdempotencyActionFilter>();

        builder.Services.AddScoped<IIdempotencyStore,
            EfCoreIdempotencyStore<TContext>>();

        return builder;
    }

    public static IApplicationBuilder UseIdempotencyFingerprint(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<IdempotencyFingerprintMiddleware>();
    }

    public static RouteHandlerBuilder RequireIdempotency(
        this RouteHandlerBuilder builder,
        string scope)
    {
        builder.AddEndpointFilter<IdempotencyEndpointFilter>();
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new IdempotencyMetadata(scope));
        });
        return builder;
    }
}
