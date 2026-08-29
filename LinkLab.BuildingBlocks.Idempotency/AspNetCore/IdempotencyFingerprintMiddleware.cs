using Microsoft.AspNetCore.Http;

namespace LinkLab.BuildingBlocks.Idempotency;

public sealed class IdempotencyFingerprintMiddleware
{
    private readonly RequestDelegate _next;

    public IdempotencyFingerprintMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, RequestHasher hasher)
    {
        var metadata = context.GetEndpoint()?.Metadata
            .GetMetadata<IdempotencyMetadata>();

        if (metadata is null)
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering(
            bufferThreshold: 64 * 1024,
            bufferLimit: 1024 * 1024);

        var hash = await hasher.HashRequestAsync(
            context.Request, context.RequestAborted);

        context.Request.Body.Position = 0;

        context.Items[IdempotencyConstants.RequestHashKey] = hash;

        await _next(context);
    }
}
