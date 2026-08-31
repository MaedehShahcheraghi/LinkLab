namespace LinkLab.Identity.Api.Core.DTOs;

public record RegisterResponse(
    Guid UserId,
    string Email,
    string Message);
