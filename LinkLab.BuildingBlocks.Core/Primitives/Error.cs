namespace LinkLab.BuildingBlocks.Core.Primitives;

public sealed record Error(
    string Code,
    string Description,
    ErrorType Type,
    string? Details = null)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.", ErrorType.Failure);

    public static Error Failure(string code, string description, string? details = null)
        => new(code, description, ErrorType.Failure, details);
    public static Error Validation(string code, string description, string? details = null)
        => new(code, description, ErrorType.Validation, details);
    public static Error NotFound(string code, string description, string? details = null)
        => new(code, description, ErrorType.NotFound, details);
    public static Error Conflict(string code, string description, string? details = null)
        => new(code, description, ErrorType.Conflict, details);
    public static Error Unauthorized(string code, string description, string? details = null)
        => new(code, description, ErrorType.Unauthorized, details);
    public static Error Forbidden(string code, string description, string? details = null)
        => new(code, description, ErrorType.Forbidden, details);
}
