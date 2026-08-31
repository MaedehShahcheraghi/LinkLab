namespace LinkLab.Identity.Api.Core.DTOs;

public record LoginRequest(
    string Email,
    string Password);
