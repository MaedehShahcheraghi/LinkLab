namespace LinkLab.Identity.Api.Core.Interfaces;

public interface ITokenContext
{
    string? UserAgent { get; }
    string? IpAddress { get; }
}
