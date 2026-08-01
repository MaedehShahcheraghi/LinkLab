using LinkLab.ServiceDefaults.Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LinkLab.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static WebApplicationBuilder AddServiceDefaults(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();

        builder.AddDefaultHealthChecks();
        builder.AddPrometheusMonitoring();
        builder.AddConsulRegistration();

        return builder;
    }

    public static WebApplication MapServiceDefaults(
        this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UsePrometheusMonitoring();

        app.UseExceptionHandler();

        app.MapDefaultHealthChecks();
        app.MapPrometheusMetrics();

        return app;
    }
}