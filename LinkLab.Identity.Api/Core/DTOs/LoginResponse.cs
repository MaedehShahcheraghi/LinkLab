namespace LinkLab.Identity.Api.Core.DTOs;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds);
