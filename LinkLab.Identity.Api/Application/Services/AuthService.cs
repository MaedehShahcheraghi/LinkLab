using System.Text.Json;
using LinkLab.BuildingBlocks.Core.Primitives;
using LinkLab.BuildingBlocks.Idempotency;
using LinkLab.Identity.Api.Core.DTOs;
using LinkLab.Identity.Api.Core.Interfaces;
using LinkLab.Identity.Api.Models;
using Microsoft.AspNetCore.Http;

namespace LinkLab.Identity.Api.Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    IPermissionCalculator permissionCalculator,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    ITokenContext tokenContext,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<Result<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        IdempotencyHandle handle,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var existingUser = await userRepository.FindByEmailAsync(request.Email, cancellationToken);
            if (existingUser is not null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Error.Conflict("User.Exists", "Email is already registered.");
            }

            var user = new ApplicationUser
            {
                UserName     = request.Email,
                Email        = request.Email,
                CreatedAtUtc = timeProvider.GetUtcNow()
            };

            var identityResult = await userRepository.CreateUserAsync(user, request.Password, cancellationToken);
            if (!identityResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                var details = string.Join(" | ", identityResult.Errors.Select(e => e.Description));
                return Error.Validation("User.CreateFailed", "User creation failed.", details);
            }

            var response     = new RegisterResponse(user.Id, user.Email, "User registered successfully.");
            var responseBody = JsonSerializer.Serialize(response);

            var prepared = await idempotencyStore.CompleteAsync(
                handle, StatusCodes.Status201Created, "application/json", responseBody, cancellationToken);

            if (!prepared)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Error.Conflict("Idempotency.Lost", "Idempotency ownership was lost.");
            }

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            await idempotencyStore.WarmCacheFromSqlAsync(handle, CancellationToken.None);

            return response;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");
        }

        var isPasswordValid = await userRepository.CheckPasswordAsync(user, request.Password, cancellationToken);
        if (!isPasswordValid)
        {
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");
        }

        var roles          = await userRepository.GetRolesAsync(user, cancellationToken);
        var permissionMask = await permissionCalculator.CalculateAsync(user.Id, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user, roles, permissionMask);

        var userAgent = tokenContext.UserAgent ?? string.Empty;
        var (_, plainToken) = await refreshTokenService.GenerateRefreshTokenAsync(
            user.Id, ipAddress, userAgent, null, cancellationToken);

        user.LastLoginAtUtc = timeProvider.GetUtcNow();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, plainToken, _accessTokenSeconds);
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(
        RefreshRequest request,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var userAgent = tokenContext.UserAgent ?? string.Empty;
        var rotationResult = await refreshTokenService.ValidateAndRotateRefreshTokenAsync(
            request.RefreshToken, ipAddress, userAgent, cancellationToken);

        if (rotationResult.IsFailure)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken); // Save potential family revokes
            return rotationResult.Error;
        }

        var (user, newRefreshTokenEntity, plainToken) = rotationResult.Value;

        var roles          = await userRepository.GetRolesAsync(user, cancellationToken);
        var permissionMask = await permissionCalculator.CalculateAsync(user.Id, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user, roles, permissionMask);

        user.LastLoginAtUtc = timeProvider.GetUtcNow();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, plainToken, _accessTokenSeconds);
    }

    private const int _accessTokenSeconds = 15 * 60;
}
