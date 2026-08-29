namespace LinkLab.BuildingBlocks.Idempotency;

public interface IIdempotencyResponseCache
{
    Task<IdempotencyCachedResponse?> GetAsync(
        string serviceName, string scope, string key);

    Task SetAsync(
        string serviceName, string scope, string key,
        IdempotencyCachedResponse response, TimeSpan ttl);
}
