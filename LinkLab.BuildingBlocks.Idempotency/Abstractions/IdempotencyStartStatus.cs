namespace LinkLab.BuildingBlocks.Idempotency;

public enum IdempotencyStartStatus
{
    Acquired,
    InProgress,
    Completed,
    Conflict
}
