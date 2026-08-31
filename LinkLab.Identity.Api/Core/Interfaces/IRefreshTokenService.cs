using LinkLab.BuildingBlocks.Core.Primitives;
using LinkLab.Identity.Api.Models;

namespace LinkLab.Identity.Api.Core.Interfaces;

public interface IRefreshTokenService
{
    Task<(RefreshToken Entity, string PlainToken)> GenerateRefreshTokenAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        Guid? familyId = null,
        CancellationToken cancellationToken = default);

    Task<Result<(ApplicationUser User, RefreshToken TokenEntity, string PlainToken)>> ValidateAndRotateRefreshTokenAsync(
        string plainToken,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default);
}
