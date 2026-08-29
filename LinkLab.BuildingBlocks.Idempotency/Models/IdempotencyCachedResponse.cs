namespace LinkLab.BuildingBlocks.Idempotency;

public sealed record IdempotencyCachedResponse(
    string RequestHash,
    int StatusCode,
    string? ContentType,
    string? ResponseBody);
