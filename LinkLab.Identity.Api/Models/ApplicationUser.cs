using Microsoft.AspNetCore.Identity;

namespace LinkLab.Identity.Api.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } =
        new List<RefreshToken>();
}