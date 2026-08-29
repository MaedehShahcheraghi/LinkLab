using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkLab.BuildingBlocks.Idempotency;

public sealed class EfCoreIdempotencyStore<TContext> : IIdempotencyStore
    where TContext : DbContext
{
    private readonly TContext _dbContext;
    private readonly IDbContextFactory<TContext> _dbFactory;
    private readonly IIdempotencyResponseCache _cache;
    private readonly IdempotencyOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EfCoreIdempotencyStore<TContext>> _logger;

    public EfCoreIdempotencyStore(
        TContext dbContext,
        IDbContextFactory<TContext> dbFactory,
        IIdempotencyResponseCache cache,
        IOptions<IdempotencyOptions> options,
        TimeProvider timeProvider,
        ILogger<EfCoreIdempotencyStore<TContext>> logger)
    {
        _dbContext = dbContext;
        _dbFactory = dbFactory;
        _cache = cache;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IdempotencyStartResult> TryStartAsync(
        string scope,
        string key,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);

        var cached = await _cache.GetAsync(_options.ServiceName, scope, key);

        if (cached is not null)
        {
            if (!string.Equals(cached.RequestHash, requestHash, StringComparison.Ordinal))
                return new IdempotencyStartResult(IdempotencyStartStatus.Conflict);

            return new IdempotencyStartResult(
                IdempotencyStartStatus.Completed,
                CachedResponse: cached);
        }

        return await AcquireViaSqlAsync(scope, key, requestHash, cancellationToken);
    }

    private async Task<IdempotencyStartResult> AcquireViaSqlAsync(
        string scope,
        string key,
        string requestHash,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var attemptId = Guid.NewGuid();
        var now = _timeProvider.GetUtcNow();
        var processingTtl = TimeSpan.FromSeconds(_options.ProcessingTtlSeconds);

        var record = IdempotencyRecord.CreateProcessing(
            scope, key, requestHash, attemptId, now, processingTtl);

        try
        {
            db.Set<IdempotencyRecord>().Add(record);
            await db.SaveChangesAsync(cancellationToken);

            return new IdempotencyStartResult(
                IdempotencyStartStatus.Acquired,
                Handle: new IdempotencyHandle(scope, key, requestHash, attemptId));
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return await ResolveExistingAsync(scope, key, requestHash, cancellationToken);
        }
    }

    private async Task<IdempotencyStartResult> ResolveExistingAsync(
        string scope,
        string key,
        string requestHash,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.Set<IdempotencyRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Scope == scope && r.Key == key,
                cancellationToken);

        if (existing is null)
            return new IdempotencyStartResult(IdempotencyStartStatus.InProgress);

        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            return new IdempotencyStartResult(IdempotencyStartStatus.Conflict);

        if (existing.State == IdempotencyState.Completed)
        {
            return new IdempotencyStartResult(
                IdempotencyStartStatus.Completed,
                CachedResponse: new IdempotencyCachedResponse(
                    existing.RequestHash,
                    existing.StatusCode!.Value,
                    existing.ContentType,
                    existing.ResponseBody));
        }

        var now = _timeProvider.GetUtcNow();

        if (existing.ExpiresAtUtc <= now)
            return await TryTakeoverAsync(db, existing, scope, key, requestHash, now, cancellationToken);

        return new IdempotencyStartResult(IdempotencyStartStatus.InProgress);
    }

    private async Task<IdempotencyStartResult> TryTakeoverAsync(
        TContext db,
        IdempotencyRecord existing,
        string scope,
        string key,
        string requestHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var newAttemptId = Guid.NewGuid();
        var processingTtl = TimeSpan.FromSeconds(_options.ProcessingTtlSeconds);

        var updated = await db.Set<IdempotencyRecord>()
            .Where(r =>
                r.Scope == scope &&
                r.Key == key &&
                r.RequestHash == requestHash &&
                r.State == IdempotencyState.Processing &&
                r.AttemptId == existing.AttemptId &&
                r.ExpiresAtUtc <= now)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.AttemptId, newAttemptId)
                    .SetProperty(r => r.ExpiresAtUtc, now.Add(processingTtl)),
                cancellationToken);

        if (updated == 1)
        {
            _logger.LogInformation(
                "Took over stale Processing record for {Scope}:{Key}. " +
                "Old AttemptId={OldAttemptId}, New AttemptId={NewAttemptId}.",
                scope, key, existing.AttemptId, newAttemptId);

            return new IdempotencyStartResult(
                IdempotencyStartStatus.Acquired,
                Handle: new IdempotencyHandle(scope, key, requestHash, newAttemptId));
        }

        return new IdempotencyStartResult(IdempotencyStartStatus.InProgress);
    }

    public async Task<bool> CompleteAsync(
        IdempotencyHandle handle,
        int statusCode,
        string? contentType,
        string? responseBody,
        CancellationToken cancellationToken = default)
    {
        var completedTtl = TimeSpan.FromHours(_options.CompletedTtlHours);
        var now = _timeProvider.GetUtcNow();

        var record = await _dbContext.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(
                r => r.Scope == handle.Scope && r.Key == handle.Key,
                cancellationToken);

        if (record is null)
        {
            _logger.LogWarning(
                "Idempotency completion failed: record {Scope}:{Key} not found.",
                handle.Scope, handle.Key);
            return false;
        }

        var completed = record.TryComplete(
            handle.AttemptId, statusCode, contentType, responseBody,
            now, completedTtl);

        if (!completed)
        {
            _logger.LogWarning(
                "Idempotency ownership check failed for {Scope}:{Key}. " +
                "Expected AttemptId={Expected}, actual AttemptId={Actual}, State={State}.",
                handle.Scope, handle.Key, handle.AttemptId, record.AttemptId, record.State);
        }

        return completed;
    }

    public async Task WarmCacheFromSqlAsync(
        IdempotencyHandle handle,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

            var record = await db.Set<IdempotencyRecord>()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.Scope == handle.Scope &&
                         r.Key == handle.Key &&
                         r.State == IdempotencyState.Completed &&
                         r.AttemptId == handle.AttemptId,
                    cancellationToken);

            if (record is null)
            {
                _logger.LogWarning(
                    "WarmCache: no Completed record for {Scope}:{Key} with AttemptId={AttemptId}.",
                    handle.Scope, handle.Key, handle.AttemptId);
                return;
            }

            var now = _timeProvider.GetUtcNow();
            var remainingTtl = record.ExpiresAtUtc - now;

            if (remainingTtl <= TimeSpan.Zero)
                return;

            var cached = new IdempotencyCachedResponse(
                record.RequestHash,
                record.StatusCode!.Value,
                record.ContentType,
                record.ResponseBody);

            await _cache.SetAsync(
                _options.ServiceName, handle.Scope, handle.Key,
                cached, remainingTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "WarmCache failed for {Scope}:{Key}. SQL remains source of truth.",
                handle.Scope, handle.Key);
        }
    }

    public async Task ReleaseAsync(
        IdempotencyHandle handle,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var deleted = await db.Set<IdempotencyRecord>()
            .Where(r =>
                r.Scope == handle.Scope &&
                r.Key == handle.Key &&
                r.State == IdempotencyState.Processing &&
                r.AttemptId == handle.AttemptId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
        {
            _logger.LogWarning(
                "Idempotency release: no Processing record with AttemptId={AttemptId} for {Scope}:{Key}.",
                handle.AttemptId, handle.Scope, handle.Key);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
    }
}
