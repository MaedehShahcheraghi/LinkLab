using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkLab.BuildingBlocks.Idempotency;

public sealed record IdempotencyMetadata(string Scope);

public sealed class IdempotencyEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        var metadata = httpContext.GetEndpoint()?.Metadata
            .GetMetadata<IdempotencyMetadata>();

        if (metadata is null)
            return await next(context);

        var store = httpContext.RequestServices.GetRequiredService<IIdempotencyStore>();
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<IdempotencyEndpointFilter>>();

        if (!httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var values)
            || values.Count != 1)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Idempotency-Key",
                detail: "Exactly one Idempotency-Key header is required.");
        }

        var idempotencyKey = values[0]?.Trim();

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 256)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Idempotency-Key",
                detail: "Idempotency-Key must contain between 1 and 256 characters.");
        }

        var requestHash = httpContext.Items[IdempotencyConstants.RequestHashKey] as string
            ?? throw new InvalidOperationException(
                "Request hash not found. Ensure IdempotencyFingerprintMiddleware is registered.");

        var result = await store.TryStartAsync(
            metadata.Scope, idempotencyKey, requestHash,
            httpContext.RequestAborted);

        switch (result.Status)
        {
            case IdempotencyStartStatus.Completed:
                return BuildCachedResult(result.CachedResponse!);

            case IdempotencyStartStatus.InProgress:
                return Results.Conflict(new
                {
                    Error = "This request is currently being processed. Please retry later."
                });

            case IdempotencyStartStatus.Conflict:
                return Results.UnprocessableEntity(new
                {
                    Error = "This Idempotency-Key was already used with a different request body."
                });
        }

        httpContext.Items[IdempotencyConstants.HandleKey] = result.Handle!;

        try
        {
            return await next(context);
        }
        catch
        {
            try
            {
                await store.ReleaseAsync(result.Handle!, CancellationToken.None);
            }
            catch (Exception releaseEx)
            {
                logger.LogError(releaseEx,
                    "Failed to release idempotency handle for {Scope}:{Key}.",
                    metadata.Scope, idempotencyKey);
            }

            throw;
        }
    }

    private static IResult BuildCachedResult(IdempotencyCachedResponse cached)
    {
        if (cached.ResponseBody is null)
            return Results.StatusCode(cached.StatusCode);

        return Results.Text(
            cached.ResponseBody,
            cached.ContentType ?? "application/json",
            statusCode: cached.StatusCode);
    }
}

public static class IdempotencyConstants
{
    internal static readonly object HandleKey = new();
    internal static readonly object RequestHashKey = new();

    public static IdempotencyHandle GetIdempotencyHandle(this HttpContext context)
    {
        return context.Items[HandleKey] as IdempotencyHandle
            ?? throw new InvalidOperationException(
                "IdempotencyHandle not found. Ensure this endpoint uses .RequireIdempotency().");
    }
}
