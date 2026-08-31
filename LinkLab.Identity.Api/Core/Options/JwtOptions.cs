using System.ComponentModel.DataAnnotations;

namespace LinkLab.Identity.Api.Core.Options;

public sealed class JwtOptions
{
    public const string SectionName = "JwtSettings";

    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; init; } = 15;

    [Range(1, 365)]
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
