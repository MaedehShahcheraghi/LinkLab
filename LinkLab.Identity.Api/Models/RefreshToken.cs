namespace LinkLab.Identity.Api.Models;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public string? RevocationReason { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? RevokedByIp { get; private set; }

    public string? UserAgent { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public ApplicationUser User { get; private set; } = null!;

    public bool IsExpired(
        DateTimeOffset utcNow)
    {
        return ExpiresAtUtc <= utcNow;
    }

    public bool IsActive(
        DateTimeOffset utcNow)
    {
        return RevokedAtUtc is null &&
               !IsExpired(utcNow);
    }

    public static RefreshToken Create(
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string? createdByIp,
        string? userAgent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        if (expiresAtUtc <= createdAtUtc)
            throw new ArgumentOutOfRangeException(
                nameof(expiresAtUtc),
                "Expiration must be after creation time.");

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            CreatedByIp = createdByIp,
            UserAgent = userAgent
        };
    }

    public void Revoke(
        DateTimeOffset revokedAtUtc,
        string reason,
        string? revokedByIp,
        string? replacedByTokenHash = null)
    {
        if (RevokedAtUtc is not null) return;

        RevokedAtUtc = revokedAtUtc;
        RevocationReason = reason;
        RevokedByIp = revokedByIp;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}