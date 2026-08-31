using LinkLab.Identity.Api.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LinkLab.Identity.Api.Infrastructure.Services;

public sealed class HttpContextTokenContext(IHttpContextAccessor httpContextAccessor) : ITokenContext
{
    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
    
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
