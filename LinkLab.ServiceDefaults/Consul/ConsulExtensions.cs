using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LinkLab.ServiceDefaults.Consul;

public static class ConsulExtensions
{
    public static WebApplicationBuilder AddConsulRegistration(
        this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddConsulOptions(builder);
        AddConsulClient(builder);
        AddConsulHostedService(builder);

        return builder;
    }

    private static void AddConsulOptions(
        WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<ConsulRegistrationOptions>()
            .Bind(
                builder.Configuration.GetSection(
                    ConsulRegistrationOptions.SectionName))
            .ValidateOnStart();

        builder.Services.AddSingleton<
            IValidateOptions<ConsulRegistrationOptions>,
            ConsulRegistrationOptionsValidator>();
    }

    private static void AddConsulClient(
        WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IConsulClient>(
            serviceProvider =>
            {
                var options = serviceProvider
                    .GetRequiredService<
                        IOptions<ConsulRegistrationOptions>>()
                    .Value;

                if (!options.Enabled) return new ConsulClient();

                if (!Uri.TryCreate(
                        options.Address,
                        UriKind.Absolute,
                        out var consulAddress))
                    throw new InvalidOperationException(
                        "Consul address is invalid.");

                return new ConsulClient(configuration => { configuration.Address = consulAddress; });
            });
    }

    private static void AddConsulHostedService(
        WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<
            ConsulRegistrationHostedService>();
    }
}