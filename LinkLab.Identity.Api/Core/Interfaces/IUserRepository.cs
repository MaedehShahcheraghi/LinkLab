using LinkLab.Identity.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace LinkLab.Identity.Api.Core.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default);
    Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default);
    Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<(ApplicationUser? User, RefreshToken? Token)> GetUserByRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenFamilyAsync(ApplicationUser user, Guid familyId, string reason, CancellationToken cancellationToken = default);
}
