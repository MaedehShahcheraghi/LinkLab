using LinkLab.BuildingBlocks.Core.Primitives;
using LinkLab.BuildingBlocks.Idempotency;
using LinkLab.Identity.Api.Core.DTOs;

namespace LinkLab.Identity.Api.Core.Interfaces;

public interface IAuthService
{
    Task<Result<RegisterResponse>> RegisterAsync(
        RegisterRequest request, 
        IdempotencyHandle handle, 
        CancellationToken cancellationToken = default);

    Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request, 
        string ipAddress, 
        CancellationToken cancellationToken = default);

    Task<Result<LoginResponse>> RefreshTokenAsync(
        RefreshRequest request, 
        string ipAddress, 
        CancellationToken cancellationToken = default);
}
