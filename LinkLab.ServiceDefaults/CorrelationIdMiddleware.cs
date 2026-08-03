using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LinkLab.ServiceDefaults;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName =
        "X-Correlation-ID";

    private const int MaximumCorrelationIdLength = 128;

    private readonly ILogger<
        CorrelationIdMiddleware> _logger;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

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


        using var scope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });
        return _next(httpContext);
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