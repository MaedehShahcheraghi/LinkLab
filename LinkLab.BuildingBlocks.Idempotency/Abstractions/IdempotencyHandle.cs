namespace LinkLab.BuildingBlocks.Idempotency;

public sealed record IdempotencyHandle(
    string Scope,
    string Key,
    string RequestHash,
    Guid AttemptId);
