using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LinkLab.ServiceDefaults;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName =
        "X-Correlation-ID";

    private const int MaximumCorrelationIdLength = 128;

    public Task InvokeAsync(HttpContext httpContext)
    {
        var correlationId =
            GetOrCreateCorrelationId(httpContext);

        httpContext.TraceIdentifier = correlationId;

        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers[HeaderName] =
                correlationId;

            return Task.CompletedTask;
        });


        using var scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });
        return next(httpContext);
    }

    private static string GetOrCreateCorrelationId(
        HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName,
                out var headerValues))
        {
            var suppliedCorrelationId =
                headerValues.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(
                    suppliedCorrelationId) &&
                suppliedCorrelationId.Length <=
                MaximumCorrelationIdLength)
                return suppliedCorrelationId;
        }

        return Guid.NewGuid().ToString("N");
    }
}