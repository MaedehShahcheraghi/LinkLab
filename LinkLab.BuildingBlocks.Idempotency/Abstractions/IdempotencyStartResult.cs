namespace LinkLab.BuildingBlocks.Idempotency;

public sealed record IdempotencyStartResult(
    IdempotencyStartStatus Status,
    IdempotencyHandle? Handle = null,
    IdempotencyCachedResponse? CachedResponse = null);
