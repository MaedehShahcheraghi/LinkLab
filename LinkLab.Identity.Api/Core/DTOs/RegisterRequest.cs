namespace LinkLab.Identity.Api.Core.DTOs;

public record RegisterRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName);
