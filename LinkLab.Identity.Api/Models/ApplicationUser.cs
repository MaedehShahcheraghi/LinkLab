using Microsoft.AspNetCore.Identity;

namespace LinkLab.Identity.Api.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastLoginAtUtc { get; set; }

    public int AuthzVersion { get; set; } = 1;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public void IncrementAuthzVersion() => AuthzVersion++;
}