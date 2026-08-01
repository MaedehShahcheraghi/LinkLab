using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LinkLab.ServiceDefaults;

public static class HealthCheckExtensions
{
    private const string LiveTag = "live";

    public static WebApplicationBuilder AddDefaultHealthChecks(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { LiveTag });

        return builder;
    }


    public static WebApplication MapDefaultHealthChecks(
        this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains(LiveTag),
            ResponseWriter = WriteResponseAsync
        });


        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteResponseAsync
        });


        return app;
    }


    private static Task WriteResponseAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMs =
                report.TotalDuration.TotalMilliseconds,

            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs =
                    entry.Value.Duration.TotalMilliseconds
            })
        };

        return context.Response.WriteAsJsonAsync(
            response,
            context.RequestAborted);
    }
}