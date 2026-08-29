using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace LinkLab.BuildingBlocks.Idempotency;

public sealed class RedisIdempotencyResponseCache : IIdempotencyResponseCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisIdempotencyResponseCache> _logger;

    public RedisIdempotencyResponseCache(
        IConnectionMultiplexer redis,
        ILogger<RedisIdempotencyResponseCache> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<IdempotencyCachedResponse?> GetAsync(
        string serviceName, string scope, string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(BuildKey(serviceName, scope, key));

            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<IdempotencyCachedResponse>(
                value.ToString(), JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Redis GET failed for {Scope}:{Key}. Falling back to SQL.",
                scope, key);
            return null;
        }
    }

    public async Task SetAsync(
        string serviceName, string scope, string key,
        IdempotencyCachedResponse response, TimeSpan ttl)
    {
        try
        {
            var db = _redis.GetDatabase();
            var serialized = JsonSerializer.Serialize(response, JsonOptions);
            await db.StringSetAsync(BuildKey(serviceName, scope, key), serialized, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for {Scope}:{Key}.", scope, key);
        }
    }

    private static string BuildKey(string serviceName, string scope, string key)
        => $"linklab:{serviceName}:idempotency:{scope}:{key}";
}
