using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkLab.BuildingBlocks.Idempotency;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : Attribute, IFilterFactory
{
    public string Scope { get; }
    public bool IsReusable => false;

    public IdempotentAttribute(string scope)
    {
        Scope = scope;
    }

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var filter = serviceProvider.GetRequiredService<IdempotencyActionFilter>();
        filter.Scope = Scope;
        return filter;
    }
}

public sealed class IdempotencyActionFilter : IAsyncActionFilter
{
    private readonly IIdempotencyStore _store;
    private readonly ILogger<IdempotencyActionFilter> _logger;
    
    public string Scope { get; set; } = default!;

    public IdempotencyActionFilter(IIdempotencyStore store, ILogger<IdempotencyActionFilter> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var values)
            || values.Count != 1)
        {
            context.Result = new BadRequestObjectResult(new { Error = "Exactly one Idempotency-Key header is required." });
            return;
        }

        var idempotencyKey = values[0]?.Trim();

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 256)
        {
            context.Result = new BadRequestObjectResult(new { Error = "Idempotency-Key must contain between 1 and 256 characters." });
            return;
        }

        var requestHash = httpContext.Items[IdempotencyConstants.RequestHashKey] as string;
        if (requestHash is null)
        {
            throw new InvalidOperationException("Request hash not found. Ensure IdempotencyFingerprintMiddleware is registered.");
        }

        var result = await _store.TryStartAsync(
            Scope, idempotencyKey, requestHash,
            httpContext.RequestAborted);

        switch (result.Status)
        {
            case IdempotencyStartStatus.Completed:
                context.Result = BuildCachedResult(result.CachedResponse!);
                return;

            case IdempotencyStartStatus.InProgress:
                context.Result = new ConflictObjectResult(new { Error = "This request is currently being processed. Please retry later." });
                return;

            case IdempotencyStartStatus.Conflict:
                context.Result = new UnprocessableEntityObjectResult(new { Error = "This Idempotency-Key was already used with a different request body." });
                return;
        }

        httpContext.Items[IdempotencyConstants.HandleKey] = result.Handle!;

        var executedContext = await next();

        if (executedContext.Exception is not null && !executedContext.ExceptionHandled)
        {
            try
            {
                await _store.ReleaseAsync(result.Handle!, CancellationToken.None);
            }
            catch (Exception releaseEx)
            {
                _logger.LogError(releaseEx,
                    "Failed to release idempotency handle for {Scope}:{Key}.",
                    Scope, idempotencyKey);
            }
        }
    }

    private static IActionResult BuildCachedResult(IdempotencyCachedResponse cached)
    {
        if (cached.ResponseBody is null)
            return new StatusCodeResult(cached.StatusCode);

        return new ContentResult
        {
            Content = cached.ResponseBody,
            ContentType = cached.ContentType ?? "application/json",
            StatusCode = cached.StatusCode
        };
    }
}
