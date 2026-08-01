using Microsoft.AspNetCore.Builder;
using Prometheus;

namespace LinkLab.ServiceDefaults;

public static class PrometheusExtensions
{
    public static WebApplicationBuilder AddPrometheusMonitoring(
        this WebApplicationBuilder builder)
    {
        builder.Services.UseHttpClientMetrics();

        return builder;
    }

    public static WebApplication UsePrometheusMonitoring(
        this WebApplication app)
    {
        app.UseHttpMetrics();

        return app;
    }

    public static WebApplication MapPrometheusMetrics(
        this WebApplication app)
    {
        app.MapMetrics();

        return app;
    }
}