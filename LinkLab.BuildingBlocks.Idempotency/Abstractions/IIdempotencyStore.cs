namespace LinkLab.BuildingBlocks.Idempotency;

public interface IIdempotencyStore
{
    Task<IdempotencyStartResult> TryStartAsync(
        string scope,
        string key,
        string requestHash,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(
        IdempotencyHandle handle,
        int statusCode,
        string? contentType,
        string? responseBody,
        CancellationToken cancellationToken = default);

    Task WarmCacheFromSqlAsync(
        IdempotencyHandle handle,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        IdempotencyHandle handle,
        CancellationToken cancellationToken = default);
}
