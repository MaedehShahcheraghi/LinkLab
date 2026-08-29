namespace LinkLab.BuildingBlocks.Idempotency;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    public string ServiceName { get; init; } = default!;
    public int ProcessingTtlSeconds { get; init; } = 30;
    public int CompletedTtlHours { get; init; } = 24;
}
