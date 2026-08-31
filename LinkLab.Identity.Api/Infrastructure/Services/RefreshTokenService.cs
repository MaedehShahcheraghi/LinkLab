using System.Security.Cryptography;
using System.Text;
using LinkLab.BuildingBlocks.Core.Primitives;
using LinkLab.Identity.Api.Core.Interfaces;
using LinkLab.Identity.Api.Core.Options;
using LinkLab.Identity.Api.Models;
using Microsoft.Extensions.Options;

namespace LinkLab.Identity.Api.Infrastructure.Services;

public sealed class RefreshTokenService(
    IOptions<JwtOptions> options,
    TimeProvider timeProvider,
    IUserRepository userRepository) : IRefreshTokenService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<(RefreshToken Entity, string PlainToken)> GenerateRefreshTokenAsync(
        Guid userId, string? ipAddress, string? userAgent, Guid? familyId = null, CancellationToken cancellationToken = default)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var plainToken  = Convert.ToBase64String(randomBytes);
        var tokenHash   = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken))).ToLowerInvariant();

        var now           = timeProvider.GetUtcNow();
        var expires       = now.AddDays(_options.RefreshTokenExpiryDays);
        var finalFamilyId = familyId ?? Guid.NewGuid();

        var entity = RefreshToken.Create(
            userId:       userId,
            familyId:     finalFamilyId,
            tokenHash:    tokenHash,
            createdAtUtc: now,
            expiresAtUtc: expires,
            createdByIp:  ipAddress,
            userAgent:    userAgent);

        await userRepository.AddRefreshTokenAsync(entity, cancellationToken);
        return (entity, plainToken);
    }

    public async Task<Result<(ApplicationUser User, RefreshToken TokenEntity, string PlainToken)>> ValidateAndRotateRefreshTokenAsync(
        string plainToken, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        var incomingHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken))).ToLowerInvariant();

        var (user, tokenEntity) = await userRepository.GetUserByRefreshTokenAsync(incomingHash, cancellationToken);

        if (user is null || tokenEntity is null)
        {
            return Error.Unauthorized("Auth.InvalidToken", "Invalid token.");
        }

        var now = timeProvider.GetUtcNow();

        if (!tokenEntity.IsActive(now))
        {
            if (tokenEntity.RevokedAtUtc is not null || tokenEntity.IsExpired(now))
            {
                await userRepository.RevokeRefreshTokenFamilyAsync(
                    user,
                    tokenEntity.FamilyId,
                    $"Reuse of inactive token detected. IP: {ipAddress}",
                    cancellationToken);
            }

            return Error.Unauthorized("Auth.InvalidToken", "Invalid token.");
        }

        tokenEntity.Revoke(now, "Replaced by new token", revokedByIp: ipAddress);

        var (newRefreshTokenEntity, newPlainToken) = await GenerateRefreshTokenAsync(
            user.Id, ipAddress, userAgent, tokenEntity.FamilyId, cancellationToken);

        tokenEntity.Revoke(now, "Replaced by new token", ipAddress, newRefreshTokenEntity.TokenHash);

        return (user, newRefreshTokenEntity, newPlainToken);
    }
}
