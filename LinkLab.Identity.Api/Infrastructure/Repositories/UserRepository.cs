using LinkLab.Identity.Api.Core.Interfaces;
using LinkLab.Identity.Api.Data;
using LinkLab.Identity.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinkLab.Identity.Api.Infrastructure.Repositories;

public sealed class UserRepository(
    UserManager<ApplicationUser> userManager,
    IdentityDbContext dbContext) : IUserRepository
{
    public async Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await userManager.FindByEmailAsync(email);

    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default)
        => await userManager.CheckPasswordAsync(user, password);

    public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default)
        => await userManager.CreateAsync(user, password);

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        => await userManager.GetRolesAsync(user);

    public Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        dbContext.RefreshTokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public async Task<(ApplicationUser? User, RefreshToken? Token)> GetUserByRefreshTokenAsync(
        string tokenHash, CancellationToken cancellationToken = default)
    {
        var token = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        return (token?.User, token);
    }

    public async Task RevokeRefreshTokenFamilyAsync(
        ApplicationUser user, Guid familyId, string reason, CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.FamilyId == familyId && rt.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var token in tokens)
        {
            token.Revoke(now, reason, revokedByIp: null);
        }
    }
}
