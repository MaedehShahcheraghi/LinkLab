namespace LinkLab.BuildingBlocks.Idempotency;

public sealed class IdempotencyRecord
{
    private IdempotencyRecord() { }

    public long Id { get; private set; }
    public string Scope { get; private set; } = default!;
    public string Key { get; private set; } = default!;
    public string RequestHash { get; private set; } = default!;
    public Guid AttemptId { get; private set; }
    public IdempotencyState State { get; private set; }
    public int? StatusCode { get; private set; }
    public string? ContentType { get; private set; }
    public string? ResponseBody { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public static IdempotencyRecord CreateProcessing(
        string scope,
        string key,
        string requestHash,
        Guid attemptId,
        DateTimeOffset now,
        TimeSpan processingTtl)
    {
        return new IdempotencyRecord
        {
            Scope = scope,
            Key = key,
            RequestHash = requestHash,
            AttemptId = attemptId,
            State = IdempotencyState.Processing,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(processingTtl)
        };
    }

    public bool TryComplete(
        Guid attemptId,
        int statusCode,
        string? contentType,
        string? responseBody,
        DateTimeOffset now,
        TimeSpan completedTtl)
    {
        if (State != IdempotencyState.Processing)
            return false;

        if (AttemptId != attemptId)
            return false;

        State = IdempotencyState.Completed;
        StatusCode = statusCode;
        ContentType = contentType;
        ResponseBody = responseBody;
        CompletedAtUtc = now;
        ExpiresAtUtc = now.Add(completedTtl);
        return true;
    }
}
